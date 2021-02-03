using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    class RegionListersUpdater_Transpile
	{
		public static IEnumerable<CodeInstruction> DeregisterInRegions(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionListersUpdater_Patch), "tmpRegions"));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Ceq);
			Label tmpRegionNullLabel = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, tmpRegionNullLabel);
			yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(List<Region>)));
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionListersUpdater_Patch), "tmpRegions"));
			instructionsList[i].labels.Add(tmpRegionNullLabel);
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == Method(typeof(ListerThings), "Remove")
				)
				{
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(RegionListersUpdater_Patch), "lockAndRemove");
					yield return instructionsList[i++];
					matchesFound[matchIndex]++;
					continue;
				}
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionListersUpdater), "tmpRegions")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionListersUpdater_Patch), "tmpRegions");
					yield return instructionsList[i++];
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
		public static IEnumerable<CodeInstruction> RegisterInRegions(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionListersUpdater_Patch), "tmpRegions"));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Ceq);
			Label tmpRegionNullLabel = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, tmpRegionNullLabel);
			yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(List<Region>)));
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionListersUpdater_Patch), "tmpRegions"));
			instructionsList[i].labels.Add(tmpRegionNullLabel);
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == Method(typeof(ListerThings), "Add")
				)
				{
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(RegionListersUpdater_Patch), "lockAndAdd");
					yield return instructionsList[i++];
					matchesFound[matchIndex]++;
					continue;
				}
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionListersUpdater), "tmpRegions")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionListersUpdater_Patch), "tmpRegions");
					yield return instructionsList[i++];
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

		public static IEnumerable<CodeInstruction> RegisterAllAt(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			Label breakDestinationLabel = iLGenerator.DefineLabel();
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 2 < instructionsList.Count &&
					instructionsList[i + 2].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i + 2].operand == Method(typeof(List<Thing>), "get_Item")
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
