using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse.AI;
using static HarmonyLib.AccessTools;

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

        private readonly static Type original = typeof(RegionCostCalculator);
        private readonly static Type patched = typeof(RegionCostCalculator_Patch);

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "PathableNeighborIndices");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetPreciseRegionLinkDistances");
            RimThreadedHarmony.Transpile(original, patched, "GetPreciseRegionLinkDistances");
        }
        public static IEnumerable<CodeInstruction> GetPreciseRegionLinkDistances(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach(CodeInstruction codeInstruction in instructions)
            {
                if(codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(Verse.Dijkstra<int>), "Run", new Type[] {
                        typeof(IEnumerable<int>), typeof(Func<int, IEnumerable<int>>), typeof(Func<int, int, float>), typeof(Dictionary<int, float>), typeof(Dictionary<int, int>)
                    }))
                {
                    codeInstruction.operand = Method(typeof(Dijkstra_Patch<int>), "Run", new Type[] {
                        typeof(IEnumerable<int>), typeof(Func<int, IEnumerable<int>>), typeof(Func<int, int, float>), typeof(Dictionary<int, float>), typeof(Dictionary<int, int>)
                    });
                }
                yield return codeInstruction;
            }
		}

	}
}
