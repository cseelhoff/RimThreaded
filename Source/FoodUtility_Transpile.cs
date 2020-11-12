using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using RimWorld;

namespace RimThreaded
{
    public class FoodUtility_Transpile
    {
        public static IEnumerable<CodeInstruction> FoodOptimality(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FoodUtility), "FoodOptimalityEffectFromMoodCurve")),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_S, 4),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ThoughtDef>), "get_Item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThoughtDef), "stages")),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ThoughtStage>), "get_Item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThoughtStage), "baseMoodEffect")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(SimpleCurve), "Evaluate")),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc_0)
            };
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if(RimThreaded.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    Label breakDestination = RimThreaded.GetBreakDestination(instructionsList, currentInstructionIndex, iLGenerator);
                    List<CodeInstruction> tryCatchInstructions = RimThreaded.UpdateTryCatchCodeInstructions(
                        instructionsList, breakDestination, iLGenerator.DefineLabel(), ref currentInstructionIndex, currentInstructionIndex + searchInstructions.Count);
                    foreach (CodeInstruction codeInstruction in tryCatchInstructions)
                    {
                        yield return codeInstruction;
                    }
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                }
                currentInstructionIndex++;
            }
            if(!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
    }
}
