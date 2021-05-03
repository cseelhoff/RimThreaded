using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class ColonistBarColonistDrawer_Patch
    {
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(ColonistBarColonistDrawer);
            Type patched = typeof(ColonistBarColonistDrawer_Patch);
            RimThreadedHarmony.Transpile(original, patched, "DrawSelectionOverlayOnGUI", new [] {typeof(Pawn), typeof(Rect)});
            RimThreadedHarmony.Transpile(original, patched, "DrawCaravanSelectionOverlayOnGUI");
        }
        public static IEnumerable<CodeInstruction> DrawSelectionOverlayOnGUI(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo && methodInfo ==
                    Method(typeof(SelectionDrawerUtility), "CalculateSelectionBracketPositionsUI").MakeGenericMethod(typeof(object)))
                {
                    codeInstruction.operand = Method(typeof(SelectionDrawerUtility_Patch), "CalculateSelectionBracketPositionsUI").MakeGenericMethod(typeof(object));
                }
                yield return codeInstruction;
            }
        }
        public static IEnumerable<CodeInstruction> DrawCaravanSelectionOverlayOnGUI(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
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
