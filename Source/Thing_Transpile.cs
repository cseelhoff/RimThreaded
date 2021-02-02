using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;
using static RimThreaded.RimThreadedHarmony;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
	public class Thing_Transpile
	{
		public static IEnumerable<CodeInstruction> SpawnSetup(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			//---START EDIT---
			int[] matchesFound = new int[1];
			Type listerThings = typeof(ListerThings);
			//---END EDIT---
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
						//---START EDIT---
						i + 3 < instructionsList.Count &&
						instructionsList[i + 3].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i + 3].operand == Method(listerThings, "Add")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, Field(typeof(Map), "listerThings"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(listerThings);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
								//---START EDIT---
								instructionsList[i - 1].opcode == OpCodes.Callvirt &&
								(MethodInfo)instructionsList[i - 1].operand == Method(listerThings, "Add")
						//---END EDIT---
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList, ref i))
						yield return ci;
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
		public static IEnumerable<CodeInstruction> DeSpawn(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			//---START EDIT---
			int[] matchesFound = new int[2];
			Type listerThings = typeof(ListerThings);
			//---END EDIT---
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
						//---START EDIT---
						i + 3 < instructionsList.Count &&
						instructionsList[i + 3].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i + 3].operand == Method(listerThings, "Remove")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldloc_0),
						new CodeInstruction(OpCodes.Ldfld, Field(typeof(Map), "listerThings"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(listerThings);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
							//---START EDIT---
							instructionsList[i - 1].opcode == OpCodes.Callvirt &&
							(MethodInfo)instructionsList[i - 1].operand == Method(listerThings, "Remove")
						//---END EDIT---
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList, ref i))
						yield return ci;
					matchesFound[matchIndex]++;
					continue;
				}
				matchIndex++;
				if (
					//---START EDIT---
					i - 3 > 0 &&
					instructionsList[i - 3].opcode == OpCodes.Ldstr &&
					instructionsList[i - 3].operand.ToString().Equals(" which is already destroyed.")
					//---END EDIT---
					)
				{
					instructionsList[i].operand = Method(typeof(Log), "Warning");
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

		public static IEnumerable<CodeInstruction> get_FlammableNow(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
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
					instructionsList[i - 1].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i - 1].operand == Method(typeof(List<Thing>), "get_Item")
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
