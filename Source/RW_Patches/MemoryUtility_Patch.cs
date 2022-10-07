using System;
using Verse.Profile;

namespace RimThreaded.RW_Patches
{
    class MemoryUtility_Patch
    {
        private static readonly Type Original = typeof(MemoryUtility);
        private static readonly Type Patched = typeof(MemoryUtility_Patch);

        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(Original, Patched, "ClearAllMapsAndWorld");
        }

        public static void ClearAllMapsAndWorld()
        {
            //GenTemperature_Patch.SeasonalShiftAmplitudeCache.Clear();
            RimThreaded.billFreeColonistsSpawned.Clear();
            RimThreaded.plantHarvest_Cache.positionsAwaitingAction.Clear();
            RimThreaded.plantSowing_Cache.positionsAwaitingAction.Clear();
            HaulingCache.waitingForZoneBetterThanMapDict.Clear();
            HaulingCache.awaitingHaulingMapDict.Clear();
            ListerThings_Patch.mapToGroupToZoomsToGridToThings.Clear();
            WorkGiver_Grower_Patch.awaitingPlantCellsMapDict.Clear();
        }
    }
}
