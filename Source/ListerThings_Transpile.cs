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
	public class ListerThings_Transpile
	{
		public static IEnumerable<CodeInstruction> Add(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			//---START EDIT---
			int[] matchesFound = new int[4];
			Type dictionary_ThingDef_List_Thing = typeof(Dictionary<ThingDef, List<Thing>>);
			Type list_Thing = typeof(List<Thing>);
			Type list_ThingArray = typeof(List<Thing>[]);
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
						(MethodInfo)instructionsList[i + 3].operand == Method(dictionary_ThingDef_List_Thing, "TryGetValue")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByDef"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(dictionary_ThingDef_List_Thing);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count) 
					{
						if (
							//---START EDIT---
								instructionsList[i - 1].opcode == OpCodes.Callvirt &&
								(MethodInfo)instructionsList[i - 1].operand == Method(dictionary_ThingDef_List_Thing, "Add")
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
						i + 2 < instructionsList.Count &&
						instructionsList[i].opcode == OpCodes.Ldloc_0 &&
						instructionsList[i + 2].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i + 2].operand == Method(list_Thing, "Add")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldloc_0)
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(list_Thing);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
								//---START EDIT---
								instructionsList[i - 1].opcode == OpCodes.Callvirt &&
								(MethodInfo)instructionsList[i - 1].operand == Method(list_Thing, "Add")
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
					   i + 3 < instructionsList.Count &&
					   instructionsList[i + 1].opcode == OpCodes.Ldfld &&
					   (FieldInfo)instructionsList[i + 1].operand == Field(listerThings, "listsByGroup") &&
					   instructionsList[i + 3].opcode == OpCodes.Ldelem_Ref
				   //---END EDIT---
				   )
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, Field(listerThings, "listsByGroup"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(list_ThingArray);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
							//---START EDIT---
							instructionsList[i - 1].opcode == OpCodes.Stelem_Ref
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
					   i + 2 < instructionsList.Count &&
					   instructionsList[i].opcode == OpCodes.Ldloc_S &&
					   ((LocalBuilder)instructionsList[i].operand).LocalIndex == 4 &&
					   instructionsList[i + 2].opcode == OpCodes.Callvirt &&
					   (MethodInfo)instructionsList[i + 2].operand == Method(list_Thing, "Add")
					//---END EDIT---
				   )
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldloc_S, 4)
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(list_Thing);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
						//---START EDIT---
						instructionsList[i - 1].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i - 1].operand == Method(list_Thing, "Add")
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

		public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			Type loadLockObjectType = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByDef")),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(Thing), "def")),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<ThingDef, List<Thing>>), "get_Item"))
		};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeof(Thing), "Remove")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

			Type loadLockObjectType2 = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByGroup")),
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Ldelem_Ref)
			};
			List<CodeInstruction> searchInstructions2 = loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, Method(loadLockObjectType2, "Remove")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Pop));

			int i = 0;
			int matchesFound = 0;

			while (i < instructionsList.Count)
			{
				if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
				{
					matchesFound++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions, loadLockObjectType))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions2, instructionsList, i))
				{
					matchesFound++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions2.Count, loadLockObjectInstructions2, loadLockObjectType2))
					{
						yield return codeInstruction;
					}
					i += searchInstructions2.Count;
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			if (matchesFound < 1)
			{
				Log.Error("IL code instructions not found");
			}
		}

	}
}
