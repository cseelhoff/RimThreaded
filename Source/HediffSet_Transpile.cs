using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using RimWorld;
using System.Reflection;

namespace RimThreaded
{
    public class HediffSet_Transpile
    {
        public static IEnumerable<CodeInstruction> PartIsMissing(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffSet), "hediffs")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Hediff>), "get_Item")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Hediff), "get_Part")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Bne_Un_S),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffSet), "hediffs")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Hediff>), "get_Item")),
                new CodeInstruction(OpCodes.Isinst, typeof(Hediff_MissingPart)),
                new CodeInstruction(OpCodes.Brfalse_S),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret),
            };
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in RimThreadedHarmony.UpdateTryCatchCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count))
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
        public static IEnumerable<CodeInstruction> HasDirectlyAddedPartFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffSet), "hediffs")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Hediff>), "get_Item")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Hediff), "get_Part")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Bne_Un_S),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffSet), "hediffs")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Hediff>), "get_Item")),
                new CodeInstruction(OpCodes.Isinst, typeof(Hediff_AddedPart)),
                new CodeInstruction(OpCodes.Brfalse_S),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret),
            };
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in RimThreadedHarmony.UpdateTryCatchCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count))
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
        public static IEnumerable<CodeInstruction> AddDirect(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HediffSet), "hediffs")),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Hediff>), "get_Item")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Hediff), "TryMergeWith")),
                new CodeInstruction(OpCodes.Brfalse_S)
            };
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    for (int i = 0; i < 4; i++)
                    {
                        CodeInstruction codeInstruction = instructionsList[currentInstructionIndex + i];
                        yield return new CodeInstruction(codeInstruction.opcode, codeInstruction.operand);
                    }
                    yield return new CodeInstruction(OpCodes.Brfalse_S, instructionsList[currentInstructionIndex + 6].operand);
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
        public static IEnumerable<CodeInstruction> MoveNext(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (
                    instructionsList[currentInstructionIndex].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[currentInstructionIndex].operand == AccessTools.Method(typeof(HediffSet), "PartIsMissing")) 
                {
                    matchFound++;
                    yield return new CodeInstruction(OpCodes.Brfalse, instructionsList[currentInstructionIndex + 1].operand);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }


    }
}
