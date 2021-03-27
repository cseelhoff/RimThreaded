using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class ThinkNode_PrioritySorter_Transpile
    {
		public static IEnumerable<CodeInstruction> TryIssueJobPackage(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(ThinkNode_PrioritySorter_Patch), "workingNodes"));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Ceq);
			Label workingNodesNullLabel = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, workingNodesNullLabel);
			yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(List<ThinkNode>)));
			yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(ThinkNode_PrioritySorter_Patch), "workingNodes"));
			CodeInstruction ci = new CodeInstruction(OpCodes.Ldsfld, Field(typeof(ThinkNode_PrioritySorter_Patch), "workingNodes"));
			ci.labels.Add(workingNodesNullLabel);
			yield return ci;
			yield return new CodeInstruction(OpCodes.Callvirt, Method(typeof(List<ThinkNode>), "Clear"));

			instructionsList[i].labels.Add(workingNodesNullLabel);
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(ThinkNode_PrioritySorter), "workingNodes")
				)
				{
					instructionsList[i].operand = Field(typeof(ThinkNode_PrioritySorter_Patch), "workingNodes");
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

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(ThinkNode_PrioritySorter);
			Type patched = typeof(ThinkNode_PrioritySorter_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "TryIssueJobPackage");
		}
    }
}
