using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Patch_TryOpportunisticJob_Transpile
    {
		public static IEnumerable<CodeInstruction> TryOpportunisticJob(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 3 < instructionsList.Count &&
					instructionsList[i + 3].opcode == OpCodes.Callvirt &&
					instructionsList[i + 3].operand.ToString().Contains("GetValue")
					)
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(Patch_TryOpportunisticJob), "getPawn");
					yield return instructionsList[i++];
					i += 3;
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
