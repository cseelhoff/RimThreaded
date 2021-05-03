using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class WorldSelectionDrawer_Patch
    {
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(WorldSelectionDrawer);
            Type patched = typeof(WorldSelectionDrawer_Patch);
            RimThreadedHarmony.Transpile(original, patched, "DrawSelectionBracketOnGUIFor");
        }
        public static IEnumerable<CodeInstruction> DrawSelectionBracketOnGUIFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(SelectionDrawerUtility), "CalculateSelectionBracketPositionsUI").MakeGenericMethod(typeof(WorldObject)))
                {
                    codeInstruction.operand = Method(typeof(SelectionDrawerUtility_Patch), "CalculateSelectionBracketPositionsUI").MakeGenericMethod(typeof(WorldObject));
                }
                yield return codeInstruction;
            }
        }
    }
}
