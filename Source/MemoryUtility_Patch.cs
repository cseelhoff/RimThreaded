using System;
using Verse.Profile;

namespace RimThreaded
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
        }
    }
}
