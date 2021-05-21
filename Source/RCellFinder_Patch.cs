using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class RCellFinder_Patch
    {
        [ThreadStatic] public static List<Region> regions;
        [ThreadStatic] public static HashSet<Thing> tmpBuildings;
        [ThreadStatic] public static List<Thing> tmpSpotThings;
        [ThreadStatic] public static List<IntVec3> tmpSpotsToAvoid;
        [ThreadStatic] public static List<IntVec3> tmpEdgeCells;

        public static void InitializeThreadStatics()
        {
            regions = new List<Region>();
            tmpBuildings = new HashSet<Thing>();
            tmpSpotThings = new List<Thing>();
            tmpSpotsToAvoid = new List<IntVec3>();
            tmpEdgeCells = new List<IntVec3>();
        }

        public static void RunNonDestructivePatches() {
            Type original = typeof(RCellFinder);
            Type patched = typeof(RCellFinder_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "RandomWanderDestFor");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomSpotJustOutsideColony", new Type[] {
                typeof(IntVec3), typeof(Map), typeof(Pawn), typeof(IntVec3).MakeByRefType(), typeof(Predicate<IntVec3>)});
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindGatheringSpot_NewTemp");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindEdgeCellWithPathToPositionAvoidingColony");
            RimThreadedHarmony.TranspileFieldReplacements(original, "FindBestAngleAvoidingSpots");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomSpotNearAvoidingHostilePawns");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindEdgeCellFromTargetAvoidingColony");
        }

    }
}
