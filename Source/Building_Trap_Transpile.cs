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
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    class Building_Trap_Transpile
    {
		public static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[2]; //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 3 < instructionsList.Count && //EDIT
					instructionsList[i+3].opcode == OpCodes.Isinst
					)
				{
					instructionsList[i].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
					while (i < instructionsList.Count)
					{
						if (instructionsList[i-1].opcode == OpCodes.Stloc_2)
							break;
						yield return instructionsList[i++];
					}
					Label handlerEnd = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Leave_S, handlerEnd);
					CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
					pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
					yield return pop;
					Label exitLoop = iLGenerator.DefineLabel();
					CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Leave, exitLoop);
					codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
					yield return codeInstruction;
					instructionsList[i].labels.Add(handlerEnd);
					instructionsList[i + 23].labels.Add(exitLoop);
					matchesFound[matchIndex]++;
					continue;
				}
				matchIndex++;
				if (
					i + 3 < instructionsList.Count && //EDIT
					instructionsList[i + 3].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i + 3].operand == Method(typeof(List<Pawn>), "get_Item")
					)
				{
					instructionsList[i].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
					while (i < instructionsList.Count)
					{
						if (
							instructionsList[i -2].opcode == OpCodes.Callvirt &&
							(MethodInfo)instructionsList[i -2].operand == Method(typeof(List<Pawn>), "get_Item")
						)
							break;
						yield return instructionsList[i++];
					}
					Label handlerEnd = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Leave_S, handlerEnd);
					CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
					pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
					yield return pop;
					Label exitLoop = iLGenerator.DefineLabel();
					CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Leave, exitLoop);
					codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
					yield return codeInstruction;
					instructionsList[i].labels.Add(handlerEnd);
					instructionsList[i + 23].labels.Add(exitLoop);
					matchesFound[matchIndex]++;
					continue;
				}
				else
				{
					yield return instructionsList[i++];
				}
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}
	}
}
