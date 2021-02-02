using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class Rand_Transpile
    {

        public static IEnumerable<CodeInstruction> PushState(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Stack<UInt64>);
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Rand), "stateStack")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Rand), "get_StateCompressed")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeDictionaryIntData, "Push")));

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
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Rand), "stateStack")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeDictionaryIntData, "Pop")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Rand), "set_StateCompressed")));

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
            int[] matchesFound = new int[1];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            /*
            yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Rand_Patch), "tmpRange"));
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Ceq);
            Label tmpRangeLabel = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse_S, tmpRangeNullLabel);
            yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(List<int>)));
            yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(Rand_Patch), "tmpRange"));
            instructionsList[i].labels.Add(tmpRangeNullLabel);
            */
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(Rand), "tmpRange")
                )
                {
                    instructionsList[i].operand = Field(typeof(Rand_Patch), "tmpRange");
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
