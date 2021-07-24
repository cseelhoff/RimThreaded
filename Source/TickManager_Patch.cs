using Verse;
using UnityEngine;
using System;

namespace RimThreaded
{
    public class TickManager_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(TickManager);
            Type patched = typeof(TickManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(DoSingleTick));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_TickRateMultiplier));
        }

        public static bool DoSingleTick(TickManager __instance)
        {
            //RimThreaded.currentInstance = __instance;
                        
            if (!DebugSettings.fastEcology)
            {
                __instance.ticksGameInt++;
            }
            else
            {
                __instance.ticksGameInt += 2000;
            }
            Shader.SetGlobalFloat(ShaderPropertyIDs.GameSeconds, __instance.TicksGame.TicksToSeconds());

            RimThreaded.MainThreadWaitLoop(__instance);

            if (DebugViewSettings.logHourlyScreenshot && Find.TickManager.TicksGame >= __instance.lastAutoScreenshot + 2500)
            {
                ScreenshotTaker.QueueSilentScreenshot();
                __instance.lastAutoScreenshot = Find.TickManager.TicksGame / 2500 * 2500;
            }

            Debug.developerConsoleVisible = false;
            return false;
        }


        public static bool get_TickRateMultiplier(TickManager __instance, ref float __result)
        {
            if (__instance.slower.ForcedNormalSpeed && !RimThreadedMod.Settings.disableforcedslowdowns)
            {
                TimeControls_Patch.lastTickForcedSlow = true;
                if (!TimeControls_Patch.overrideForcedSlow)
                {
                    __result = __instance.curTimeSpeed == TimeSpeed.Paused ? 0.0f : 1f;
                    return false;
                }
            }
            else
            {
                TimeControls_Patch.lastTickForcedSlow = false;
                TimeControls_Patch.overrideForcedSlow = false;
            }
            
            switch (__instance.curTimeSpeed)
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
