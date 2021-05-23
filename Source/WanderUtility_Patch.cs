using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class WanderUtility_Patch
	{
        [ThreadStatic] public static List<IntVec3> gatherSpots;
        [ThreadStatic] public static List<IntVec3> candidateCells;
        [ThreadStatic] public static List<Building> candidateBuildingsInRandomOrder;

        public static void InitializeThreadStatics()
        {
            gatherSpots = new List<IntVec3>();
            candidateCells = new List<IntVec3>();
            candidateBuildingsInRandomOrder = new List<Building>();
        }

        internal static void RunNoneDestructivePatches()
        {
            Type original = typeof(WanderUtility);
            Type patched = typeof(WanderUtility_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetColonyWanderRoot");
        }

    }
}
