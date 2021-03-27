using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System.Reflection;
using Verse.Grammar;
using System;

namespace RimThreaded
{
    public class GrammarResolver_Transpile
    {
        public static IEnumerable<CodeInstruction> AddRule(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            Label notNull = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brtrue, notNull);
            yield return new CodeInstruction(OpCodes.Ret);
            instructionsList[currentInstructionIndex].labels.Add(notNull);
            while (currentInstructionIndex < instructionsList.Count)
            {
                if(
                    instructionsList[currentInstructionIndex].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[currentInstructionIndex].operand == AccessTools.Field(typeof(GrammarResolver), "rulePool")
                    )
                {
                    matchFound++;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Newobj;
                    instructionsList[currentInstructionIndex].operand = AccessTools.Constructor(typeof(List<>).MakeGenericType(new System.Type[] { AccessTools.TypeByName("Verse.Grammar.GrammarResolver+RuleEntry") }));
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex+=2;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if(matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> RandomPossiblyResolvableEntry(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (
                    instructionsList[currentInstructionIndex].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[currentInstructionIndex].operand == AccessTools.Field(AccessTools.TypeByName("Verse.Grammar.GrammarResolver+RuleEntry"), "knownUnresolvable"))
                {
                    matchFound++;
                    yield return new CodeInstruction(OpCodes.Brfalse, instructionsList[currentInstructionIndex + 1].operand);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> RandomPossiblyResolvableEntryb__0(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            Label ruleNotNull = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Brtrue, ruleNotNull);
            yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
            yield return new CodeInstruction(OpCodes.Ret);
            instructionsList[currentInstructionIndex].labels.Add(ruleNotNull);
            yield return instructionsList[currentInstructionIndex];
            currentInstructionIndex++;
            while (currentInstructionIndex < instructionsList.Count)
            {                
                yield return instructionsList[currentInstructionIndex];
                currentInstructionIndex++;                
            }
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GrammarResolver);
            Type patched = typeof(GrammarResolver_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "AddRule");
            RimThreadedHarmony.Transpile(original, patched, "RandomPossiblyResolvableEntry");
            original = AccessTools.TypeByName("Verse.Grammar.GrammarResolver+<>c__DisplayClass17_0");
            MethodInfo oMethod = AccessTools.Method(original, "<RandomPossiblyResolvableEntry>b__0");
            MethodInfo pMethod = AccessTools.Method(patched, "RandomPossiblyResolvableEntryb__0");
            RimThreadedHarmony.harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));
        }
    }
}
