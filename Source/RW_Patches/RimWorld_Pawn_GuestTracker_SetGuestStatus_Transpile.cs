using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Reflection.Emit;

namespace RimThreaded.RW_Patches
{
    public class RimWorld_Pawn_GuestTracker_SetGuestStatus_Transpile
    {
        public static AccessTools.FieldRef<Pawn_GuestTracker, Pawn> pawn = AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");
        public static IEnumerable<CodeInstruction> Prefix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (currentInstructionIndex + 2 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex + 2].opcode == OpCodes.Ldstr)
                {
                    matchFound++;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldsfld;
                    instructionsList[currentInstructionIndex].operand = AccessTools.Field(typeof(RimWorld_Pawn_GuestTracker_SetGuestStatus_Transpile), "pawn");
                    yield return instructionsList[currentInstructionIndex];
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AccessTools.FieldRef<Pawn_GuestTracker, Pawn>), "Invoke"));
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    currentInstructionIndex += 5;
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
