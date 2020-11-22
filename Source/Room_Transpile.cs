using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using System.Threading;

namespace RimThreaded
{
    public class Room_Transpile
    {
        public static IEnumerable<CodeInstruction> RemoveRegion(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type lockObjectType = typeof(object);

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;

			LocalBuilder lockObject = iLGenerator.DeclareLocal(lockObjectType);
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));

			yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Room_Patch), "roomLock"));
			yield return new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex);
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			yield return codeInstruction;
			yield return new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() }));

			while (currentInstructionIndex < instructionsList.Count - 1)
			{
				yield return instructionsList[currentInstructionIndex];
				currentInstructionIndex++;
			}
			Label endHandlerDestination = iLGenerator.DefineLabel();
			yield return (new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
			CodeInstruction codeInstruction2 = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
			codeInstruction2.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
			yield return (codeInstruction2);
			Label endFinallyDestination = iLGenerator.DefineLabel();
			yield return (new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
			yield return (new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
			yield return (new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit")));
			CodeInstruction codeInstruction3 = new CodeInstruction(OpCodes.Endfinally);
			codeInstruction3.labels.Add(endFinallyDestination);
			codeInstruction3.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			yield return (codeInstruction3);
			instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
			yield return (instructionsList[currentInstructionIndex]);

		}
    }
}
