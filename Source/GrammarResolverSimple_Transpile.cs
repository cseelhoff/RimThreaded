using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;

namespace RimThreaded
{
	public class GrammarResolverSimple_Transpile
	{
		internal static void RunNonDestructivePatches()
		{
			Type original = typeof(GrammarResolverSimple);
			Type patched = typeof(GrammarResolverSimple_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "Formatted");
		}
		public static IEnumerable<CodeInstruction> Formatted(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			Type loadLockObjectType = typeof(List<string>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_1),
			};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Enumerable), "ToList").MakeGenericMethod(typeof(string))));
			searchInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, 5));

			Type loadLockObjectType2 = typeof(List<object>);
			List<CodeInstruction> loadLockObjectInstructions2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_2),
			};
			List<CodeInstruction> searchInstructions2 = loadLockObjectInstructions2.ListFullCopy();
			searchInstructions2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Enumerable), "ToList").MakeGenericMethod(typeof(object))));
			searchInstructions2.Add(new CodeInstruction(OpCodes.Stloc_S, 6));

			/* Original PathFinder.FindPath
			
			---START ORIG---	IL_000b: ldsfld bool Verse.GrammarResolverSimple::working
			
			---END ORIG---

			---START TARGET---
			IL_0000: ldc-i4-1
			---END TARGET---


			*/
			//LocalBuilder tmpCells = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
			int i = 0;
			//yield return new CodeInstruction(OpCodes.Newobj, typeof(List<IntVec3>).GetConstructor(Type.EmptyTypes));
			//yield return new CodeInstruction(OpCodes.Stloc_S, tmpCells.LocalIndex);
			int matchesFound = 0;
			
			while (i < instructionsList.Count)
			{
				//---START ORIG-- -
				//	IL_000b: ldsfld bool Verse.GrammarResolverSimple::working
				//-- - END ORIG-- -

				//---START TARGET-- -
				//IL_0000: Ldc_I4_0
				//-- - END TARGET-- -
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(GrammarResolverSimple), "working")
					)
				{
					instructionsList[i].opcode = OpCodes.Ldc_I4_1;
					instructionsList[i].operand = null;
					yield return instructionsList[i];
					i++;
				}
				else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
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
					i += searchInstructions.Count;
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			if (matchesFound < 2)
			{
				Log.Error("IL code instructions not found");
			}
		}

    }
}
