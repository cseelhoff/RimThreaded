using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class BFSWorker_Transpile
	{
		public static IEnumerable<CodeInstruction> QueueNewOpenRegion(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			LocalBuilder regionClosedIndex = iLGenerator.DeclareLocal(typeof(uint[]));
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, Method(typeof(BFSWorker_Patch), "getRegionClosedIndex"));
			yield return new CodeInstruction(OpCodes.Stloc, regionClosedIndex.LocalIndex);

			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 1 < instructionsList.Count &&
					instructionsList[i + 1].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i + 1].operand == Field(typeof(Region), "closedIndex")
				)
				{
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = regionClosedIndex.LocalIndex;
					yield return instructionsList[i++];
					i++;
					matchesFound[matchIndex]++;
					continue;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}
		public static IEnumerable<CodeInstruction> BreadthFirstTraverseWork(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(Region), "closedIndex")
				)
				{
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(BFSWorker_Patch), "getRegionClosedIndex");
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
