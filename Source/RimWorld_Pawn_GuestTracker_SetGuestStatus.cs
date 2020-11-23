using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Reflection.Emit;
using System.Reflection;

namespace RimThreaded
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
                if (currentInstructionIndex + 1 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[currentInstructionIndex + 1].operand == AccessTools.Method(typeof(Traverse), "Create"))
                {
                    matchFound++;
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RimWorld_Pawn_GuestTracker_SetGuestStatus_Transpile), "pawn"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AccessTools.FieldRef<Pawn_GuestTracker, Pawn>), "Invoke"));
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    currentInstructionIndex += 5;
                }
                yield return instructionsList[currentInstructionIndex];
                currentInstructionIndex++;
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }
    }
}
