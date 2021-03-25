using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class HaulAIUtility_Patch
    {
        [ThreadStatic] public static List<IntVec3> candidates;

        public static void InitializeThreadStatics()
        {
            candidates = new List<IntVec3>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(HaulAIUtility);
            Type patched = typeof(HaulAIUtility_Patch);
            RimThreadedHarmony.threadStaticPatches.Add(original, patched);
            RimThreadedHarmony.TranspileThreadStatics(original, "TryFindSpotToPlaceHaulableCloseTo");
        }
    }
}
