using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using System.Linq;
using Verse;

namespace RimThreaded.Mod_Patches
{
    class SpeakUp_Patch
    {
        public static Type GrammarResolver_RandomPossiblyResolvableEntry;

        public static void Patch()
        {
            GrammarResolver_RandomPossiblyResolvableEntry = TypeByName("SpeakUp.GrammarResolver_RandomPossiblyResolvableEntry");
            if (GrammarResolver_RandomPossiblyResolvableEntry != null)
            {
                string methodName = nameof(Prefix);
                Log.Message("RimThreaded is patching " + GrammarResolver_RandomPossiblyResolvableEntry.FullName + " " + methodName);
                RimThreadedHarmony.Transpile(GrammarResolver_RandomPossiblyResolvableEntry, typeof(SpeakUp_Patch), methodName);
            }
        }

        public static IEnumerable<CodeInstruction> Prefix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction ci = instructionsList[i];
                if (ci.opcode == OpCodes.Ldarg_S) //&& (ArgumentInfo)ci.operand == Argument(GrammarResolver_RandomPossiblyResolvableEntry, "___rules")
                {
                    ci.opcode = OpCodes.Ldsfld;
                    ci.operand = Field(TypeByName("GrammarResolver_Replacement"), "rules");
                }
                yield return ci;
            }
        }
    }
}
