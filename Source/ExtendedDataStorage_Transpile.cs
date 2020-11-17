using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;

namespace RimThreaded
{
    public class ExtendedDataStorage_Transpile
    {
        public static IEnumerable<CodeInstruction> DeleteExtendedDataFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(int), RimThreadedHarmony.giddyUpCoreStorageExtendedPawnData });
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(RimThreadedHarmony.giddyUpCoreStorageExtendedDataStorage, "_store")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "thingIDNumber")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeDictionaryIntData, "Remove", new Type[] { typeof(int) })));
            searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

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
        public static IEnumerable<CodeInstruction> GetExtendedDataFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(int), RimThreadedHarmony.giddyUpCoreStorageExtendedPawnData });
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(RimThreadedHarmony.giddyUpCoreStorageExtendedDataStorage, "_store")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_0));
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_2));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeDictionaryIntData, "set_Item", new Type[] { typeof(int) })));
            //searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

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
    }
}
