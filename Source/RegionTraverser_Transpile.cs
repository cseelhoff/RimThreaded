using HarmonyLib;
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
    class RegionTraverser_Transpile
	{
		[ThreadStatic] public static Queue<object> freeWorkers;
		[ThreadStatic] public static int NumWorkers;
		public static void RunNonDestructivePatches()
		{
			Type original = TypeByName("Verse.RegionTraverser+BFSWorker");
			RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(BFSWorker_Patch2), "getRegionClosedIndex"));
			RimThreadedHarmony.TranspileFieldReplacements(original, "QueueNewOpenRegion");
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverseWork");

			original = typeof(RegionTraverser);
			Type patched = typeof(RegionTraverser_Transpile);
			//RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.replaceFields.Add(Field(original, "NumWorkers"), Field(patched, "NumWorkers"));
			RimThreadedHarmony.replaceFields.Add(Field(original, "freeWorkers"), Field(patched, "freeWorkers"));
			/*
				RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverse", new Type[] {
                typeof(Region),
                typeof(RegionEntryPredicate),
                typeof(RegionProcessor),
                typeof(int),
                typeof(RegionType)
            });
			*/
			//RimThreadedHarmony.TranspileFieldReplacements(original, "RecreateWorkers");
			//ConstructorInfo constructorInfo = Constructor(original); // not sure why this doesn't work
			ConstructorInfo constructorInfo = ((ConstructorInfo[])((TypeInfo)original).DeclaredConstructors)[0];
			Log.Message(constructorInfo.ToString());
			//RimThreadedHarmony.harmony.Patch(constructorInfo, transpiler: RimThreadedHarmony.replaceFieldsHarmonyTranspiler);
			RimThreadedHarmony.Transpile(original, patched, "BreadthFirstTraverse", new Type[] {
				typeof(Region),
				typeof(RegionEntryPredicate),
				typeof(RegionProcessor),
				typeof(int),
				typeof(RegionType)
			});
			RimThreadedHarmony.Transpile(original, patched, "RecreateWorkers");


		}
		public static IEnumerable<CodeInstruction> BreadthFirstTraverse(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionTraverser_Transpile), "freeWorkers"));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Ceq);
			Label freeWorkersNullLabel = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, freeWorkersNullLabel);
			yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(Queue<object>)));
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Transpile), "freeWorkers"));
			yield return new CodeInstruction(OpCodes.Ldc_I4_8);
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Transpile), "NumWorkers"));
			yield return new CodeInstruction(OpCodes.Call, Method(typeof(RegionTraverser), "RecreateWorkers"));
			instructionsList[i].labels.Add(freeWorkersNullLabel);
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "freeWorkers")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionTraverser_Transpile), "freeWorkers");
					matchesFound[matchIndex]++;
				}
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "NumWorkers")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionTraverser_Transpile), "NumWorkers");
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
		public static IEnumerable<CodeInstruction> RecreateWorkers(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionTraverser_Transpile), "freeWorkers"));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Ceq);
			Label freeWorkersNullLabel = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, freeWorkersNullLabel);
			yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(Queue<object>)));
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Transpile), "freeWorkers"));
			yield return new CodeInstruction(OpCodes.Ldc_I4_8);
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Transpile), "NumWorkers"));
			instructionsList[i].labels.Add(freeWorkersNullLabel);
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "freeWorkers")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionTraverser_Transpile), "freeWorkers");
					matchesFound[matchIndex]++;
				}
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "NumWorkers")
				)
				{
					instructionsList[i].operand = Field(typeof(RegionTraverser_Transpile), "NumWorkers");
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
