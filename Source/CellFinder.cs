using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

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
            RimThreadedHarmony.TranspileFieldReplacements(TypeByName("Verse.CellFinder+<>c"), "<RandomRegionNear>b__15_1");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomReachableCellNear");
            RimThreadedHarmony.TranspileFieldReplacements(TypeByName("Verse.CellFinder+<>c"), "<TryFindRandomReachableCellNear>b__17_1");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomCellInRegion");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomCellNear");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomEdgeCellWith", new Type[] {
                typeof(Predicate<IntVec3>), typeof(Map), typeof(float), typeof(IntVec3).MakeByRefType() });
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomEdgeCellWith", new Type[] {
                typeof(Predicate<IntVec3>), typeof(Map), typeof(Rot4), typeof(float), typeof(IntVec3).MakeByRefType() });
            RimThreadedHarmony.TranspileFieldReplacements(original, "FindNoWipeSpawnLocNear");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindBestPawnStandCell");
            RimThreadedHarmony.Transpile(original, patched, "TryFindBestPawnStandCell");

        }
        public static IEnumerable<CodeInstruction> TryFindBestPawnStandCell(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(Dijkstra<Region>), "Run", new Type[] {
                        typeof(IntVec3), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(Dictionary < IntVec3, float >), typeof(Dictionary<IntVec3, IntVec3>)
                    }))
                {
                    codeInstruction.operand = Method(typeof(Dijkstra_Patch<Region>), "Run", new Type[] {
                        typeof(IntVec3), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(Dictionary < IntVec3, float >), typeof(Dictionary<IntVec3, IntVec3>)
                    });
                }
                yield return codeInstruction;
            }
        }


    }
}
