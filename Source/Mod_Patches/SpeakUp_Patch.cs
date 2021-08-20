using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using System.Linq;

namespace RimThreaded.Mod_Patches
{
    class SpeakUp_Patch
    {
        public static Type SpeakUp_GrammarResolver_Resolve;
        public static void Patch()
        {
            SpeakUp_GrammarResolver_Resolve = TypeByName("SpeakUp.GrammarResolver_Resolve");
            if (SpeakUp_GrammarResolver_Resolve != null)
            {
                string methodName = nameof(Prefix);
                //Log.Message("RimThreaded is patching " + SpeakUp_GrammarResolver_Resolve.FullName + " " + methodName);
                //Transpile(SpeakUp_GrammarResolver_Resolve, typeof(SpeakUp_Patch), methodName);
            }
        }
        public static IEnumerable<CodeInstruction> Prefix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                //if(i + 2 < instructionsList.Count)
                //{
                //    CodeInstruction ci2 = instructionsList[i+2];
                //    if(ci2.opcode == OpCodes.Callvirt && (MethodInfo)ci2.operand == Method(typeof(Verse.Grammar.Rule), "AddRange"))
                //    {
                //        instructionsList[i] = new CodeInstruction(OpCodes.Ldsfld, Field(typeof(GrammarResolver_Patch), nameof(GrammarResolver_Patch.rules)));
                //    }
                //}
                CodeInstruction ci = instructionsList[i];
                if (ci.opcode == OpCodes.Ldsfld && (FieldInfo)ci.operand == Field(SpeakUp_GrammarResolver_Resolve, "rulesInfo"))
                {
                    ci.operand = Field(typeof(GrammarResolver_Patch), nameof(GrammarResolver_Patch.rules));
                }
                yield return ci;
            }
        }
    }
}
