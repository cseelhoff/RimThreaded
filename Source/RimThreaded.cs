using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using RimWorld.Planet;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace RimThreaded
{
    [StaticConstructorOnStartup]
    public class RimThreaded
    {

        public static int maxThreads = Math.Max(Int32.Parse(RimThreadedMod.Settings.maxThreadsBuffer), 1);
        public static int timeoutMS = Math.Max(Int32.Parse(RimThreadedMod.Settings.timeoutMSBuffer), 1);
        public static bool suppressTexture2dError = RimThreadedMod.Settings.suppressTexture2dError;
        public static EventWaitHandle mainThreadWaitHandle = new AutoResetEvent(false);
        public static EventWaitHandle monitorThreadWaitHandle = new AutoResetEvent(false);
        public static Dictionary<int, EventWaitHandle> eventWaitStarts = new Dictionary<int, EventWaitHandle>();
        public static Dictionary<int, EventWaitHandle> eventWaitDones = new Dictionary<int, EventWaitHandle>();

        //public static ConcurrentQueue<Thing> drawQueue = new ConcurrentQueue<Thing>();
        private static Dictionary<int, Thread> allThreads = new Dictionary<int, Thread>();
        private static Thread monitorThread = null;
        private static bool allWorkerThreadsFinished = false;
        public static bool SingleTickComplete = true;

        //MainThreadRequests
        public static Dictionary<int, EventWaitHandle> mainRequestWaits = new Dictionary<int, EventWaitHandle>();
        public static Dictionary<int, object[]> tryMakeAndPlayRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, SampleSustainer> tryMakeAndPlayResults = new Dictionary<int, SampleSustainer>();
        public static Dictionary<int, object[]> newSustainerRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, Sustainer> newSustainerResults = new Dictionary<int, Sustainer>();
        public static Dictionary<string, Texture2D> texture2DResults = new Dictionary<string, Texture2D>();
        public static Dictionary<int, string> texture2DRequests = new Dictionary<int, string>();
        public static Dictionary<MaterialRequest, Material> materialResults = new Dictionary<MaterialRequest, Material>();
        public static Dictionary<int, MaterialRequest> materialRequests = new Dictionary<int, MaterialRequest>();
        public static Dictionary<int, Map> generateMapResults = new Dictionary<int, Map>();
        public static Dictionary<int, object[]> generateMapRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, AudioSource> newAudioSourceResults = new Dictionary<int, AudioSource>();
        public static Dictionary<int, GameObject> newAudioSourceRequests = new Dictionary<int, GameObject>();
        public static Dictionary<int, RenderTexture> renderTextureResults = new Dictionary<int, RenderTexture>();
        public static Dictionary<int, object[]> renderTextureRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, RenderTexture> renderTextureSetActiveRequests = new Dictionary<int, RenderTexture>();
        public static Dictionary<int, RenderTexture> renderTextureGetActiveRequests = new Dictionary<int, RenderTexture>();
        public static Dictionary<int, RenderTexture> renderTextureGetActiveResults = new Dictionary<int, RenderTexture>();
        public static Dictionary<int, object[]> texture2dRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, Texture2D> texture2dResults = new Dictionary<int, Texture2D>();
        public static Dictionary<int, object[]> blitRequests = new Dictionary<int, object[]>();
        public static Dictionary<int, Mesh> newBoltMeshResults = new Dictionary<int, Mesh>();
        public static ConcurrentQueue<int> newBoltMeshRequests = new ConcurrentQueue<int>();
        public static HashSet<int> timeoutExemptThreads = new HashSet<int>();

        public static ConcurrentQueue<Tuple<SoundDef, SoundInfo>> PlayOneShot = new ConcurrentQueue<Tuple<SoundDef, SoundInfo>>();
        public static ConcurrentQueue<Tuple<SoundDef, Map>> PlayOneShotCamera = new ConcurrentQueue<Tuple<SoundDef, Map>>();

        public static Stopwatch stopwatch = new Stopwatch();

        //ThingListTicks
        public static List<Thing> thingListNormal;
        public static int thingListNormalTicks = 0;
        public static List<Thing> thingListRare;
        public static int thingListRareTicks = 0;
        public static List<Thing> thingListLong;
        public static int thingListLongTicks = 0;

        //SteadyEnvironmentEffects
        public static MapCellsInRandomOrder steadyEnvironmentEffectsCellsInRandomOrder = null;
        public static int steadyEnvironmentEffectsTicks = 0;
        public static int steadyEnvironmentEffectsArea = 0;
        public static int steadyEnvironmentEffectsCycleIndex = 0;
        public static SteadyEnvironmentEffects steadyEnvironmentEffectsInstance = null;
        public static int steadyEnvironmentEffectsCycleIndexOffset = 0;

        //WorldObjectsHolder
        public static WorldObjectsHolder worldObjectsHolder = null;
        public static int worldObjectsTicks = 0;
        public static List<WorldObject> worldObjects = null;

        //WorldPawns
        public static WorldPawns worldPawns = null;
        public static int worldPawnsTicks = 0;
        public static List<Pawn> worldPawnsAlive = null;

        //WindManager
        public static int plantMaterialsCount = 0;
        public static float plantSwayHead = 0;
        
        //FactionManager
        public static List<Faction> allFactions = null;
        public static int allFactionsTicks = 0;

        //WildPlantSpawner
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

        //TradeShip
        public static int TradeShipTicks = 0;
        public static ThingOwner TradeShipThings = null;

        //WorldComponents
        public static int WorldComponentTicks = 0;
        public static List<WorldComponent> WorldComponents = null;

        public static int currentPrepsDone = -1;
        public static readonly int totalPrepsCount = 11;
        public static List<EventWaitHandle> prepEventWaitStarts = new List<EventWaitHandle>();
        public static EventWaitHandle ProcessTicksManualWait = new ManualResetEvent(false);
        public static EventWaitHandle WaitingForAllThreadsToComplete = new ManualResetEvent(false);
        public static int workingOnMapPreTick = -1;
        public static int workingOnTickListNormal = -1;
        public static int workingOnTickListRare = -1;
        public static int workingOnTickListLong = -1;
        public static int workingOnDateNotifierTick = -1;
        public static int workingOnWorldTick = -1;
        public static int workingOnMapPostTick = -1;
        public static int workingOnHistoryTick = -1;
        public static int workingOnMiscellaneous = -1;
        public static TickManager currentInstance;
        public static int listsFullyProcessed = 0;

        static RimThreaded()
        {
            CreateWorkerThreads();
            monitorThread = new Thread(() => MonitorThreads());
            monitorThread.Start();
            for(int index = 0; index < totalPrepsCount; index++)
            {
                prepEventWaitStarts.Add(new ManualResetEvent(false));
            }
        }

        public static void RestartAllWorkerThreads()
        {
            foreach (int tID2 in eventWaitDones.Keys.ToArray())
            {
                AbortWorkerThread(tID2);
            }
            CreateWorkerThreads();
        }

        private static void CreateWorkerThreads()
        {
            while (allThreads.Count < maxThreads)
            {
                CreateWorkerThread();
            }
        }

        private static void CreateWorkerThread()
        {
            Thread thread = new Thread(() => ProcessTicks());
            int tID = thread.ManagedThreadId;
            allThreads.Add(tID, thread);
            lock (eventWaitStarts)
            {
                eventWaitStarts[tID] = new AutoResetEvent(false);
            }
            lock (eventWaitDones)
            {
                eventWaitDones[tID] = new AutoResetEvent(false);
            }
            lock(mainRequestWaits)
            {
                mainRequestWaits[tID] = new AutoResetEvent(false);
            }            
            thread.Start();
        }

        private static void ProcessTicks()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            eventWaitStarts.TryGetValue(tID, out EventWaitHandle eventWaitStart);
            eventWaitDones.TryGetValue(tID, out EventWaitHandle eventWaitDone);
            while (true)
            {
                eventWaitStart.WaitOne();
                PrepareWorkLists();
                for(int loopsCompleted = listsFullyProcessed; loopsCompleted < totalPrepsCount; loopsCompleted++)
                {
                    prepEventWaitStarts[loopsCompleted].WaitOne();
                    ExecuteTicks();
                }
                CompletePostWorkLists();
                eventWaitDone.Set();
                //WaitingForAllThreadsToComplete.WaitOne();
            }
        }

        private static void CompletePostWorkLists()
        {
            if (Interlocked.Increment(ref workingOnDateNotifierTick) == 0)
            {
                try
                {
                    Find.DateNotifier.DateNotifierTick();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            if (Interlocked.Increment(ref workingOnHistoryTick) == 0)
            {
                try
                {
                    Find.History.HistoryTick();
                }
                catch (Exception ex10)
                {
                    Log.Error(ex10.ToString());
                }
            }
            if (Interlocked.Increment(ref workingOnMiscellaneous) == 0)
            {
                try
                {
                    Find.Scenario.TickScenario();
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2.ToString());
                }

                try
                {
                    Find.StoryWatcher.StoryWatcherTick();
                }
                catch (Exception ex4)
                {
                    Log.Error(ex4.ToString());
                }

                try
                {
                    Find.GameEnder.GameEndTick();
                }
                catch (Exception ex5)
                {
                    Log.Error(ex5.ToString());
                }

                try
                {
                    Find.Storyteller.StorytellerTick();
                }
                catch (Exception ex6)
                {
                    Log.Error(ex6.ToString());
                }

                try
                {
                    Find.TaleManager.TaleManagerTick();
                }
                catch (Exception ex7)
                {
                    Log.Error(ex7.ToString());
                }

                try
                {
                    Find.QuestManager.QuestManagerTick();
                }
                catch (Exception ex8)
                {
                    Log.Error(ex8.ToString());
                }

                try
                {
                    Find.World.WorldPostTick();
                }
                catch (Exception ex9)
                {
                    Log.Error(ex9.ToString());
                }

                try
                {
                    Find.Scenario.TickScenario();
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2.ToString());
                }

                try
                {
                    Find.StoryWatcher.StoryWatcherTick();
                }
                catch (Exception ex4)
                {
                    Log.Error(ex4.ToString());
                }

                try
                {
                    Find.GameEnder.GameEndTick();
                }
                catch (Exception ex5)
                {
                    Log.Error(ex5.ToString());
                }

                try
                {
                    Find.Storyteller.StorytellerTick();
                }
                catch (Exception ex6)
                {
                    Log.Error(ex6.ToString());
                }

                try
                {
                    Find.TaleManager.TaleManagerTick();
                }
                catch (Exception ex7)
                {
                    Log.Error(ex7.ToString());
                }

                try
                {
                    Find.QuestManager.QuestManagerTick();
                }
                catch (Exception ex8)
                {
                    Log.Error(ex8.ToString());
                }

                try
                {
                    Find.World.WorldPostTick();
                }
                catch (Exception ex9)
                {
                    Log.Error(ex9.ToString());
                }


                GameComponentUtility.GameComponentTick();
                try
                {
                    Find.LetterStack.LetterStackTick();
                }
                catch (Exception ex11)
                {
                    Log.Error(ex11.ToString());
                }

                try
                {
                    Find.Autosaver.AutosaverTick();
                }
                catch (Exception ex12)
                {
                    Log.Error(ex12.ToString());
                }

                try
                {
                    FilthMonitor2.FilthMonitorTick();
                }
                catch (Exception ex13)
                {
                    Log.Error(ex13.ToString());
                }
            }
        }

        private static void PrepareWorkLists()
        {
            if (Interlocked.Increment(ref workingOnMapPreTick) == 0)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    maps[i].MapPreTick();
                }
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //WindManager
            }

            if (Interlocked.Increment(ref workingOnTickListNormal) == 0)
            {
                TickManager_Patch.tickListNormal(currentInstance).Tick();
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //TickNormal
            }
            if (Interlocked.Increment(ref workingOnTickListRare) == 0)
            {
                TickManager_Patch.tickListRare(currentInstance).Tick();
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //TickRare
            }
            if (Interlocked.Increment(ref workingOnTickListLong) == 0)
            {
                TickManager_Patch.tickListLong(currentInstance).Tick();
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //TickLong
            }
            if (Interlocked.Increment(ref workingOnWorldTick) == 0)
            {
                try
                {
                    World world = Find.World;
                    world.worldPawns.WorldPawnsTick();
                    world.factionManager.FactionManagerTick();
                    world.worldObjects.WorldObjectsHolderTick();
                    world.debugDrawer.WorldDebugDrawerTick();
                    world.pathGrid.WorldPathGridTick();
                    WorldComponentUtility.WorldComponentTick(world);
                }
                catch (Exception ex3)
                {
                    Log.Error(ex3.ToString());
                }
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //WorldPawns
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //Factions
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //WorldObjects
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //WorldComponents
            }
            if (Interlocked.Increment(ref workingOnMapPostTick) == 0)
            {
                List<Map> maps = Find.Maps;
                for (int j = 0; j < maps.Count; j++)
                {
                    maps[j].MapPostTick();
                }
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //WildPlantSpawner
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //SteadyEnvironment
                prepEventWaitStarts[Interlocked.Increment(ref currentPrepsDone)].Set(); //PassingShipManagerTick
            }

        }

        private static void ExecuteTicks()
        {

            if (thingListNormalTicks > 0)
            {
                int index = Interlocked.Decrement(ref thingListNormalTicks);
                while (index >= 0)
                {
                    Thing thing = thingListNormal[index];
                    if (!thing.Destroyed)
                    {
                        thing.Tick();
                    }
                    index = Interlocked.Decrement(ref thingListNormalTicks);
                }
                if(index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }
            else if (thingListRareTicks > 0)
            {
                int index = Interlocked.Decrement(ref thingListRareTicks);
                while (index >= 0)
                {
                    Thing thing = thingListRare[index];
                    if (!thing.Destroyed)
                    {
                        thing.TickRare();
                    }
                    index = Interlocked.Decrement(ref thingListRareTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }
            else if (thingListLongTicks > 0)
            {
                int index = Interlocked.Decrement(ref thingListLongTicks);
                while (index >= 0)
                {
                    Thing thing = thingListLong[index];
                    if (!thing.Destroyed)
                    {
                        thing.TickLong();
                    }
                    index = Interlocked.Decrement(ref thingListLongTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }
            else if (worldPawnsTicks > 0)
            {
                int index = Interlocked.Decrement(ref worldPawnsTicks);
                while (index >= 0)
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
                    index = Interlocked.Decrement(ref worldPawnsTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }

            else if (worldObjectsTicks > 0)
            {
                int index = Interlocked.Decrement(ref worldObjectsTicks);
                while (index >= 0)
                {
                    worldObjects[index].Tick();
                    index = Interlocked.Decrement(ref worldObjectsTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }

            else if (steadyEnvironmentEffectsTicks > 0)
            {
                int index = Interlocked.Decrement(ref steadyEnvironmentEffectsTicks);
                while (index >= 0)
                {
                    int cycleIndex = (steadyEnvironmentEffectsCycleIndexOffset - index) % steadyEnvironmentEffectsArea;
                    IntVec3 c = steadyEnvironmentEffectsCellsInRandomOrder.Get(cycleIndex);
                    SteadyEnvironmentEffects_Patch.DoCellSteadyEffects(steadyEnvironmentEffectsInstance, c);
                    //Interlocked.Increment(ref SteadyEnvironmentEffects_Patch.cycleIndex(steadyEnvironmentEffectsInstance));
                    index = Interlocked.Decrement(ref steadyEnvironmentEffectsTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }

            else if (plantMaterialsCount > 0)
            {
                int index = Interlocked.Decrement(ref plantMaterialsCount);
                while (index >= 0)
                {
                    WindManager_Patch.plantMaterials[index].SetFloat(ShaderPropertyIDs.SwayHead, plantSwayHead);
                    index = Interlocked.Decrement(ref steadyEnvironmentEffectsTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }

            else if (allFactionsTicks > 0)
            {
                int index = Interlocked.Decrement(ref allFactionsTicks);
                while (index >= 0)
                {
                    allFactions[index].FactionTick();
                    index = Interlocked.Decrement(ref allFactionsTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }

            else if (WildPlantSpawnerTicks > 0)
            {
                int index = Interlocked.Decrement(ref WildPlantSpawnerTicks);
                while (index >= 0)
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
                    }
                    else
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
                    index = Interlocked.Decrement(ref WildPlantSpawnerTicks);
                }
                if ((WildPlantSpawnerCycleIndexOffset - index) > WildPlantSpawnerArea)
                {
                    WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlants(WildPlantSpawnerInstance) = DesiredPlantsTmp1000 / 1000.0f;
                    WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlantsTmp(WildPlantSpawnerInstance) = DesiredPlants2Tmp1000 / 1000.0f;
                    WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCells(WildPlantSpawnerInstance) = FertilityCellsTmp;
                    WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCellsTmp(WildPlantSpawnerInstance) = FertilityCells2Tmp;
                }
                else
                {
                    WildPlantSpawner_Patch.calculatedWholeMapNumDesiredPlantsTmp(WildPlantSpawnerInstance) = DesiredPlantsTmp1000 / 1000.0f;
                    WildPlantSpawner_Patch.calculatedWholeMapNumNonZeroFertilityCells(WildPlantSpawnerInstance) = FertilityCellsTmp;
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }
            else if (TradeShipTicks > 0)
            {
                int index = Interlocked.Decrement(ref TradeShipTicks);
                while (index >= 0)
                {
                    Pawn pawn = TradeShipThings[index] as Pawn;
                    if (pawn != null)
                    {
                        pawn.Tick();
                        if (pawn.Dead)
                        {
                            lock (TradeShipThings)
                            {
                                TradeShipThings.Remove(pawn);
                            }
                        }
                    }
                    index = Interlocked.Decrement(ref TradeShipTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
                }
            }
            else if (WorldComponentTicks > 0)
            {
                int index = Interlocked.Decrement(ref WorldComponentTicks);
                while (index >= 0)
                {
                    //try
                    //{
                    WorldComponent wc = WorldComponents[index];
                    if (null != wc)
                    {
                        wc.WorldComponentTick();
                    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    Log.Error(ex.ToString());
                    //}
                    index = Interlocked.Decrement(ref WorldComponentTicks);
                }
                if (index == -1)
                {
                    Interlocked.Increment(ref listsFullyProcessed);
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

        }

        private static void MonitorThreads()
        {
            while (true)
            {
                monitorThreadWaitHandle.WaitOne();
                workingOnMapPreTick = -1;
                workingOnTickListNormal = -1;
                workingOnTickListRare = -1;
                workingOnTickListLong = -1;
                workingOnDateNotifierTick = -1;
                workingOnWorldTick = -1;
                workingOnMapPostTick = -1;
                workingOnHistoryTick = -1;
                currentPrepsDone = -1;
                workingOnMiscellaneous = -1;
                listsFullyProcessed = 0;
                foreach (EventWaitHandle eventWaitStart in eventWaitStarts.Values)
                {
                    eventWaitStart.Set();
                }
                stopwatch.Restart();
                foreach (int tID2 in eventWaitDones.Keys.ToList())
                {
                    if (eventWaitDones.TryGetValue(tID2, out EventWaitHandle eventWaitDone))
                    {
                        if (!eventWaitDone.WaitOne(timeoutMS))
                        {
                            if (!timeoutExemptThreads.Contains(tID2))
                            {
                                Log.Error("Thread: " + tID2.ToString() + " did not finish within " + timeoutMS.ToString() + "ms. Restarting thread...");
                                AbortWorkerThread(tID2);
                                CreateWorkerThread();
                            } else
                            {
                                eventWaitDone.WaitOne();
                                timeoutExemptThreads.Remove(tID2);
                            }
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
        }

        private static void AbortWorkerThread(int managedThreadID)
        {
            if (allThreads.TryGetValue(managedThreadID, out Thread thread))
            {
                thread.Abort();
                allThreads.Remove(managedThreadID);
                lock (eventWaitStarts)
                {
                    eventWaitStarts.Remove(managedThreadID);
                }
                lock (eventWaitDones)
                {
                    eventWaitDones.Remove(managedThreadID);
                }
                lock (mainRequestWaits)
                {
                    mainRequestWaits.Remove(managedThreadID);
                }
            }
            else
            {
                Log.Error("Error finding timed out thread: " + managedThreadID.ToString());
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
                RespondToGenerateMapRequests();
                RespondToNewAudioSourceRequests();
                RespondToRenderTextureRequests();
                RespondToNewBoltMeshRequests();
                RespondToBlitRequests();
                RespondToGetActiveTextureRequests();
                RespondToSetActiveTextureRequests();

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
                object[] objects;
                int key;
                lock (newSustainerRequests)
                {
                    key = newSustainerRequests.Keys.First();
                    objects = newSustainerRequests[key];
                    newSustainerRequests.Remove(key);
                }
                SoundDef soundDef = (SoundDef)objects[0];
                SoundInfo soundInfo = (SoundInfo)objects[1];
                Sustainer sustainer = new Sustainer(soundDef, soundInfo);
                newSustainerResults[key] = sustainer;
                
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToPlayRequests()
        {
            while (tryMakeAndPlayRequests.Count > 0)
            {
                object[] objects;
                int key;
                lock (tryMakeAndPlayRequests)
                {
                    key = tryMakeAndPlayRequests.Keys.First();
                    objects = tryMakeAndPlayRequests[key];
                    tryMakeAndPlayRequests.Remove(key);
                }
                SubSustainer subSustainer = (SubSustainer)objects[0];
                AudioClip clip = (AudioClip)objects[1];
                float num2 = (float)objects[2];
                SampleSustainer sampleSustainer = SampleSustainer.TryMakeAndPlay(subSustainer, clip, num2);
                tryMakeAndPlayResults[key] = sampleSustainer;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToMaterialRequests()
        {
            while (materialRequests.Count > 0)
            {
                MaterialRequest materialRequest;
                int key;
                lock (materialRequests)
                {
                    key = materialRequests.Keys.First();
                    materialRequest = materialRequests[key];
                    materialRequests.Remove(key);
                }
                Material material = MaterialPool.MatFrom(materialRequest);
                materialResults[materialRequest] = material;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToTexture2DRequests()
        {
            while (texture2DRequests.Count > 0)
            {
                string itempath;
                int key;
                lock (texture2DRequests)
                {
                    key = texture2DRequests.Keys.First();
                    itempath = texture2DRequests[key];
                    texture2DRequests.Remove(key);
                }
                Texture2D content = ContentFinder_Texture2D_Patch.GetTexture2D(itempath);
                texture2DResults[itempath] = content;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToGenerateMapRequests()
        {
            while (generateMapRequests.Count > 0)
            {
                object[] requestParams;
                int key;
                lock (generateMapRequests)
                {
                    key = generateMapRequests.Keys.First();
                    requestParams = generateMapRequests[key];
                    generateMapRequests.Remove(key);
                }

                timeoutExemptThreads.Add(key);
                Map mapResult = MapGenerator.GenerateMap((IntVec3)requestParams[0], (MapParent)requestParams[1], (MapGeneratorDef)requestParams[2], (IEnumerable<GenStepWithParams>)requestParams[3], (Action<Map>)requestParams[4]);
                generateMapResults[key] = mapResult;

                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToNewAudioSourceRequests()
        {
            while (newAudioSourceRequests.Count > 0)
            {
                GameObject go;
                int key;
                lock (newAudioSourceRequests)
                {
                    key = newAudioSourceRequests.Keys.First();
                    go = newAudioSourceRequests[key];
                    newAudioSourceRequests.Remove(key);
                }
                //timeoutExemptThreads.Add(key);
                AudioSource audioSourceResult = AudioSourceMaker.NewAudioSourceOn(go);
                newAudioSourceResults[key] = audioSourceResult;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToRenderTextureRequests()
        {
            while (renderTextureRequests.Count > 0)
            {
                object[] parameters;
                int key;
                lock (renderTextureRequests)
                {
                    key = renderTextureRequests.Keys.First();
                    parameters = renderTextureRequests[key];
                    renderTextureRequests.Remove(key);
                }
                //timeoutExemptThreads.Add(key);
                RenderTexture renderTextureResult = RenderTexture.GetTemporary((int)parameters[0], (int)parameters[1], (int)parameters[2], (RenderTextureFormat)parameters[3], (RenderTextureReadWrite)parameters[4]);
                renderTextureResults[key] = renderTextureResult;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToSetActiveTextureRequests()
        {
            while (renderTextureSetActiveRequests.Count > 0)
            {
                RenderTexture renderTexture;
                int key;
                lock (renderTextureSetActiveRequests)
                {
                    key = renderTextureSetActiveRequests.Keys.First();
                    renderTexture = renderTextureSetActiveRequests[key];
                    renderTextureSetActiveRequests.Remove(key);
                }
                //timeoutExemptThreads.Add(key);
                RenderTexture.active = renderTexture;
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToGetActiveTextureRequests()
        {
            while (renderTextureGetActiveRequests.Count > 0)
            {
                RenderTexture renderTexture;
                int key;
                lock (renderTextureGetActiveRequests)
                {
                    key = renderTextureGetActiveRequests.Keys.First();
                    renderTexture = renderTextureGetActiveRequests[key];
                    renderTextureGetActiveRequests.Remove(key);
                }
                //timeoutExemptThreads.Add(key);
                RenderTexture renderTextureResult = RenderTexture.active;
                renderTextureResults[key] = renderTextureResult;
                
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToNewBoltMeshRequests()
        {
            while (newBoltMeshRequests.Count > 0)
            {
                if (newBoltMeshRequests.TryDequeue(out int key))
                {
                    Mesh newBoltMeshResult = LightningBoltMeshMaker.NewBoltMesh();
                    newBoltMeshResults[key] = newBoltMeshResult;
                }
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }

        private static void RespondToBlitRequests()
        {
            while (blitRequests.Count > 0)
            {
                object[] parameters;
                int key;
                lock (blitRequests)
                {
                    key = blitRequests.Keys.First();
                    parameters = blitRequests[key];
                    blitRequests.Remove(key);
                }
                Graphics.Blit((Texture)parameters[0], (RenderTexture)parameters[1]);
                if (mainRequestWaits.TryGetValue(key, out EventWaitHandle eventWaitStart))
                    eventWaitStart.Set();
                else
                    Log.Error("Thread " + key.ToString() + " ended during main Thread request.");
            }
        }


    }
}

