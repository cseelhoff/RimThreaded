using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using Verse.AI;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{
    public class Pawn_PathFollower_Transpile
    {
        public static IEnumerable<CodeInstruction> StartPath(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            bool matchFound = false;
            while (i < instructionsList.Count)
            {
                if (i - 3 > 0 &&
                    instructionsList[i - 3].opcode == OpCodes.Call &&
                     (MethodInfo)instructionsList[i - 3].operand == Method(typeof(LocalTargetInfo), "get_Thing") &&
                     instructionsList[i].opcode == OpCodes.Call &&
                     (MethodInfo)instructionsList[i].operand == Method(typeof(Log), "Error"))
                {
                    matchFound = true;
                    instructionsList[i].operand = Method(typeof(Log), "Warning");
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
