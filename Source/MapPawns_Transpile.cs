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
	public class MapPawns_Transpile
	{
		public static IEnumerable<CodeInstruction> RegisterPawn(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			Type loadLockObjectType = typeof(List<Pawn>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsSpawned"))
			};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Contains")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Brtrue_S));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsSpawned")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Add")));

			Type loadLockObjectType2 = typeof(Dictionary<Faction, List<Pawn>>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsInFactionSpawned"))

			};
			List<CodeInstruction> searchInstructions2 = new List<CodeInstruction>(); //loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Faction")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Brfalse_S));
			/*
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Faction")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Faction, List<Pawn>>), "get_Item")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Contains")));
			*/
			/*
			searchInstructions2.Add(new CodeInstruction(OpCodes.Brtrue_S));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsInFactionSpawned")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Faction")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "get_Item")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Add")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Faction")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Faction), "get_OfPlayer")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Bne_Un_S));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsInFactionSpawned")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Faction), "get_OfPlayer")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "get_Item")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(AccessTools.TypeByName("Verse.MapPawns+<>c"), "<>9__82_0")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Dup));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Brtrue_S));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Pop));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(AccessTools.TypeByName("Verse.MapPawns+<>c"), "<>9")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(AccessTools.TypeByName("Verse.MapPawns+<>c"), "<RegisterPawn>b__82_0")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Comparison<Pawn>), new Type[] { typeof(object), typeof(int) } )));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Dup));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(AccessTools.TypeByName("Verse.MapPawns+<>c"), "<>9__82_0")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenList), "InsertionSort")));
			*/
			Type loadLockObjectType3 = typeof(List<Pawn>);
			List<CodeInstruction> loadLockObjectInstructions3 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "prisonersOfColonySpawned"))

			};
			List<CodeInstruction> searchInstructions3 = new List<CodeInstruction>();
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Pawn), "get_IsPrisonerOfColony")));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Brfalse_S));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "prisonersOfColonySpawned")));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Contains")));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Brtrue_S));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "prisonersOfColonySpawned")));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Add")));

			int i = 0;
			int[] matchesFound = { 0, 0, 0 };

			while (i < instructionsList.Count)
			{
				if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
				{
					matchesFound[0]++;
					
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions, loadLockObjectType))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
					
					//yield return new CodeInstruction(OpCodes.Ldarg_0);
					//yield return new CodeInstruction(OpCodes.Ldarg_1);
					//yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapPawns_Patch), "RegisterPawn3"));
					//i += searchInstructions.Count;
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions2, instructionsList, i))
				{
					matchesFound[1]++;
					/*
					LocalBuilder lockObject = iLGenerator.DeclareLocal(loadLockObjectType2);
					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					loadLockObjectInstructions2[0].labels = instructionsList[i].labels;
					for (int j = 0; j < loadLockObjectInstructions2.Count; j++)
					{
						yield return (loadLockObjectInstructions2[j]);
					}
					instructionsList[i].labels = new List<Label>();
					yield return (new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
					yield return (new CodeInstruction(OpCodes.Ldc_I4_0));
					yield return (new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
					CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
					codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
					yield return (codeInstruction);
					yield return (new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
					yield return (new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter",
						new Type[] { typeof(object), typeof(bool).MakeByRefType() })));
					//searchInstructions2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenList), "InsertionSort")));
					while (i < instructionsList.Count - 1 && (instructionsList[i + 1].opcode != OpCodes.Callvirt ||
							(MethodInfo)instructionsList[i + 1].operand != AccessTools.Method(typeof(Pawn), "get_IsPrisonerOfColony")))
					{
						yield return (instructionsList[i]);
						i++;
					}
					Label endHandlerDestination = iLGenerator.DefineLabel();
					yield return (new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
					codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
					codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
					yield return (codeInstruction);
					Label endFinallyDestination = iLGenerator.DefineLabel();
					yield return (new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
					yield return (new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
					yield return (new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit")));
					codeInstruction = new CodeInstruction(OpCodes.Endfinally);
					codeInstruction.labels.Add(endFinallyDestination);
					codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
					yield return (codeInstruction);
					instructionsList[i].labels.Add(endHandlerDestination);
					*/
					
					instructionsList[i].opcode = OpCodes.Ldarg_0;
					instructionsList[i].operand = null;
					yield return instructionsList[i];
					//yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapPawns_Patch), "RegisterPawn2"));
					while (i < instructionsList.Count - 1 && (instructionsList[i + 1].opcode != OpCodes.Callvirt ||
						(MethodInfo)instructionsList[i + 1].operand != AccessTools.Method(typeof(Pawn), "get_IsPrisonerOfColony")))
					{
						//yield return (instructionsList[i]);
						i++;
					}
					
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions3, instructionsList, i))
				{
					matchesFound[2]++;
					/*
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions3.Count, loadLockObjectInstructions3, loadLockObjectType3))
					{
						yield return codeInstruction;
					}
					i += searchInstructions3.Count;
					*/
					instructionsList[i].opcode = OpCodes.Ldarg_0;
					instructionsList[i].operand = null;
					yield return instructionsList[i];
					//yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapPawns_Patch), "RegisterPawn3"));
					i += searchInstructions3.Count;
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
				{
					Log.Error("IL code instruction set " + mIndex + " not found");
				}
			}
		}
		public static IEnumerable<CodeInstruction> DeRegisterPawn(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			Type loadLockObjectType = typeof(List<Pawn>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsSpawned"))
			};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Remove")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

			Type loadLockObjectType2 = typeof(Dictionary<Faction, List<Pawn>>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsInFactionSpawned"))

			};
			List<CodeInstruction> searchInstructions2 = new List<CodeInstruction>(); //loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldloc_0));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldloc_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Faction>), "get_Item")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Stloc_2));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "pawnsInFactionSpawned")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldloc_2));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Faction, List<Pawn>>), "get_Item")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Remove")));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Pop));

			Type loadLockObjectType3 = typeof(List<Pawn>);
			List<CodeInstruction> loadLockObjectInstructions3 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapPawns), "prisonersOfColonySpawned"))

			};
			List<CodeInstruction> searchInstructions3 = loadLockObjectInstructions3.ListFullCopy();
			searchInstructions3.Add(new CodeInstruction(OpCodes.Ldarg_1));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "Remove")));
			searchInstructions3.Add(new CodeInstruction(OpCodes.Pop));

			int i = 0;
			int[] matchesFound = { 0, 0, 0 };

			while (i < instructionsList.Count)
			{
				if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
				{
					matchesFound[0]++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions, loadLockObjectType))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions2, instructionsList, i))
				{
					matchesFound[1]++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions2.Count, loadLockObjectInstructions2, loadLockObjectType2))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions3, instructionsList, i))
				{
					matchesFound[2]++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions3.Count, loadLockObjectInstructions3, loadLockObjectType3))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
				{
					Log.Error("IL code instruction set " + mIndex + " not found");
				}
			}
		}
	}
}
