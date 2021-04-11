using System;
using System.Collections.Generic;
using Verse.AI;

namespace RimThreaded
{

    public class RegionCostCalculator_Patch
    {
        [ThreadStatic] public static List<int> tmpPathableNeighborIndices;
        [ThreadStatic] public static Dictionary<int, float> tmpDistances;
        [ThreadStatic] public static List<int> tmpCellIndices;

        internal static void InitializeThreadStatics()
        {
            tmpPathableNeighborIndices = new List<int>();
            tmpDistances = new Dictionary<int, float>();
            tmpCellIndices = new List<int>();
        }

        readonly static Type original = typeof(RegionCostCalculator);
        readonly static Type patched = typeof(RegionCostCalculator_Patch);

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "PathableNeighborIndices");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetPreciseRegionLinkDistances");
        }

    }
}
