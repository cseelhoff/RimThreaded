using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;

namespace RimThreaded.RW_Patches
{
    public class Mesh_Transpile
    {
        public static IEnumerable<CodeInstruction> Mesh(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[i].operand == AccessTools.Method(typeof(Mesh), "InternalCreate")
                    )
                {
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
            }
        }
    }
}
