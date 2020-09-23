using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
    public class TickList_Patch
    {
        public static AccessTools.FieldRef<TickList, List<List<Thing>>> thingLists =
            AccessTools.FieldRefAccess<TickList, List<List<Thing>>>("thingLists");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToRegister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToRegister");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToDeregister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToDeregister");
        public static AccessTools.FieldRef<TickList, TickerType> tickType =
            AccessTools.FieldRefAccess<TickList, TickerType>("tickType");

        public static int maxThreads = Math.Max(Int32.Parse(RimThreadedMod.Settings.maxThreadsBuffer), 1);
        public static int timeoutMS = Math.Max(Int32.Parse(RimThreadedMod.Settings.timeoutMSBuffer), 1);
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitStarts = new ConcurrentDictionary<int, EventWaitHandle>();
        public static ConcurrentDictionary<int, EventWaitHandle> eventWaitStarts2 = new ConcurrentDictionary<int, EventWaitHandle>();
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

        public static ConcurrentQueue<Thing> drawQueue = new ConcurrentQueue<Thing>();
        public static CellRect cellRect;
        public static bool[] fogGrid;
        public static CellIndices cellIndices;
        public static SnowGrid snowGrid;

        public static ConcurrentQueue<Pawn> tmpPawnsToTick = new ConcurrentQueue<Pawn>();

        public static ConcurrentQueue<WorldObject> tmpWorldObjects = new ConcurrentQueue<WorldObject>();
        //public static WorldPawns worldPawns;
        public static MapCellsInRandomOrder steadyEnvironmentEffectsCellsInRandomOrder = null;
        public static int steadyEnvironmentEffectsTicks = 0;
        public static int steadyEnvironmentEffectsArea = 0;
        public static int steadyEnvironmentEffectsCycleIndex = 0;
        public static SteadyEnvironmentEffects steadyEnvironmentEffectsInstance = null;

        public static bool allWorkerThreadsFinished = false;
        public static ConcurrentDictionary<int, bool> isThreadWaiting = new ConcurrentDictionary<int, bool>();
        public static List<Thing> thingListNormal;
        public static int thingListNormalTicks = 0;
        public static List<Thing> thingListRare;
        public static int thingListRareTicks = 0;
        public static List<Thing> thingListLong;
        public static int thingListLongTicks = 0;
        public static ConcurrentQueue<Thing> thingQueue = new ConcurrentQueue<Thing>();
        public static Thread monitorThread = null;
        public static TickerType currentTickType;
        public static int currentTickInterval;
        public static Dictionary<int, Thread> allThreads = new Dictionary<int, Thread>();
        public static StackTrace trace = null;

        public static WorldObjectsHolder worldObjectsHolder = null;
        public static int worldObjectsTicks = 0;
        public static List<WorldObject> worldObjects = null;

        public static WorldPawns worldPawns = null;
        public static int worldPawnsTicks = 0;

        public static List<Pawn> worldPawnsAlive = null;

        public static int plantMaterialsCount = 0;

        public static float plantSwayHead = 0;

        public static List<Faction> allFactions = null;
        public static int allFactionsTicks = 0;

        public static int steadyEnvironmentEffectsCycleIndexOffset = 0;

        public static int WildPlantSpawnerTicks = 0;
        public static int WildPlantSpawnerCycleIndexOffset = 0;

        public static int WildPlantSpawnerArea = 0;
        public static Map WildPlantSpawnerMap = null;

        public static MapCellsInRandomOrder WildPlantSpawnerCellsInRandomOrder = null;
        public static float WildPlantSpawnerCurrentPlantDensity = 0f;
        public static float DesiredPlants = 0f;
        public static float DesiredPlantsTmp = 0f;
        public static int DesiredPlants1000 = 0;
        public static int DesiredPlantsTmp1000 = 0;
        public static int DesiredPlants2Tmp1000 = 0;
        public static int FertilityCellsTmp = 0;
        public static int FertilityCells2Tmp = 0;
        public static int FertilityCells = 0;
        public static WildPlantSpawner WildPlantSpawnerInstance = null;
        public static float WildPlantSpawnerChance = 0f;

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
            CreateMonitorThread();
            switch (currentTickType)
            {
                case TickerType.Normal:
                    thingListNormal = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    thingListNormalTicks = thingListNormal.Count;
                    break;
                case TickerType.Rare:
                    thingListRare = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    thingListRareTicks = thingListRare.Count;
                    break;
                case TickerType.Long:
                    thingListLong = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    thingListLongTicks = thingListLong.Count;
                    break;
            }
            //thingQueue = new ConcurrentQueue<Thing>(thingList1);
            MainThreadWaitLoop();
            return false;            
        }


        private static void CreateWorkerThread()
        {
            Thread thread = new Thread(() => ProcessTicks());
            int tID = thread.ManagedThreadId;
            allThreads.Add(tID, thread);
            eventWaitStarts.TryAdd(tID, new AutoResetEvent(false));
            eventWaitStarts2.TryAdd(tID, new AutoResetEvent(false));
            eventWaitDones.TryAdd(tID, new AutoResetEvent(false));
            tryMakeAndPlayWaits.TryAdd(tID, new AutoResetEvent(false));
            newSustainerWaits.TryAdd(tID, new AutoResetEvent(false));
            texture2DWaits.TryAdd(tID, new AutoResetEvent(false));
            materialWaits.TryAdd(tID, new AutoResetEvent(false));
            thread.Start();
        }

        public static void AbortWorkerThread(int managedThreadID)
        {
            if (allThreads.TryGetValue(managedThreadID, out Thread thread))
            {
                thread.Abort();
                allThreads.Remove(managedThreadID);
                eventWaitStarts.TryRemove(managedThreadID, out _);
                eventWaitStarts2.TryRemove(managedThreadID, out _);
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

        public static void CreateMonitorThread()
        {
            if (null == monitorThread)
            {
                CreateWorkerThreads();
                monitorThread = new Thread(() =>
                {
                    while (true)
                    {
                        monitorThreadWaitHandle.WaitOne();
                        foreach (EventWaitHandle eventWaitHandle in eventWaitStarts.Values)
                        {
                            eventWaitHandle.Set();
                        }
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
                        foreach (EventWaitHandle eventWaitHandle2 in eventWaitStarts2.Values)
                        {
                            eventWaitHandle2.Set();
                        }
                    }
                });
                monitorThread.Start();
            }
        }

        public static void CreateWorkerThreads()
        {
            while (eventWaitStarts.Count < maxThreads)
            {
                CreateWorkerThread();
            }
        }

        public static void MainThreadWaitLoop()
        {
            //bool continueWaiting = true;
            allWorkerThreadsFinished = false;
            monitorThreadWaitHandle.Set();
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
                    Texture2D content = ContentFinder_Texture2D_Patch.GetTexture2D(itempath);
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
            eventWaitStarts2.TryGetValue(tID, out EventWaitHandle eventWaitStart2);
            eventWaitDones.TryGetValue(tID, out EventWaitHandle eventWaitDone);
            while (true)
            {
                ProcessTicksWait(eventWaitStart);

                while (thingListNormalTicks > 0)
                {
                    int index = Interlocked.Decrement(ref thingListNormalTicks);
                    if (index >= 0)
                    {
                        Thing thing = thingListNormal[index];
                        if (!thing.Destroyed)
                        {
                            thing.Tick();
                        }
                    }
                }
                while (thingListRareTicks > 0)
                {
                    int index = Interlocked.Decrement(ref thingListRareTicks);
                    if (index >= 0)
                    {
                        Thing thing = thingListRare[index];
                        if (!thing.Destroyed)
                        {
                            thing.TickRare();
                        }
                    }
                }
                while (thingListLongTicks > 0)
                {
                    int index = Interlocked.Decrement(ref thingListLongTicks);
                    if (index >= 0)
                    {
                        Thing thing = thingListLong[index];
                        if (!thing.Destroyed)
                        {
                            thing.TickLong();
                        }
                    }
                }

                while (worldPawnsTicks > 0)
                {
                    int index = Interlocked.Decrement(ref worldPawnsTicks);
                    if (index >= 0)
                    {
                        Pawn pawn = worldPawnsAlive[index];
                        try
                        {
                            pawn.Tick();
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorOnce("Exception ticking world pawn " + pawn.ToStringSafe<Pawn>() + ". Suppressing further errors. " + (object)ex, pawn.thingIDNumber ^ 1148571423, false);
                        }
                        try
                        {
                            if (!pawn.Dead && !pawn.Destroyed && (pawn.IsHashIntervalTick(7500) && !pawn.IsCaravanMember()) && !PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
                                TendUtility.DoTend(null, pawn, null);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorOnce("Exception tending to a world pawn " + pawn.ToStringSafe<Pawn>() + ". Suppressing further errors. " + (object)ex, pawn.thingIDNumber ^ 8765780, false);
                        }
                    }
                }

                while(worldObjectsTicks > 0)
                {
                    int index = Interlocked.Decrement(ref worldObjectsTicks);
                    if (index >= 0)
                    {
                        worldObjects[index].Tick();
                    }
                }
                
                while(steadyEnvironmentEffectsTicks > 0)
                {
                    int index = Interlocked.Decrement(ref steadyEnvironmentEffectsTicks);
                    if (index >= 0)
                    {
                        int cycleIndex = (steadyEnvironmentEffectsCycleIndexOffset - index) % steadyEnvironmentEffectsArea;
                        IntVec3 c = steadyEnvironmentEffectsCellsInRandomOrder.Get(cycleIndex);
                        SteadyEnvironmentEffects_Patch.DoCellSteadyEffects(steadyEnvironmentEffectsInstance, c);
                        //Interlocked.Increment(ref SteadyEnvironmentEffects_Patch.cycleIndex(steadyEnvironmentEffectsInstance));
                    }
                }

                while (plantMaterialsCount > 0)
                {
                    int index = Interlocked.Decrement(ref plantMaterialsCount);
                    if (index >= 0)
                    {
                        WindManager_Patch.plantMaterials[index].SetFloat(ShaderPropertyIDs.SwayHead, plantSwayHead);
                    }
                }

                while (allFactionsTicks > 0)
                {
                    int index = Interlocked.Decrement(ref allFactionsTicks);
                    if (index >= 0)
                    {
                        allFactions[index].FactionTick();
                    }
                }

                while (WildPlantSpawnerTicks > 0)
                {
                    int index = Interlocked.Decrement(ref WildPlantSpawnerTicks);
                    if (index >= 0)
                    {
                        int cycleIndex = (WildPlantSpawnerCycleIndexOffset - index) % WildPlantSpawnerArea;
                        IntVec3 intVec = WildPlantSpawnerCellsInRandomOrder.Get(cycleIndex);

                        if ((WildPlantSpawnerCycleIndexOffset - index) > WildPlantSpawnerArea)
                        {
                            Interlocked.Add(ref DesiredPlants2Tmp1000,
                                1000 * (int)WildPlantSpawner_Patch.GetDesiredPlantsCountAt2(WildPlantSpawnerMap, intVec, intVec, WildPlantSpawnerCurrentPlantDensity));
                            if (intVec.GetTerrain(WildPlantSpawnerMap).fertility > 0f)
                            {
                                Interlocked.Increment(ref FertilityCells2Tmp);
                            }

                            float mtb = WildPlantSpawner_Patch.GoodRoofForCavePlant2(WildPlantSpawnerMap, intVec) ? 130f : WildPlantSpawnerMap.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(WildPlantSpawnerChance) && Rand.MTBEventOccurs(mtb, 60000f, 10000) && WildPlantSpawner_Patch.CanRegrowAt2(WildPlantSpawnerMap, intVec))
                            {
                                WildPlantSpawnerInstance.CheckSpawnWildPlantAt(intVec, WildPlantSpawnerCurrentPlantDensity, DesiredPlantsTmp1000 / 1000.0f);
                            }
                        } else
                        {
                            Interlocked.Add(ref DesiredPlantsTmp1000,
                                1000 * (int)WildPlantSpawner_Patch.GetDesiredPlantsCountAt2(WildPlantSpawnerMap, intVec, intVec, WildPlantSpawnerCurrentPlantDensity));
                            if (intVec.GetTerrain(WildPlantSpawnerMap).fertility > 0f)
                            {
                                Interlocked.Increment(ref FertilityCellsTmp);
                            }

                            float mtb = WildPlantSpawner_Patch.GoodRoofForCavePlant2(WildPlantSpawnerMap, intVec) ? 130f : WildPlantSpawnerMap.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(WildPlantSpawnerChance) && Rand.MTBEventOccurs(mtb, 60000f, 10000) && WildPlantSpawner_Patch.CanRegrowAt2(WildPlantSpawnerMap, intVec))
                            {
                                WildPlantSpawnerInstance.CheckSpawnWildPlantAt(intVec, WildPlantSpawnerCurrentPlantDensity, DesiredPlants);
                            }
                        }

                    }
                    if ((WildPlantSpawnerCycleIndexOffset - index) > WildPlantSpawnerArea)
                    {
                        WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlants(WildPlantSpawnerInstance) = DesiredPlantsTmp1000 / 1000.0f;
                        WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlantsTmp(WildPlantSpawnerInstance) = DesiredPlants2Tmp1000 / 1000.0f;
                        WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCells(WildPlantSpawnerInstance) = FertilityCellsTmp;
                        WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCellsTmp(WildPlantSpawnerInstance) = FertilityCells2Tmp;
                    } else
                    {
                        WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlantsTmp(WildPlantSpawnerInstance) = DesiredPlantsTmp1000 / 1000.0f;
                        WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCells(WildPlantSpawnerInstance) = FertilityCellsTmp;
                    }
                }
                
                /*
                while(drawQueue.TryDequeue(out Thing drawThing))
                {
                    IntVec3 position = drawThing.Position;
                    if ((cellRect.Contains(position) || drawThing.def.drawOffscreen) && (!fogGrid[cellIndices.CellToIndex(position)] || drawThing.def.seeThroughFog) && (drawThing.def.hideAtSnowDepth >= 1.0 || snowGrid.GetDepth(position) <= (double)drawThing.def.hideAtSnowDepth))
                    {
                        try
                        {
                            drawThing.Draw();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception drawing " + (object)drawThing + ": " + ex.ToString(), false);
                        }
                    }
                }
                */
                eventWaitDone.Set();
                ProcessTicksWait2(eventWaitStart2);

            }
        }

        private static void ProcessTicksWait(EventWaitHandle eventWaitStart)
        {
            eventWaitStart.WaitOne();
        }
        private static void ProcessTicksWait2(EventWaitHandle eventWaitStart)
        {
            eventWaitStart.WaitOne();
        }
    }
}
