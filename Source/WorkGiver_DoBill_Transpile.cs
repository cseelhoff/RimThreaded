using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class WorkGiver_DoBill_Transpile
    {
		public static IEnumerable<CodeInstruction> TryFindBestBillIngredients(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[8]; //EDIT
			
			List<CodeInstruction> instructionsList = instructions.ToList();

			LocalBuilder workGiver_DoBill_RegionProcessor = iLGenerator.DeclareLocal(typeof(WorkGiver_DoBill_RegionProcessor));

			yield return new CodeInstruction(OpCodes.Newobj, typeof(WorkGiver_DoBill_RegionProcessor).GetConstructor(Type.EmptyTypes));
			yield return new CodeInstruction(OpCodes.Stloc, workGiver_DoBill_RegionProcessor.LocalIndex);
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					(FieldInfo)instructionsList[i].operand == Field(typeof(WorkGiver_DoBill), "newRelevantThings") //EDIT
					)
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "newRelevantThings"));
					continue;
				}
				matchIndex++;
				if (
					 instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					 (FieldInfo)instructionsList[i].operand == Field(typeof(WorkGiver_DoBill), "relevantThings") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "relevantThings"));
					continue;
				}
				matchIndex++;
				if (
					 instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					 (FieldInfo)instructionsList[i].operand == Field(typeof(WorkGiver_DoBill), "processedThings") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "processedThings"));
					continue;
				}
				matchIndex++;
				if (
					 instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					 (FieldInfo)instructionsList[i].operand == Field(typeof(WorkGiver_DoBill), "ingredientsOrdered") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "ingredientsOrdered"));
					continue;
				}
				matchIndex++;
				if (
					 instructionsList[i].opcode == OpCodes.Call && //EDIT
					 (MethodInfo)instructionsList[i].operand == Method(typeof(WorkGiver_DoBill), "AddEveryMedicineToRelevantThings") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].operand = Method(typeof(WorkGiver_DoBill_Patch), "AddEveryMedicineToRelevantThings2");
					yield return instructionsList[i++];
					continue;
				}
				matchIndex++;
				if (
					 instructionsList[i].opcode == OpCodes.Call && //EDIT
					 (MethodInfo)instructionsList[i].operand == Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "ingredientsOrdered"));
					instructionsList[i].operand = Method(typeof(WorkGiver_DoBill_Patch), "TryFindBestBillIngredientsInSet2");
					yield return instructionsList[i++];
					continue;
				}
				matchIndex++;
				if (
					i + 1 < instructionsList.Count &&
					 instructionsList[i+1].opcode == OpCodes.Ldftn && //EDIT
					 (MethodInfo)instructionsList[i+1].operand == Method(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "<TryFindBestBillIngredients>b__3") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "bill"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "pawn"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "baseValidator"));
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "baseValidator"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "billGiverIsPawn"));
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "billGiverIsPawn"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "adjacentRegionsAvailable"));
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "adjacentRegionsAvailable"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "rootCell"));
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "rootCell"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldfld, Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "chosen"));
					yield return new CodeInstruction(OpCodes.Stfld, Field(typeof(WorkGiver_DoBill_RegionProcessor), "chosen"));
					yield return new CodeInstruction(OpCodes.Ldloc, workGiver_DoBill_RegionProcessor.LocalIndex);
					instructionsList[i].operand = Method(typeof(WorkGiver_DoBill_RegionProcessor), "Get_RegionProcessor");
					yield return instructionsList[i++];
					yield return instructionsList[i++];
					yield return instructionsList[i++];
					continue;
				}
				matchIndex++;
				if (i + 1 < instructionsList.Count &&
					 instructionsList[i+1].opcode == OpCodes.Ldfld && //EDIT
					 (FieldInfo)instructionsList[i+1].operand == Field(TypeByName("RimWorld.WorkGiver_DoBill+<>c__DisplayClass20_0"), "foundAll") //EDIT
					 )
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = workGiver_DoBill_RegionProcessor.LocalIndex;
					yield return instructionsList[i++];
					instructionsList[i].operand = Field(typeof(WorkGiver_DoBill_RegionProcessor), "foundAll");
					yield return instructionsList[i++];
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

		public static IEnumerable<CodeInstruction> AddEveryMedicineToRelevantThings(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			LocalBuilder tmpMedicine = iLGenerator.DeclareLocal(typeof(List<Thing>));

			yield return new CodeInstruction(OpCodes.Newobj, typeof(List<Thing>).GetConstructor(Type.EmptyTypes));
			yield return new CodeInstruction(OpCodes.Stloc, tmpMedicine.LocalIndex);
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					(FieldInfo)instructionsList[i].operand == Field(typeof(WorkGiver_DoBill), "tmpMedicine") //EDIT
					)
				{
					matchesFound[matchIndex]++;
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = tmpMedicine.LocalIndex;
					yield return instructionsList[i++];
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

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(WorkGiver_DoBill);
			Type patched = typeof(WorkGiver_DoBill_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "TryFindBestBillIngredients");
			RimThreadedHarmony.Transpile(original, patched, "AddEveryMedicineToRelevantThings");
		}
    }
}
