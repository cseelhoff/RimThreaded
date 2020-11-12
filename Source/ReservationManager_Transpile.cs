using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using RimWorld;
using Verse.AI;

namespace RimThreaded
{
    public class ReservationManager_Transpile
    {
        public static IEnumerable<CodeInstruction> CanReserve(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> searchInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ReservationManager), "reservations")),
                new CodeInstruction(OpCodes.Ldloc_S, 4),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ReservationManager.Reservation>), "get_Item")),
                new CodeInstruction(OpCodes.Stloc_S, 5)
            };
            List<CodeInstruction> searchInstructions2 = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ReservationManager), "reservations")),
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ReservationManager.Reservation>), "get_Item")),
                new CodeInstruction(OpCodes.Stloc_S, 7)
            };
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (RimThreaded.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
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
                else if (RimThreaded.IsCodeInstructionsMatching(searchInstructions2, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    Label breakDestination = RimThreaded.GetBreakDestination(instructionsList, currentInstructionIndex, iLGenerator);
                    List<CodeInstruction> tryCatchInstructions = RimThreaded.UpdateTryCatchCodeInstructions(
                        instructionsList, breakDestination, iLGenerator.DefineLabel(), ref currentInstructionIndex, currentInstructionIndex + searchInstructions2.Count);
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
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
        
    }
}
