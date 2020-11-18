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
            Type lockObjectType = typeof(Room);
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
            };

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
			CodeInstruction codeInstruction;

			LocalBuilder lockObject = iLGenerator.DeclareLocal(lockObjectType);
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			for (int i = 0; i < loadLockObjectInstructions.Count - 1; i++)
			{
				yield return (loadLockObjectInstructions[i]);
			}
			codeInstruction = loadLockObjectInstructions[loadLockObjectInstructions.Count - 1];
			codeInstruction.labels = instructionsList[currentInstructionIndex].labels;
			instructionsList[currentInstructionIndex].labels = new List<Label>();
			yield return (codeInstruction);
			yield return (new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
			yield return (new CodeInstruction(OpCodes.Ldc_I4_0));
			yield return (new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
			codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			yield return (codeInstruction);
			yield return (new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
			yield return (new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() })));

			while (currentInstructionIndex < instructionsList.Count - 1)
			{
				yield return (instructionsList[currentInstructionIndex]);
				currentInstructionIndex++;
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
			instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
			yield return (instructionsList[currentInstructionIndex]);

		}
    }
}
