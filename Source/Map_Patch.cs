using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class Map_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Map);
            Type patched = typeof(Map_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_IsPlayerHome");
        }

        public static bool get_IsPlayerHome(Map __instance, ref bool __result)
        {
            if (__instance.info != null && __instance.info.parent != null && __instance.info.parent.def != null && __instance.info.parent.def.canBePlayerHome)
            {
                __result = __instance.info.parent.Faction == Faction.OfPlayer;
                return false;
            }
            __result = false;
            return false;

        }

    }
}
