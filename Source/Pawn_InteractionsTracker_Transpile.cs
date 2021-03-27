using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Threading;
using Verse.AI;
using static RimThreaded.PathFinder_Patch;

namespace RimThreaded
{
    public class Pawn_InteractionsTracker_Transpile
	{
        public static IEnumerable<CodeInstruction> TryInteractRandomly(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			LocalBuilder workingList = iLGenerator.DeclareLocal(typeof(List<Pawn>));

			List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;

			while (i < instructionsList.Count)
			{
				if (instructionsList[i].opcode == OpCodes.Stloc_0) {
					yield return instructionsList[i];
					i++;
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Pawn_InteractionsTracker_Patch), "getWorkingList"));
					yield return new CodeInstruction(OpCodes.Stloc, workingList.LocalIndex);
				}
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(Pawn_InteractionsTracker), "workingList")
					)
				{
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workingList.LocalIndex;
					yield return instructionsList[i];
					i++;
				}
				else
				{
                    yield return instructionsList[i];
					i++;
				}
			}

		}

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(Pawn_InteractionsTracker);
			Type patched = typeof(Pawn_InteractionsTracker_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "TryInteractRandomly");
		}
    }
}
