using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class InfestationCellFinder_Patch
    {
        public struct LocationCandidate
        {
            public IntVec3 cell;

            public float score;

            public LocationCandidate(IntVec3 cell, float score)
            {
                this.cell = cell;
                this.score = score;
            }
        }
        [ThreadStatic] public static Dictionary<Region, float> regionsDistanceToUnroofed;
        [ThreadStatic] public static List<IntVec3> tmpColonyBuildingsLocs;
        [ThreadStatic] public static List<KeyValuePair<IntVec3, float>> tmpDistanceResult;
        [ThreadStatic] public static ByteGrid distToColonyBuilding;
        [ThreadStatic] public static ByteGrid closedAreaSize;
        [ThreadStatic] public static List<Pair<IntVec3, float>> tmpCachedInfestationChanceCellColors;
        [ThreadStatic] public static HashSet<Region> tempUnroofedRegions;
        [ThreadStatic] public static List<LocationCandidate> locationCandidates;

        public static void InitializeThreadStatics()
        {
            tmpColonyBuildingsLocs = new List<IntVec3>();
            tmpDistanceResult = new List<KeyValuePair<IntVec3, float>>();
            tmpCachedInfestationChanceCellColors = new List<Pair<IntVec3, float>>();
            tempUnroofedRegions = new HashSet<Region>();
            locationCandidates = new List<LocationCandidate>();
            regionsDistanceToUnroofed = new Dictionary<Region, float>();
        }

        static readonly Type original = typeof(InfestationCellFinder);
        static readonly Type patched = typeof(InfestationCellFinder_Patch);
        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetScoreAt");
            RimThreadedHarmony.TranspileFieldReplacements(original, "DebugDraw");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateLocationCandidates");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateClosedAreaSizeGrid");
            RimThreadedHarmony.TranspileFieldReplacements(
                TypeByName("RimWorld.InfestationCellFinder+<>c__DisplayClass25_1"),
                "<CalculateClosedAreaSizeGrid>b__3");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateTraversalDistancesToUnroofed");
            RimThreadedHarmony.Transpile(original, patched, "CalculateTraversalDistancesToUnroofed");

            RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateDistanceToColonyBuildingGrid");
            RimThreadedHarmony.Transpile(original, patched, "CalculateDistanceToColonyBuildingGrid");

        }


        public static IEnumerable<CodeInstruction> CalculateTraversalDistancesToUnroofed(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(Dijkstra<Region>), "Run", new Type[] {
                        typeof(IEnumerable<Region>), typeof(Func<Region, IEnumerable<Region>>), typeof(Func<Region, Region, float>), typeof(Dictionary<Region, float>), typeof(Dictionary<Region, Region>)
                    }))
                {
                    codeInstruction.operand = Method(typeof(Dijkstra_Patch<Region>), "Run", new Type[] {
                        typeof(IEnumerable<Region>), typeof(Func<Region, IEnumerable<Region>>), typeof(Func<Region, Region, float>), typeof(Dictionary<Region, float>), typeof(Dictionary<Region, Region>)
                    });
                }
                yield return codeInstruction;
            }
        }

        public static IEnumerable<CodeInstruction> CalculateDistanceToColonyBuildingGrid(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(Dijkstra<IntVec3>), "Run", new Type[] {
                        typeof(IEnumerable<IntVec3>), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(List<KeyValuePair<IntVec3, float>>), typeof(Dictionary <IntVec3, IntVec3>)
                    }))
                {
                    codeInstruction.operand = Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new Type[] {
                        typeof(IEnumerable<IntVec3>), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(List<KeyValuePair<IntVec3, float>>), typeof(Dictionary <IntVec3, IntVec3>)
                    });
                }
                yield return codeInstruction;
            }
        }

    }
}
