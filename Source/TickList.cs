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
        public static ConcurrentDictionary<int, object[]> tryMakeAndPlayRequests = new ConcurrentDictionary<int, object[]>();
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitStarts = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitDones = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<string, Texture2D> texture2DResults = new ConcurrentDictionary<string, Texture2D>();
        public static ConcurrentDictionary<int, string> texture2DRequests = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<MaterialRequest, Material> materialResults = new ConcurrentDictionary<MaterialRequest, Material>();
        public static ConcurrentDictionary<int, MaterialRequest> materialRequests = new ConcurrentDictionary<int, MaterialRequest>();
        public static EventWaitHandle mainThreadWaitHandle = new AutoResetEvent(false);
        public static EventWaitHandle monitorThreadWaitHandle = new AutoResetEvent(false);
        public static List<Thing> thingList1;
        public static ConcurrentQueue<Thing> thingQueue = new ConcurrentQueue<Thing>();
        public static Thread monitorThread = null;
        public static TickerType currentTickType;
        public static int currentTickInterval;
        public static Dictionary<int, Thread> allThreads = new Dictionary<int, Thread>();
        public static Thread thread2 = null;
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

            CreateTickThreads();       
            foreach (EventWaitHandle eventWaitHandle in eventWaitStarts.Values)
            {
                eventWaitHandle.Set();
            }

            monitorThreadWaitHandle.Set();
            MainThreadWaitLoop();
                
            return false;            
        }

        private static void CreateTickThreads()
        {
            while (eventWaitStarts.Count < maxThreads)
            {
                Thread thread = new Thread(() => ProcessTicks());
                int tID = thread.ManagedThreadId;
                allThreads.Add(tID, thread);
                eventWaitStarts.TryAdd(tID, new AutoResetEvent(false));
                eventWaitDones.TryAdd(tID, new AutoResetEvent(false));
                thread.Start();

                if (null == monitorThread)
                {
                    monitorThread = new Thread(() =>
                    {
                        while (true)
                        {
                            monitorThreadWaitHandle.WaitOne();
                            //List<int> ewd = eventWaitDones.Keys.ToList();
                            foreach (int tID2 in eventWaitDones.Keys)
                            {
                                eventWaitDones.TryGetValue(tID2, out EventWaitHandle eventWaitDone);
                                if (!eventWaitDone.WaitOne(1000))
                                {
                                    Log.Error("Thread: " + tID2.ToString() + " did not finish within 1000ms. Restarting thread...");                                    
                                    Thread thread2 = allThreads[tID2];
                                    thread2.Abort();
                                    allThreads.Remove(tID2);
                                    eventWaitStarts.TryRemove(tID2, out _);
                                    eventWaitDones.TryRemove(tID2, out _);
                                    Thread thread3 = new Thread(() => ProcessTicks());
                                    int tID3 = thread3.ManagedThreadId;
                                    allThreads.Add(tID3, thread);
                                    AutoResetEvent eventWaitStart1 = new AutoResetEvent(false);
                                    eventWaitStarts.TryAdd(tID3, eventWaitStart1);
                                    AutoResetEvent eventWaitDone1 = new AutoResetEvent(false);
                                    eventWaitDones.TryAdd(tID3, eventWaitDone1);
                                    thread3.Start();
                                    eventWaitStart1.Set();
                                    //ewd.Add(eventWaitDone1);
                                    //thread2.Suspend();
                                    //trace = new System.Diagnostics.StackTrace(thread2, false);
                                    //Log.Error(trace.ToString());
                                }
                            }
                            mainThreadWaitHandle.Set();
                        }
                    });
                    monitorThread.Start();
                }
            }
        }

        private static void MainThreadWaitLoop()
        {
            bool continueWaiting = true;
            while (continueWaiting)
            {
                mainThreadWaitHandle.WaitOne();
                continueWaiting = false;
                if (texture2DRequests.Count > 0)
                {
                    continueWaiting = true;
                    int key = texture2DRequests.Keys.First();
                    if (texture2DRequests.TryRemove(key, out string itempath))
                    {
                        Texture2D content = ContentFinder<Texture2D>.Get(itempath);
                        texture2DResults.TryAdd(itempath, content);
                    }
                    eventWaitStarts.TryGetValue(key, out EventWaitHandle eventWaitStart);
                    eventWaitStart.Set();

                }
                if (materialRequests.Count > 0)
                {
                    continueWaiting = true;
                    int key = materialRequests.Keys.First();
                    if (materialRequests.TryRemove(key, out MaterialRequest materialRequest))
                    {
                        Material material = MaterialPool.MatFrom(materialRequest);
                        materialResults.TryAdd(materialRequest, material);
                    }
                    eventWaitStarts.TryGetValue(key, out EventWaitHandle eventWaitStart);
                    eventWaitStart.Set();
                }
                if (tryMakeAndPlayRequests.Count > 0)
                {
                    continueWaiting = true;
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
                    eventWaitStarts.TryGetValue(key, out EventWaitHandle eventWaitStart);
                    eventWaitStart.Set();
                }
            }
        }

        public static void ProcessTicks()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            eventWaitStarts.TryGetValue(tID, out EventWaitHandle eventWaitStart);
            eventWaitDones.TryGetValue(tID, out EventWaitHandle eventWaitDone);
            while (true)
            {
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
