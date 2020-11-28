using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System.Reflection;
using Verse.Grammar;

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
    }
}
