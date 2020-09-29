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
    public class Fire_Transpile
    {
        public static IEnumerable<CodeInstruction> DoComplexCalcs(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original Fire.DoComplexCalcs
             * 
             * C#
             * flammableList.Clear();
             * 
             * IL
	                ldsfld class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing> Rimworld.Fire_Target::flammableList
	                callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::Clear()
             * 
             * Replace with
             * 
             * C#
                    List<Thing> flammableList = new List<Thing>();
             * 
             * IL
	                newobj instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.AI.Thing>::.ctor()
	                stloc.3 (flammableList.LocalIndex)

            
		     * Original IL:
		     *      ldsfld class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing> Rimworld.Fire_Target::flammableList
		     *      
		     * IL Replace with:
		     *      ldloc.3  (flammableList.LocalIndex)
		     * 
            */
            LocalBuilder flammableList = iLGenerator.DeclareLocal(typeof(List<Thing>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] flammableList") &&
                    instructionsList[i + 1].opcode == OpCodes.Callvirt && instructionsList[i + 1].operand.ToString().Equals("Void Clear()")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Newobj;
                    instructionsList[i].operand = AccessTools.Constructor(typeof(List<Thing>));
                    yield return instructionsList[i];
                    instructionsList[i + 1].opcode = OpCodes.Stloc;
                    instructionsList[i + 1].operand = flammableList.LocalIndex;
                    yield return instructionsList[i + 1];
                    i += 1;
                }
                else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] flammableList")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = flammableList.LocalIndex;
                    yield return instructionsList[i];
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }
    }
}
