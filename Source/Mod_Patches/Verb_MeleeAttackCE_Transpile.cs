using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    public class Verb_MeleeAttackCE_Transpile
    {
        
        public static void PreApplyMeleeSlaveSuppression(DamageWorker.DamageResult damageResult, Pawn pawn, RimWorld.Verb_MeleeAttack verb_MeleeAttack)
        {
            if (pawn != null && damageResult.totalDamageDealt > 0f)
            {
                verb_MeleeAttack.ApplyMeleeSlaveSuppression(pawn, damageResult.totalDamageDealt);
            }
        }

        public static IEnumerable<CodeInstruction> TryCastShot(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            bool matchFound = false;
            /*
             * ldloc.1      // thing
             * brfalse labelThingIsNull
                IL_0587: ldloc.0      // casterPawn
                IL_0588: ldfld        class ['Assembly-CSharp']Verse.Pawn_RotationTracker ['Assembly-CSharp']Verse.Pawn::rotationTracker
                IL_058d: ldloc.1      // thing
                IL_058e: callvirt     instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
                IL_0593: callvirt     instance void ['Assembly-CSharp']Verse.Pawn_RotationTracker::FaceCell(valuetype ['Assembly-CSharp']Verse.IntVec3)
                add label labelThingIsNull
            */
            while (i < instructionsList.Count)
            {
                CodeInstruction ci = instructionsList[i];
                if(ci.opcode == OpCodes.Callvirt && (MethodInfo)ci.operand == Method(typeof(RimWorld.Verb_MeleeAttack), nameof(RimWorld.Verb_MeleeAttack.ApplyMeleeDamageToTarget))) {
                    yield return ci;
                    yield return new CodeInstruction(OpCodes.Dup);

                    i++; 
                    yield return instructionsList[i]; // ldloc.s 20
                    i++; 
                    yield return instructionsList[i]; // AssociateWithLog

                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6); // load pawn target
                    yield return new CodeInstruction(OpCodes.Ldarg_0);    // verb_MeleeAttack (this)
                    yield return new CodeInstruction(OpCodes.Call, Method(typeof(Verb_MeleeAttackCE_Transpile), nameof(PreApplyMeleeSlaveSuppression)));
                }
                else if (i + 3 < instructionsList.Count &&
                    instructionsList[i + 3].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i + 3].operand == Method(typeof(Thing), "get_Position"))
                {
                    matchFound = true;
                    instructionsList[i].opcode = OpCodes.Ldloc_1;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i];
                    i++;
                    Label label = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return instructionsList[i];
                    i++;
                    yield return instructionsList[i];
                    i++;
                    yield return instructionsList[i];
                    i++;
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].labels.Add(label);
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
