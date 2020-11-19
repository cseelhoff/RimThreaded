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

namespace RimThreaded
{
	public class ListerThings_Transpile
	{
		public static IEnumerable<CodeInstruction> Add(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			Type loadLockObjectType = typeof(Dictionary<ThingDef, List<Thing>>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ListerThings), "listsByDef"))
			};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_0));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ThingDef, List<Thing>>), "Add")));

			Type loadLockObjectType2 = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldloc_0)
			};
			List<CodeInstruction> searchInstructions2 = loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(loadLockObjectType2, "Add")));

			Type loadLockObjectType3 = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions3 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldloc_S, 4)
			};
			List<CodeInstruction> searchInstructions3 = loadLockObjectInstructions3.ListFullCopy();
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(loadLockObjectType3, "Add")));

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
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions3, instructionsList, i))
				{
					matchesFound++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions3.Count, loadLockObjectInstructions3, loadLockObjectType3))
					{
						yield return codeInstruction;
					}
					i += searchInstructions3.Count;
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

		public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();

			Type loadLockObjectType = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ListerThings), "listsByDef")),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")),
				new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ThingDef, List<Thing>>), "get_Item"))
		};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "Remove")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

			Type loadLockObjectType2 = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ListerThings), "listsByGroup")),
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Ldelem_Ref)
			};
			List<CodeInstruction> searchInstructions2 = loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(loadLockObjectType2, "Remove")));
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
