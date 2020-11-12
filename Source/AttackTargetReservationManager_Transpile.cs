using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
namespace RimThreaded
{
    public class AttackTargetReservationManager_Transpile
    {
        public static IEnumerable<CodeInstruction> IsReservedBy(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verse.AI.AttackTargetReservationManager), "reservations")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Verse.AI.AttackTargetReservationManager.AttackTargetReservation>), "get_Item")),
                new CodeInstruction(OpCodes.Stloc_1)
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
