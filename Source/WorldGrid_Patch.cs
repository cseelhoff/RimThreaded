using RimWorld.Planet;
using System;
using System.Collections.Generic;

namespace RimThreaded
{

    public class WorldGrid_Patch
    {
        [ThreadStatic] public static List<int> tmpNeighbors;

        internal static void InitializeThreads()
        {
            tmpNeighbors = new List<int>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(WorldGrid);
            Type patched = typeof(WorldGrid_Patch);

            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "IsNeighbor");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetNeighborId");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetTileNeighbor");
            RimThreadedHarmony.TranspileFieldReplacements(original, "FindMostReasonableAdjacentTileForDisplayedPathCost");
        }

    }
}
