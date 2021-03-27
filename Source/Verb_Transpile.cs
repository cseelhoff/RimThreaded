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
    public class Verb_Transpile
    {
        public static IEnumerable<CodeInstruction> TryFindShootLineFromTo(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original AttackTargetFinder.BestAttackTarget
             * 
             * C#
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                 
             * 
             * IL
	            // ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
	            IL_011d: ldarg.1
	            IL_011e: ldloca.s 0
	            IL_0120: ldarg.1
	            IL_0121: call instance valuetype Verse.IntVec3 Verse.CellRect::ClosestCellTo(valuetype Verse.IntVec3)
	            IL_0126: ldarg.0
	            IL_0127: ldfld class Verse.Thing Verse.Verb::caster
	            IL_012c: callvirt instance class Verse.Map Verse.Thing::get_Map()
	            IL_0131: ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
	            IL_0136: call void Verse.ShootLeanUtility::LeanShootingSourcesFromTo(valuetype Verse.IntVec3, valuetype Verse.IntVec3, class Verse.Map, class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3>)


             * Replace with
             * 
             * C#
                List<IntVec3> tempLeanShootSources = new List<IntVec3>(); //ADDED
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);

             * 
             * IL
                //List<IntVec3> tempLeanShootSources = new List<IntVec3>();
                +newobj List<IntVec3>
                +stloc tempLeanShootSources.LocalIndex
	            // ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
	            IL_011d: ldarg.1
	            IL_011e: ldloca.s 0
	            IL_0120: ldarg.1
	            IL_0121: call instance valuetype Verse.IntVec3 Verse.CellRect::ClosestCellTo(valuetype Verse.IntVec3)
	            IL_0126: ldarg.0
	            IL_0127: ldfld class Verse.Thing Verse.Verb::caster
	            IL_012c: callvirt instance class Verse.Map Verse.Thing::get_Map()
	            -IL_0131: ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
                +ldloc tempLeanShootShources.LocalIndex
	            *IL_0136: call void Verse.ShootLeanUtility::LeanShootingSourcesFromTo(valuetype Verse.IntVec3, valuetype Verse.IntVec3, class Verse.Map, class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3>)

            
		     * Original IL:
		     *      -ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
		     *      
		     * IL Replace with:
		     *      +ldloc tempLeanShootShources.LocalIndex
		     * 
            */
            LocalBuilder tempLeanShootSources = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    i+8<instructionsList.Count && instructionsList[i + 8].opcode == OpCodes.Call && 
                    (MethodInfo)instructionsList[i+8].operand == AccessTools.Method(typeof(ShootLeanUtility), "LeanShootingSourcesFromTo")
                    )
                {
                    CodeInstruction ci = new CodeInstruction(instructionsList[i].opcode, instructionsList[i].operand);
                    instructionsList[i].opcode = OpCodes.Newobj;
                    instructionsList[i].operand = AccessTools.Constructor(typeof(List<IntVec3>));
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Stloc_S, tempLeanShootSources.LocalIndex);
                    yield return ci;
                    yield return instructionsList[i+1];
                    yield return instructionsList[i+2];
                    yield return instructionsList[i+3];
                    yield return instructionsList[i+4];
                    yield return instructionsList[i+5];
                    yield return instructionsList[i+6];
                    yield return new CodeInstruction(OpCodes.Ldloc_S, tempLeanShootSources.LocalIndex);
                    yield return instructionsList[i+8];
                    i += 8;
                }
                else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && (FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(Verb), "tempLeanShootSources")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = tempLeanShootSources.LocalIndex;
                    yield return instructionsList[i];
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }
        public static IEnumerable<CodeInstruction> CanHitFromCellIgnoringRange(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original AttackTargetFinder.BestAttackTarget
             * 
             * C#
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                 
             * 
             * IL
	            // ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
	            IL_011d: ldarg.1
	            IL_011e: ldloca.s 0
	            IL_0120: ldarg.1
	            IL_0121: call instance valuetype Verse.IntVec3 Verse.CellRect::ClosestCellTo(valuetype Verse.IntVec3)
	            IL_0126: ldarg.0
	            IL_0127: ldfld class Verse.Thing Verse.Verb::caster
	            IL_012c: callvirt instance class Verse.Map Verse.Thing::get_Map()
	            IL_0131: ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
	            IL_0136: call void Verse.ShootLeanUtility::LeanShootingSourcesFromTo(valuetype Verse.IntVec3, valuetype Verse.IntVec3, class Verse.Map, class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3>)


             * Replace with
             * 
             * C#
                List<IntVec3> tempLeanShootSources = new List<IntVec3>(); //ADDED
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);

             * 
             * IL
                //List<IntVec3> tempLeanShootSources = new List<IntVec3>();
                +newobj List<IntVec3>
                +stloc tempLeanShootSources.LocalIndex
	            // ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
	            IL_011d: ldarg.1
	            IL_011e: ldloca.s 0
	            IL_0120: ldarg.1
	            IL_0121: call instance valuetype Verse.IntVec3 Verse.CellRect::ClosestCellTo(valuetype Verse.IntVec3)
	            IL_0126: ldarg.0
	            IL_0127: ldfld class Verse.Thing Verse.Verb::caster
	            IL_012c: callvirt instance class Verse.Map Verse.Thing::get_Map()
	            -IL_0131: ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
                +ldloc tempLeanShootShources.LocalIndex
	            *IL_0136: call void Verse.ShootLeanUtility::LeanShootingSourcesFromTo(valuetype Verse.IntVec3, valuetype Verse.IntVec3, class Verse.Map, class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3>)

            
		     * Original IL:
		     *      -ldsfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.IntVec3> Verse.Verb::tempLeanShootSources
		     *      
		     * IL Replace with:
		     *      +ldloc tempLeanShootShources.LocalIndex
		     * 
            */
            LocalBuilder tempDestList = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    i + 3 < instructionsList.Count && instructionsList[i + 3].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[i + 3].operand == AccessTools.Method(typeof(ShootLeanUtility), "CalcShootableCellsOf")
                    )
                {
                    //CodeInstruction ci = new CodeInstruction(instructionsList[i].opcode, instructionsList[i].operand);
                    instructionsList[i].opcode = OpCodes.Newobj;
                    instructionsList[i].operand = AccessTools.Constructor(typeof(List<IntVec3>));
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Stloc_S, tempDestList.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, tempDestList.LocalIndex);
                    //yield return ci;
                    yield return instructionsList[i + 1];
                    yield return instructionsList[i + 2];
                    yield return instructionsList[i + 3];
                    i += 3;
                }
                else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && (FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(Verb), "tempDestList")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = tempDestList.LocalIndex;
                    yield return instructionsList[i];
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Verb);
            Type patched = typeof(Verb_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "TryFindShootLineFromTo");
            RimThreadedHarmony.Transpile(original, patched, "CanHitFromCellIgnoringRange");
        }
    }
}
