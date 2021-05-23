using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded
{
    public class Toils_Ingest_Patch
    {
        [ThreadStatic] public static List<IntVec3> spotSearchList;
        [ThreadStatic] public static List<IntVec3> cardinals;
        [ThreadStatic] public static List<IntVec3> diagonals;

        internal static void InitializeThreadStatics()
        {
            spotSearchList = new List<IntVec3>();
            cardinals = GenAdj.CardinalDirections.ToList();
            diagonals = GenAdj.DiagonalDirections.ToList();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Toils_Ingest);
            Type patched = typeof(Toils_Ingest_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindAdjacentIngestionPlaceSpot");
        }

    }
}
