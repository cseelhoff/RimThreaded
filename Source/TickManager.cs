using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class TickManager_Patch
    {
        public static AccessTools.FieldRef<TickManager, TimeSpeed> curTimeSpeed =
            AccessTools.FieldRefAccess<TickManager, TimeSpeed>("curTimeSpeed");

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
                    __result = Find.Maps.Count == 0 ? 150f : 30f;
                    return false;
                default:
                    __result = -1f;
                    return false;
            }

        }

    }
}
