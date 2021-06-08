using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded
{
    public class Toils_Ingest_Patch
    {
        [ThreadStatic] public static List<IntVec3> cardinals;
        [ThreadStatic] public static List<IntVec3> diagonals;

        internal static void InitializeThreadStatics()
        {
            cardinals = GenAdj.CardinalDirections.ToList();
            diagonals = GenAdj.DiagonalDirections.ToList();
        }

    }
}
