using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading;
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
        public static int mapPostThreads;

        public static void MapsPostTickPrepare()
        {
            try
            {
                List<Map> maps = Find.Maps;
                for (int j = 0; j < maps.Count; j++)
                {
                    maps[j].MapPostTick();
                }
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }

            mapPostThreads = -1;
        }

        public static bool MapPostListTick()
        {
            int threadIndex = Interlocked.Increment(ref mapPostThreads);
            SteadyEnvironmentEffects_Patch.SteadyEffectTick();
            WildPlantSpawner_Patch.WildPlantSpawnerListTick();
            TradeShip_Patch.PassingShipListTick();
            return threadIndex == 0;
        }
    }
}
