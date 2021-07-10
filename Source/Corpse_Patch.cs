using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Corpse_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Corpse);
            Type patched = typeof(Corpse_Patch);
            RimThreadedHarmony.Transpile(original, patched, nameof(SpawnSetup));
        }
        public static IEnumerable<CodeInstruction> SpawnSetup(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction ci = instructionsList[i];
                if(ci.opcode == OpCodes.Call && (MethodInfo)ci.operand == Method(typeof(Corpse), "get_InnerPawn"))
                {
                    ci.operand = Method(typeof(Corpse_Patch), nameof(SetRotationSouth));
                    yield return ci;
                    i++; //call valuetype Verse.Rot4 Verse.Rot4::get_South()
                    i++; //callvirt instance void Verse.Thing::set_Rotation(valuetype Verse.Rot4)
                    i++; //ldarg.0
                    i++; //NotifyColonistBar();
                    continue;
                }
                yield return ci;
            }
        }

        public static void SetRotationSouth(Corpse __instance)
        {
            Pawn InnerPawn = __instance.InnerPawn;
            if (InnerPawn == null)
                return;
            InnerPawn.Rotation = Rot4.South;
            __instance.NotifyColonistBar();
        }
    }
}
