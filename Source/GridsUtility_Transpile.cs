using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using RimWorld;

namespace RimThreaded
{
    public class GridsUtility_Transpile
    {
        public static IEnumerable<CodeInstruction> GetGas(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "get_Item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "category")),
                new CodeInstruction(OpCodes.Ldc_I4_7),
                new CodeInstruction(OpCodes.Bne_Un_S),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "get_Item")),
                new CodeInstruction(OpCodes.Castclass, typeof(Gas)),
                new CodeInstruction(OpCodes.Ret),
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
