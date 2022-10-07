using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace RimThreaded.RW_Patches
{
    class Pawn_ApparelTracker_Patch
    {
        [ThreadStatic] public static List<Apparel> tmpApparel = new List<Apparel>();
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_ApparelTracker);
            Type patched = typeof(Pawn_ApparelTracker_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LostBodyPart));
            RimThreadedHarmony.Transpile(original, patched, nameof(Notify_LostBodyPart));
        }
        public static IEnumerable<CodeInstruction> Notify_LostBodyPart(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (code.opcode == OpCodes.Stloc_2)
                {
                    yield return code;
                    for (int j = i + 1; j < codes.Count; j++)
                    {
                        CodeInstruction tempCode = codes[j];
                        if (tempCode.opcode == OpCodes.Brtrue_S)
                        {
                            Label jumpLabel = (Label)tempCode.operand;
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);
                            break;
                        }
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        //public static bool Notify_LostBodyPart(Pawn_ApparelTracker __instance)
        //{
        //    Pawn_ApparelTracker.tmpApparel.Clear();
        //    for (int index = 0; index < __instance.wornApparel.Count; ++index)
        //        Pawn_ApparelTracker.tmpApparel.Add(__instance.wornApparel[index]);
        //    for (int index = 0; index < Pawn_ApparelTracker.tmpApparel.Count; ++index)
        //    {
        //        Apparel ap = Pawn_ApparelTracker.tmpApparel[index];
        //        if (ap != null && !ApparelUtility.HasPartsToWear(__instance.pawn, ap.def))
        //            __instance.Remove(ap);
        //    }
        //    return false;
        //}

    }
}
