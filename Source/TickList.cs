using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class Ticklist_Patch
    {
        public static AccessTools.FieldRef<TickList, List<List<Thing>>> thingLists =
            AccessTools.FieldRefAccess<TickList, List<List<Thing>>>("thingLists");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToRegister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToRegister");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToDeregister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToDeregister");
        public static AccessTools.FieldRef<TickList, TickerType> tickType =
            AccessTools.FieldRefAccess<TickList, TickerType>("tickType");

        public static int maxThreads = Math.Max(RimThreadedMod.Settings.maxThreads, 1);
        public static int timeoutMS = Math.Max(RimThreadedMod.Settings.timeoutMS, 1);
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitStarts = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitDones = new ConcurrentDictionary<int, EventWaitHandle>();
        public static EventWaitHandle mainThreadWaitHandle = new AutoResetEvent(false);
        public static EventWaitHandle monitorThreadWaitHandle = new AutoResetEvent(false);

        public static ConcurrentDictionary<int, object[]> tryMakeAndPlayRequests = new ConcurrentDictionary<int, object[]>();
        public static ConcurrentDictionary<int, EventWaitHandle> tryMakeAndPlayWaits = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<int, object[]> newSustainerRequests = new ConcurrentDictionary<int, object[]>();
        public static ConcurrentDictionary<int, Sustainer> newSustainerResults = new ConcurrentDictionary<int, Sustainer>();
        public static ConcurrentDictionary<int, EventWaitHandle> newSustainerWaits = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<string, Texture2D> texture2DResults = new ConcurrentDictionary<string, Texture2D>();
        public static ConcurrentDictionary<int, string> texture2DRequests = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<int, EventWaitHandle> texture2DWaits = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<MaterialRequest, Material> materialResults = new ConcurrentDictionary<MaterialRequest, Material>();
        public static ConcurrentDictionary<int, MaterialRequest> materialRequests = new ConcurrentDictionary<int, MaterialRequest>();
        public static ConcurrentDictionary<int, EventWaitHandle> materialWaits = new ConcurrentDictionary<int, EventWaitHandle>();

        public static ConcurrentQueue<Tuple<SoundDef, SoundInfo>> PlayOneShot = new ConcurrentQueue<Tuple<SoundDef, SoundInfo>>();
        public static ConcurrentQueue<Tuple<SoundDef, Map>> PlayOneShotCamera = new ConcurrentQueue<Tuple<SoundDef, Map>>();

        public static bool allWorkerThreadsFinished = false;
        public static ConcurrentDictionary<int, bool> isThreadWaiting = new ConcurrentDictionary<int, bool>();
        public static List<Thing> thingList1;
        public static ConcurrentQueue<Thing> thingQueue = new ConcurrentQueue<Thing>();
        public static Thread monitorThread = null;
        public static TickerType currentTickType;
        public static int currentTickInterval;
        public static Dictionary<int, Thread> allThreads = new Dictionary<int, Thread>();
        public static StackTrace trace = null;
        private static int get_TickInterval(TickList __instance)
        {
            switch (currentTickType)
            {
                case TickerType.Normal:
                    return 1;
                case TickerType.Rare:
                    return 250;
                case TickerType.Long:
                    return 2000;
                default:
                    return -1;
            }            
        }
        private static List<Thing> BucketOf(TickList __instance, Thing t)
        {
            int hashCode = t.GetHashCode();
            if (hashCode < 0)
                hashCode *= -1;
            return thingLists(__instance)[hashCode % currentTickInterval];
        }
        public static bool Tick(TickList __instance)
        {
            currentTickType = tickType(__instance);
            currentTickInterval = get_TickInterval(__instance);
            List<Thing> tr = thingsToRegister(__instance);
            for (int index = 0; index < tr.Count; ++index)
            {
                Thing i = tr[index];
                List<Thing> b = BucketOf(__instance, i);
                b.Add(i);
            }            
            tr.Clear();

            List<Thing> td = thingsToDeregister(__instance);
            for (int index = 0; index < td.Count; ++index)
            {
                Thing i = td[index];
                List<Thing> b = BucketOf(__instance, i);
                b.Remove(i);
            }
            td.Clear();

            if (DebugSettings.fastEcology)
            {
                Find.World.tileTemperatures.ClearCaches();
                for (int index1 = 0; index1 < thingLists(__instance).Count; ++index1)
                {
                    List<Thing> thingList = thingLists(__instance)[index1];
                    for (int index2 = 0; index2 < thingList.Count; ++index2)
                    {
                        if (thingList[index2].def.category == ThingCategory.Plant)
                            thingList[index2].TickLong();
                    }
                }
            }
            thingList1 = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
            thingQueue = new ConcurrentQueue<Thing>(thingList1);

            CreateWorkerThreads();
            allWorkerThreadsFinished = false;
            CreateMonitorThread();
            monitorThreadWaitHandle.Set();
            foreach (EventWaitHandle eventWaitHandle in eventWaitStarts.Values)
            {
                eventWaitHandle.Set();
            }

            MainThreadWaitLoop();
            return false;            
        }

        private static void CreateWorkerThread()
        {
            Thread thread = new Thread(() => ProcessTicks());
            int tID = thread.ManagedThreadId;
            allThreads.Add(tID, thread);
            eventWaitStarts.TryAdd(tID, new AutoResetEvent(false));
            eventWaitDones.TryAdd(tID, new AutoResetEvent(false));
            tryMakeAndPlayWaits.TryAdd(tID, new AutoResetEvent(false));
            newSustainerWaits.TryAdd(tID, new AutoResetEvent(false));
            texture2DWaits.TryAdd(tID, new AutoResetEvent(false));
            materialWaits.TryAdd(tID, new AutoResetEvent(false));
            thread.Start();
        }

        private static void AbortWorkerThread(int managedThreadID)
        {
            if (allThreads.TryGetValue(managedThreadID, out Thread thread))
            {
                thread.Abort();
                allThreads.Remove(managedThreadID);
                eventWaitStarts.TryRemove(managedThreadID, out _);
                eventWaitDones.TryRemove(managedThreadID, out _);
                tryMakeAndPlayWaits.TryRemove(managedThreadID, out _);
                newSustainerWaits.TryRemove(managedThreadID, out _);
                texture2DWaits.TryRemove(managedThreadID, out _);
                materialWaits.TryRemove(managedThreadID, out _);
            } else
            {
                Log.Error("Error finding timed out thread: " + managedThreadID.ToString());
            }
        }

        private static void CreateMonitorThread()
        {
            if (null == monitorThread)
            {
                monitorThread = new Thread(() =>
                {
                    while (true)
                    {
                        monitorThreadWaitHandle.WaitOne();
                        foreach (int tID2 in eventWaitDones.Keys.ToArray())
                        {
                            if (eventWaitDones.TryGetValue(tID2, out EventWaitHandle eventWaitDone))
                            {
                                if (!eventWaitDone.WaitOne(timeoutMS))
                                {
                                    Log.Error("Thread: " + tID2.ToString() + " did not finish within " + timeoutMS.ToString() + "ms. Restarting thread...");
                                    AbortWorkerThread(tID2);
                                    CreateWorkerThread();
                                }
                            }
                            else
                            {
                                Log.Error("Thread monitor cannot find thread: " + tID2.ToString());
                            }
                        }
                        allWorkerThreadsFinished = true;
                        mainThreadWaitHandle.Set();
                    }
                });
                monitorThread.Start();
            }
        }

        private static void CreateWorkerThreads()
        {
            while (eventWaitStarts.Count < maxThreads)
            {
                CreateWorkerThread();
            }
        }

        private static void MainThreadWaitLoop()
        {
            //bool continueWaiting = true;
            while (!allWorkerThreadsFinished)
            {
                mainThreadWaitHandle.WaitOne();
                RespondToTexture2DRequests();
                RespondToMaterialRequests();
                RespondToPlayRequests();
                RespondToSustainerRequests();
                
                // Add any sounds that were produced in this tick
                while (PlayOneShot.Count > 0)
                {
                    if (PlayOneShot.TryDequeue(out Tuple<SoundDef, SoundInfo> s))
                    {
                        s.Item1.PlayOneShot(s.Item2);
                    }
                }
                while (PlayOneShotCamera.Count > 0)
                {
                    if (PlayOneShotCamera.TryDequeue(out Tuple<SoundDef, Map> s))
                    {
                        s.Item1.PlayOneShotOnCamera(s.Item2);
                    }
                }
            }
        }

        private static void RespondToSustainerRequests()
        {
            while (newSustainerRequests.Count > 0)
            {
                int key = newSustainerRequests.Keys.First();
                if (newSustainerRequests.TryRemove(key, out object[] objects))
                {
                    SoundDef soundDef = (SoundDef)objects[0];
                    SoundInfo soundInfo = (SoundInfo)objects[1];
                    Sustainer sustainer = new Sustainer(soundDef, soundInfo);
                    newSustainerResults[key] = sustainer;
                }
                if (newSustainerWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToPlayRequests()
        {
            while (tryMakeAndPlayRequests.Count > 0)
            {
                int key = tryMakeAndPlayRequests.Keys.First();
                if (tryMakeAndPlayRequests.TryRemove(key, out object[] objects))
                {
                    SubSustainer subSustainer = (SubSustainer)objects[0];
                    AudioClip clip = (AudioClip)objects[1];
                    float num2 = (float)objects[2];
                    SampleSustainer sampleSustainer = SampleSustainer.TryMakeAndPlay(subSustainer, clip, num2);
                    if (sampleSustainer != null)
                    {
                        if (subSustainer.subDef.sustainSkipFirstAttack && Time.frameCount == subSustainer.creationFrame)
                            sampleSustainer.resolvedSkipAttack = true;
                        SubSustainer_Patch.samples(subSustainer).Add(sampleSustainer);
                    }
                }
                if (tryMakeAndPlayWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToMaterialRequests()
        {
            while (materialRequests.Count > 0)
            {
                int key = materialRequests.Keys.First();
                if (materialRequests.TryRemove(key, out MaterialRequest materialRequest))
                {
                    Material material = MaterialPool.MatFrom(materialRequest);
                    materialResults.TryAdd(materialRequest, material);
                }
                if (materialWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToTexture2DRequests()
        {
            while (texture2DRequests.Count > 0)
            {
                int key = texture2DRequests.Keys.First();
                if (texture2DRequests.TryRemove(key, out string itempath))
                {
                    Texture2D content = ContentFinder<Texture2D>.Get(itempath);
                    texture2DResults.TryAdd(itempath, content);
                }
                if (texture2DWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        public static void ProcessTicks()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            eventWaitStarts.TryGetValue(tID, out EventWaitHandle eventWaitStart);
            eventWaitDones.TryGetValue(tID, out EventWaitHandle eventWaitDone);
            while (true)
            {
                isThreadWaiting[tID] = true;
                eventWaitStart.WaitOne();
                while (thingQueue.TryDequeue(out Thing thing))
                {
                    if (!thing.Destroyed)
                    {
                        switch (currentTickType)
                        {
                            case TickerType.Normal:
                                thing.Tick();
                                break;
                            case TickerType.Rare:
                                thing.TickRare();
                                break;
                            case TickerType.Long:
                                thing.TickLong();
                                break;
                        }
                    }
                }
                eventWaitDone.Set();
            }
        }
    }
}
