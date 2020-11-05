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

        public static bool DoSingleTick(TickManager __instance)
        {
            RimThreaded.currentInstance = __instance;
                        
            if (!DebugSettings.fastEcology)
            {
                ticksGameInt(__instance)++;
            }
            else
            {
                ticksGameInt(__instance) += 2000;
            }
            Shader.SetGlobalFloat(ShaderPropertyIDs.GameSeconds, __instance.TicksGame.TicksToSeconds());

            RimThreaded.MainThreadWaitLoop();

            UnityEngine.Debug.developerConsoleVisible = false;
            if (DebugViewSettings.logHourlyScreenshot && Find.TickManager.TicksGame >= lastAutoScreenshot(__instance) + 2500)
            {
                ScreenshotTaker.QueueSilentScreenshot();
                lastAutoScreenshot(__instance) = Find.TickManager.TicksGame / 2500 * 2500;
            }
            return false;
        }


        public static bool get_TickRateMultiplier(TickManager __instance, ref float __result)
        {
            //if (__instance.slower.ForcedNormalSpeed)
            //{
                //__result = curTimeSpeed(__instance) == TimeSpeed.Paused ? 0.0f : 1f;
                //return false;
            //}
            switch (curTimeSpeed(__instance))
            {
                case TimeSpeed.Paused:
                    __result = 0.0f;
                    return false;
                case TimeSpeed.Normal:
                    __result = RimThreaded.timeSpeedNormal;
                    return false;
                case TimeSpeed.Fast:
                    __result = RimThreaded.timeSpeedFast;
                    return false;
                case TimeSpeed.Superfast:
                    __result = Find.Maps.Count == 0 ? RimThreaded.timeSpeedSuperfast : RimThreaded.timeSpeedSuperfast;
                    return false;
                case TimeSpeed.Ultrafast:
                    __result = Find.Maps.Count == 0 ? RimThreaded.timeSpeedUltrafast : RimThreaded.timeSpeedUltrafast;
                    return false;
                default:
                    __result = -1f;
                    return false;
            }

        }

    }
}
