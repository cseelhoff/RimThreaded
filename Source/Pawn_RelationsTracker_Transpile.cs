using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using System.Reflection;
using RimWorld;
using Verse;

namespace RimThreaded
{
	class Pawn_RelationsTracker_Transpile
	{
		public static IEnumerable<CodeInstruction> ReplacePotentiallyRelatedPawns(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					(instructionsList[i].opcode == OpCodes.Callvirt || instructionsList[i].opcode == OpCodes.Call) &&
					(MethodInfo)instructionsList[i].operand == Method(typeof(Pawn_RelationsTracker), "get_PotentiallyRelatedPawns")
					)
				{
					instructionsList[i].operand = Method(typeof(Pawn_RelationsTracker_Patch), "get_PotentiallyRelatedPawns2");
					matchesFound[matchIndex]++;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}
	}
}
