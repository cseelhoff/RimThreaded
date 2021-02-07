using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using Verse;

namespace RimThreaded
{
    class GenClamor_Patch
    {
		public static IEnumerable<CodeInstruction> DoClamorb__1(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			Label breakDestinationLabel = iLGenerator.DefineLabel();
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i+2 < instructionsList.Count &&
					instructionsList[i+2].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i+2].operand == Method(typeof(List<Thing>), "get_Item")
					)
				{
				StartTryAndAddBreakDestinationLabel(instructionsList, ref i, breakDestinationLabel);
				matchesFound[matchIndex]++;
				}
				matchIndex++;
				if (
					i - 2 > 0 &&
					instructionsList[i - 2].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i - 2].operand == Method(typeof(List<Thing>), "get_Item")
				)
				{
					EndTryStartCatchArgumentExceptionOutOfRange(instructionsList, ref i, iLGenerator, breakDestinationLabel);
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
