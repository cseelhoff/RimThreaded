using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Threading;
using System.Diagnostics;

namespace RimThreaded
{

    public class TickManager_Patch
    {
        public static AccessTools.FieldRef<TickManager, TimeSpeed> curTimeSpeed =
            AccessTools.FieldRefAccess<TickManager, TimeSpeed>("curTimeSpeed");
        public static AccessTools.FieldRef<TickManager, int> lastAutoScreenshot =
            AccessTools.FieldRefAccess<TickManager, int>("lastAutoScreenshot");       
        public static AccessTools.FieldRef<TickManager, int> ticksGameInt =
            AccessTools.FieldRefAccess<TickManager, int>("ticksGameInt");
        public static AccessTools.FieldRef<TickManager, TickList> tickListNormal =
            AccessTools.FieldRefAccess<TickManager, TickList>("tickListNormal");
        public static AccessTools.FieldRef<TickManager, TickList> tickListRare =
            AccessTools.FieldRefAccess<TickManager, TickList>("tickListRare");
        public static AccessTools.FieldRef<TickManager, TickList> tickListLong =
            AccessTools.FieldRefAccess<TickManager, TickList>("tickListLong");
        public static Thread monitorHelperThread = null;

        public static Thread mapPreTicksThread = null;
        public static AutoResetEvent mapPreTicksStartEvent;
        public static AutoResetEvent mapPreTicksDoneEvent;
        public static Thread tickListNormalThread = null;
        public static AutoResetEvent tickListNormalStartEvent;
        public static AutoResetEvent tickListNormalDoneEvent;
        public static Thread tickListRareThread = null;
        public static AutoResetEvent tickListRareStartEvent;
        public static AutoResetEvent tickListRareDoneEvent;
        public static Thread tickListLongThread = null;
        public static AutoResetEvent tickListLongStartEvent;
        public static AutoResetEvent tickListLongDoneEvent;

        public static Thread DateNotifierThread = null;
        public static AutoResetEvent DateNotifierStartEvent;
        public static AutoResetEvent DateNotifierDoneEvent;

        public static Thread mapPostTicksThread = null;
        public static AutoResetEvent mapPostTicksStartEvent;
        public static AutoResetEvent mapPostTicksDoneEvent;

        public static Thread WorldThread = null;
        public static AutoResetEvent WorldStartEvent;
        public static AutoResetEvent WorldDoneEvent;

        public static Thread HistoryThread = null;
        public static AutoResetEvent HistoryStartEvent;
        public static AutoResetEvent HistoryDoneEvent;

        public static TickManager currentInstance = null;
        
        public static bool DoSingleTick(TickManager __instance)
        {
            currentInstance = __instance;
            RimThreaded.SingleTickComplete = false;

            if (null == monitorHelperThread)
            {
                monitorHelperThread = new Thread(() => MonitorHelperThreads());
                monitorHelperThread.Start();
            }

            if (null == mapPreTicksThread)
            {
                mapPreTicksThread = new Thread(() => MapPreTicks());
                mapPreTicksStartEvent = new AutoResetEvent(false);
                mapPreTicksDoneEvent = new AutoResetEvent(false);
                mapPreTicksThread.Start();
            }
            mapPreTicksStartEvent.Set();

            if (!DebugSettings.fastEcology)
            {
                ticksGameInt(__instance)++;
            }
            else
            {
                ticksGameInt(__instance) += 2000;
            }

            Shader.SetGlobalFloat(ShaderPropertyIDs.GameSeconds, __instance.TicksGame.TicksToSeconds());
            if (null == tickListNormalThread)
            {
                tickListNormalThread = new Thread(() => tickListNormalTicks());
                tickListNormalStartEvent = new AutoResetEvent(false);
                tickListNormalDoneEvent = new AutoResetEvent(false);
                tickListNormalThread.Start();
            }
            tickListNormalStartEvent.Set();

            if (null == tickListRareThread)
            {
                tickListRareThread = new Thread(() => tickListRareTicks());
                tickListRareStartEvent = new AutoResetEvent(false);
                tickListRareDoneEvent = new AutoResetEvent(false);
                tickListRareThread.Start();
            }
            tickListRareStartEvent.Set();

            if (null == tickListLongThread)
            {
                tickListLongThread = new Thread(() => tickListLongTicks());
                tickListLongStartEvent = new AutoResetEvent(false);
                tickListLongDoneEvent = new AutoResetEvent(false);
                tickListLongThread.Start();
            }
            tickListLongStartEvent.Set();

            if (null == DateNotifierThread)
            {
                DateNotifierThread = new Thread(() => DateNotifierTicks());
                DateNotifierStartEvent = new AutoResetEvent(false);
                DateNotifierDoneEvent = new AutoResetEvent(false);
                DateNotifierThread.Start();
            }
            DateNotifierStartEvent.Set();

            try
            {
                Find.Scenario.TickScenario();
            }
            catch (Exception ex2)
            {
                Log.Error(ex2.ToString());
            }

            if (null == WorldThread)
            {
                WorldThread = new Thread(() => WorldTicks());
                WorldStartEvent = new AutoResetEvent(false);
                WorldDoneEvent = new AutoResetEvent(false);
                WorldThread.Start();
            }
            WorldStartEvent.Set();



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

            if (null == mapPostTicksThread)
            {
                mapPostTicksThread = new Thread(() => MapPostTicks());
                mapPostTicksStartEvent = new AutoResetEvent(false);
                mapPostTicksDoneEvent = new AutoResetEvent(false);
                mapPostTicksThread.Start();
            }
            mapPostTicksStartEvent.Set();


            RimThreaded.MainThreadWaitLoop();

            if (null == HistoryThread)
            {
                HistoryThread = new Thread(() => HistoryTicks());
                HistoryStartEvent = new AutoResetEvent(false);
                HistoryDoneEvent = new AutoResetEvent(false);
                HistoryThread.Start();
            }
            HistoryStartEvent.Set();


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

            if (DebugViewSettings.logHourlyScreenshot && Find.TickManager.TicksGame >= lastAutoScreenshot(__instance) + 2500)
            {
                ScreenshotTaker.QueueSilentScreenshot();
                lastAutoScreenshot(__instance) = Find.TickManager.TicksGame / 2500 * 2500;
            }

            try
            {
                FilthMonitor2.FilthMonitorTick();
            }
            catch (Exception ex13)
            {
                Log.Error(ex13.ToString());
            }

            UnityEngine.Debug.developerConsoleVisible = false;

            return false;
        }
        private static void MonitorHelperThreads()
        {
            while (true)
            {
                mapPreTicksDoneEvent.WaitOne();
                tickListNormalDoneEvent.WaitOne();
                tickListRareDoneEvent.WaitOne();
                tickListLongDoneEvent.WaitOne();
                DateNotifierDoneEvent.WaitOne();
                WorldDoneEvent.WaitOne();
                //once all helper threads are complete...
                RimThreaded.SingleTickComplete = true;
            }
        }

        private static void MapPostTicks()
        {
            while (true)
            {
                mapPostTicksStartEvent.WaitOne();
                List<Map> maps = Find.Maps;
                for (int j = 0; j < maps.Count; j++)
                {
                    maps[j].MapPostTick();
                }
                mapPostTicksDoneEvent.Set();
            }
        }
        private static void MapPreTicks()
        {
            while (true)
            {
                mapPreTicksStartEvent.WaitOne();
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    maps[i].MapPreTick();
                }
                mapPreTicksDoneEvent.Set();
            }
        }
        private static void tickListNormalTicks()
        {
            while (true)
            {
                tickListNormalStartEvent.WaitOne();
                tickListNormal(currentInstance).Tick();
                tickListNormalDoneEvent.Set();
            }
        }
        private static void tickListRareTicks()
        {
            while (true)
            {
                tickListRareStartEvent.WaitOne();
                tickListRare(currentInstance).Tick();
                tickListRareDoneEvent.Set();
            }
        }
        private static void tickListLongTicks()
        {
            while (true)
            {
                tickListLongStartEvent.WaitOne();
                tickListLong(currentInstance).Tick();
                tickListLongDoneEvent.Set();
            }
        }
        private static void DateNotifierTicks()
        {
            while (true)
            {
                DateNotifierStartEvent.WaitOne();
                try
                {
                    Find.DateNotifier.DateNotifierTick();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                DateNotifierDoneEvent.Set();
            }
        }
        private static void WorldTicks()
        {
            while (true)
            {
                WorldStartEvent.WaitOne();
                try
                {
                    Find.World.WorldTick();
                }
                catch (Exception ex3)
                {
                    Log.Error(ex3.ToString());
                }
                WorldDoneEvent.Set();
            }
        }
        private static void HistoryTicks()
        {
            while (true)
            {
                HistoryStartEvent.WaitOne();
                try
                {
                    Find.History.HistoryTick();
                }
                catch (Exception ex10)
                {
                    Log.Error(ex10.ToString());
                }
                HistoryDoneEvent.Set();
            }
        }

        public static bool get_TickRateMultiplier(TickManager __instance, ref float __result)
        {
            if (__instance.slower.ForcedNormalSpeed)
            {
                __result = curTimeSpeed(__instance) == TimeSpeed.Paused ? 0.0f : 1f;
                return false;
            }
            switch (curTimeSpeed(__instance))
            {
                case TimeSpeed.Paused:
                    __result = 0.0f;
                    return false;
                case TimeSpeed.Normal:
                    __result = 1f;
                    return false;
                case TimeSpeed.Fast:
                    __result = 3f;
                    return false;
                case TimeSpeed.Superfast:
                    if (Find.Maps.Count == 0)
                    {
                        __result = 120f;
                        return false;
                    }
                    __result = 12f;
                    return false;
                case TimeSpeed.Ultrafast:
                    __result = Find.Maps.Count == 0 ? 150f : 150f;
                    return false;
                default:
                    __result = -1f;
                    return false;
            }

        }

    }
}
