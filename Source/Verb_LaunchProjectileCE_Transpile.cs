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
    public class Verb_LaunchProjectileCE_Transpile
    {
        public static IEnumerable<CodeInstruction> CanHitFromCellIgnoringRange(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            // FULL REWRITE 
            //Create LOCAL VAR Thing
            LocalBuilder thing = iLGenerator.DeclareLocal(typeof(Thing));


            //IntVec3 cell = targ.Cell;
            yield return new CodeInstruction(OpCodes.Ldarga_S, 2);
            yield return new CodeInstruction(OpCodes.Call, Method(typeof(LocalTargetInfo), "get_Cell"));
            yield return new CodeInstruction(OpCodes.Stloc_0);

            //Thing thing = targ.Thing;
            yield return new CodeInstruction(OpCodes.Ldarga_S, 2);
            yield return new CodeInstruction(OpCodes.Call, Method(typeof(LocalTargetInfo), "get_Thing"));
            yield return new CodeInstruction(OpCodes.Stloc, thing.LocalIndex);

            //            if (
            //            this.CanHitCellFromCellIgnoringRange(shotSource, cell, thing) && 
            //            (thing == null || thing.Map != this.caster.Map)
            //            ) 
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Ldloc, thing.LocalIndex);
            yield return new CodeInstruction(OpCodes.Call, Method(RimThreadedHarmony.combatExtendedVerb_LaunchProjectileCE, "CanHitCellFromCellIgnoringRange"));
            Label label25 = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse_S, label25);

            yield return new CodeInstruction(OpCodes.Ldloc, thing.LocalIndex);
            Label label20 = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse_S, label20);

            yield return new CodeInstruction(OpCodes.Ldloc, thing.LocalIndex);
            yield return new CodeInstruction(OpCodes.Callvirt, Method(typeof(Thing), "get_Map"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(Verb), "caster"));
            yield return new CodeInstruction(OpCodes.Callvirt, Method(typeof(Thing), "get_Map"));
            yield return new CodeInstruction(OpCodes.Beq_S, label25);

            //{ goodDest = cell;
            CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldarg_3);
            codeInstruction.labels.Add(label20);
            yield return codeInstruction;
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Stobj, typeof(IntVec3));

            //return true;
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ret);

            //} goodDest = IntVec3.Invalid;
            codeInstruction = new CodeInstruction(OpCodes.Ldarg_3);
            codeInstruction.labels.Add(label25);
            yield return codeInstruction;
            yield return new CodeInstruction(OpCodes.Call, Method(typeof(IntVec3), "get_Invalid"));
            yield return new CodeInstruction(OpCodes.Stobj, typeof(IntVec3));

            //return false;
            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static IEnumerable<CodeInstruction> TryFindCEShootLineFromTo(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            LocalBuilder tempLeanShootSources = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            int i = 0;
            bool matchFound = false;
            while (i < instructionsList.Count)
            {
                if (i > 1 &&
                    instructionsList[i - 2].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i - 2].operand == Method(typeof(Verb), "get_CasterIsPawn"))
                {
                    matchFound = true;
                    instructionsList[i].opcode = OpCodes.Newobj;
                    instructionsList[i].operand = Constructor(typeof(List<IntVec3>));
                    yield return instructionsList[i];
                    i++;
                    yield return new CodeInstruction(OpCodes.Stloc, tempLeanShootSources.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_1, null);
                }
                if ( instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(RimThreadedHarmony.combatExtendedVerb_LaunchProjectileCE, "tempLeanShootSources"))
                {
                    matchFound = true;
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = tempLeanShootSources.LocalIndex;
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
