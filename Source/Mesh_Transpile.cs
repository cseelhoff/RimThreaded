using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Threading;
using Verse.AI;

namespace RimThreaded
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
