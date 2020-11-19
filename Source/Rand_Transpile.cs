using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class Rand_Transpile
    {

        public static IEnumerable<CodeInstruction> PushState(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Stack<UInt64>);
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Rand), "stateStack")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Rand), "get_StateCompressed")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeDictionaryIntData, "Push")));

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count, loadLockObjectInstructions, typeDictionaryIntData))
                    {
                        yield return codeInstruction;
                    }
                    currentInstructionIndex += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> PopState(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Stack<UInt64>);
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Rand), "stateStack")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeDictionaryIntData, "Pop")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Rand), "set_StateCompressed")));

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count, loadLockObjectInstructions, typeDictionaryIntData))
                    {
                        yield return codeInstruction;
                    }
                    currentInstructionIndex += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> TryRangeInclusiveWhere(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder tmpRange = iLGenerator.DeclareLocal(typeof(List<int>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (currentInstructionIndex + 1 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex+1].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[currentInstructionIndex + 1].operand == AccessTools.Method(typeof(List<int>), "Clear")
                    )
                {
                    matchFound = true;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Call;
                    instructionsList[currentInstructionIndex].operand = AccessTools.Method(typeof(Rand_Patch), "getTmpRange");
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Stloc;
                    instructionsList[currentInstructionIndex].operand = tmpRange.LocalIndex;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else if(instructionsList[currentInstructionIndex].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[currentInstructionIndex].operand == AccessTools.Field(typeof(Rand), "tmpRange"))
                {
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldloc;
                    instructionsList[currentInstructionIndex].operand = tmpRange.LocalIndex;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }

    }

}
