using RimWorld;
using System;

namespace RimThreaded
{
    class Plant_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Plant);
            Type patched = typeof(Plant_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(set_Growth));
        }

        public static void set_Growth(Plant __instance, float value)
        {
            if (__instance.HarvestableNow)
                PlantHarvest_Cache.ReregisterObject(__instance.Map, __instance.Position, PlantSowing_Cache.awaitingHarvestCellsMapDict);
        }
    }
}
