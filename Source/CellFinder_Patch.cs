using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class CellFinder_Patch
    {

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(CellFinder);
            Type patched = typeof(CellFinder_Patch);
            RimThreadedHarmony.Transpile(original, patched, "TryFindBestPawnStandCell");

        }
        public static IEnumerable<CodeInstruction> TryFindBestPawnStandCell(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(Dijkstra<IntVec3>), "Run", new Type[] {
                        typeof(IntVec3), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(Dictionary < IntVec3, float >), typeof(Dictionary<IntVec3, IntVec3>)
                    }))
                {
                    codeInstruction.operand = Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new Type[] {
                        typeof(IntVec3), typeof(Func<IntVec3, IEnumerable<IntVec3>>), typeof(Func<IntVec3, IntVec3, float>), typeof(Dictionary < IntVec3, float >), typeof(Dictionary<IntVec3, IntVec3>)
                    });
                }
                yield return codeInstruction;
            }
        }


    }
}
