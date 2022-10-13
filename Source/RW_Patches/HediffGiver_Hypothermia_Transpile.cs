using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class HediffGiver_Hypothermia_Transpile
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(HediffGiver_Hypothermia);
            Type patched = typeof(HediffGiver_Hypothermia_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "OnIntervalPassed");
        }
        public static IEnumerable<CodeInstruction> OnIntervalPassed(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder comfortableTemperatureMin = iLGenerator.DeclareLocal(typeof(float));
            LocalBuilder minTemp = iLGenerator.DeclareLocal(typeof(float));

            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;


            while (i < instructionsList.Count)
            {
                if (i + 1 < instructionsList.Count &&
                    instructionsList[i].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i + 1].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[i + 1].operand == AccessTools.Method(typeof(GenTemperature), "ComfortableTemperatureRange", new Type[] { typeof(Pawn) })
                    )
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(StatDefOf), "ComfyTemperatureMin"));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), "GetStatValue"));
                    yield return new CodeInstruction(OpCodes.Stloc, comfortableTemperatureMin.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, comfortableTemperatureMin.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 10f);
                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Stloc, minTemp.LocalIndex);
                    i += 6;
                }
                else if (instructionsList[i].opcode == OpCodes.Ldloc_3)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, minTemp.LocalIndex);
                    i += 2;
                }
                else if (instructionsList[i].opcode == OpCodes.Ldloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, comfortableTemperatureMin.LocalIndex);
                    i += 2;
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
            }

        }

    }
}
