using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class Pawn_JobTracker_DetermineNextJob_Transpile
    {

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            bool matchFound = false;
            while (i < instructionsList.Count)
            {
                if (i + 5 < instructionsList.Count &&
                    instructionsList[i + 5].opcode == OpCodes.Stloc_0)
                {
                    matchFound = true;
                    instructionsList[i].opcode = OpCodes.Ldstr;
                    instructionsList[i].operand = "pawn";
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(AccessTools), "FieldRefAccess", new Type[] { typeof(string) }).MakeGenericMethod(new Type[] { typeof(Pawn_JobTracker), typeof(Pawn) });
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Ldarg_0;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Callvirt;
                    instructionsList[i].operand = Method(typeof(FieldRef<Pawn_JobTracker, Pawn>), "Invoke");
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Ldind_Ref;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i];
                    i++;
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }      
    }
}
