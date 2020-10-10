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
    public class AttackTargetFinder_Transpile
    {
        public static IEnumerable<CodeInstruction> BestAttackTarget(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original AttackTargetFinder.BestAttackTarget
             * 
             * C#
             * AttackTargetFinder.tmpTargets.Clear();
             * 
             * IL
	                IL_013c: ldsfld class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.AI.IAttackTarget> Verse.AI.AttackTargetFinder_Target::tmpTargets
	                IL_0141: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.AI.IAttackTarget>::Clear()
             * 
             * Replace with
             * 
             * C#
                    List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
             * 
             * IL
	                IL_013c: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.AI.IAttackTarget>::.ctor()
	                IL_0141: stloc.3 (tmpTargets.LocalIndex)

            
		     * Original IL:
		     *      ldsfld class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.AI.IAttackTarget> Verse.AI.AttackTargetFinder_Target::tmpTargets
		     *      
		     * IL Replace with:
		     *      ldloc.3  (tmpTargets.LocalIndex)
		     * 
            */
            LocalBuilder tmpTargets = iLGenerator.DeclareLocal(typeof(List<IAttackTarget>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.AI.IAttackTarget] tmpTargets") &&
                    instructionsList[i + 1].opcode == OpCodes.Callvirt && instructionsList[i + 1].operand.ToString().Equals("Void Clear()")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Newobj;
                    instructionsList[i].operand = AccessTools.Constructor(typeof(List<IAttackTarget>));
                    yield return instructionsList[i];
                    instructionsList[i + 1].opcode = OpCodes.Stloc;
                    instructionsList[i + 1].operand = tmpTargets.LocalIndex;
                    yield return instructionsList[i + 1];
                    i += 1;
                }
                else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.AI.IAttackTarget] tmpTargets")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = tmpTargets.LocalIndex;
                    yield return instructionsList[i];
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }
        public static IEnumerable<CodeInstruction> CanSee(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original AttackTargetFinder.CanSee
             * 
             * C#
             * (at code start)
             * 
             * Replace with
             * 
             * C#
                    List<IntVec3> tempDestList = new List<IntVec3>();
             * 
             * IL
	                IL_0000: newobj instance void class [mscorlib]System.Collections.Generic.List`1<valuetype ['Assembly-CSharp']Verse.IntVec3>::.ctor()
	                IL_0005: stloc.0 (tempDestList.LocalIndex)

            
		     * Original IL:
		     *      ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype ['Assembly-CSharp']Verse.IntVec3> Verse.AI.AttackTargetFinder_Target::tempDestList
		     *      
		     * IL Replace with:
		     *      ldloc.0 (tempDestList.LocalIndex)
		     * 
            */
            LocalBuilder tempDestList = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            LocalBuilder tempSourceList = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(List<IntVec3>)));
            yield return new CodeInstruction(OpCodes.Stloc, tempDestList.LocalIndex);
            yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(List<IntVec3>)));
            yield return new CodeInstruction(OpCodes.Stloc, tempSourceList.LocalIndex);
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.IntVec3] tempDestList")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = tempDestList.LocalIndex;
                    yield return instructionsList[i];
                } else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.IntVec3] tempSourceList")
                    )
                    {
                        instructionsList[i].opcode = OpCodes.Ldloc;
                        instructionsList[i].operand = tempSourceList.LocalIndex;
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
