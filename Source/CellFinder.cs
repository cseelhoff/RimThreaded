using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class CellFinder_Patch
    {
        [ThreadStatic] public static List<IntVec3> workingCells;
        [ThreadStatic] public static List<Region> workingRegions;
        [ThreadStatic] public static List<int> workingListX;
        [ThreadStatic] public static List<int> workingListZ;
        [ThreadStatic] public static List<IntVec3> mapEdgeCells;
        [ThreadStatic] public static IntVec3 mapEdgeCellsSize;
        [ThreadStatic] public static List<IntVec3>[] mapSingleEdgeCells;
        [ThreadStatic] public static IntVec3 mapSingleEdgeCellsSize;
        [ThreadStatic] public static Dictionary<IntVec3, float> tmpDistances;
        [ThreadStatic] public static Dictionary<IntVec3, IntVec3> tmpParents;
        [ThreadStatic] public static List<IntVec3> tmpCells;
        [ThreadStatic] public static List<Thing> tmpUniqueWipedThings;

        public static void InitializeThreadStatics()
        {
            workingCells = new List<IntVec3>();
            workingRegions = new List<Region>();
            workingListX = new List<int>();
            workingListZ = new List<int>();
            mapSingleEdgeCells = new List<IntVec3>[4];
            tmpDistances = new Dictionary<IntVec3, float>();
            tmpParents = new Dictionary<IntVec3, IntVec3>();
            tmpCells = new List<IntVec3>();
            tmpUniqueWipedThings = new List<Thing>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(CellFinder);
            Type patched = typeof(CellFinder_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "RandomRegionNear");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomReachableCellNear");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomCellInRegion");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomCellNear");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomEdgeCellWith", new Type[] {
                typeof(Predicate<IntVec3>), typeof(Map), typeof(float), typeof(IntVec3).MakeByRefType() });
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomEdgeCellWith", new Type[] {
                typeof(Predicate<IntVec3>), typeof(Map), typeof(Rot4), typeof(float), typeof(IntVec3).MakeByRefType() });
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindBestPawnStandCell");
            RimThreadedHarmony.TranspileFieldReplacements(original, "FindNoWipeSpawnLocNear");
        }

    }
}
