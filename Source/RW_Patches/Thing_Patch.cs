using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    class Thing_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Thing);
            Type patched = typeof(Thing_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Map));
            RimThreadedHarmony.Transpile(original, patched, nameof(TakeDamage));
        }

        public static IEnumerable<CodeInstruction> TakeDamage(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction ci = instructionsList[i];
                if (ci.opcode == OpCodes.Call && (MethodInfo)ci.operand == Method(typeof(Map), "get_MapHeld"))
                {
                    yield return instructionsList[i++];
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    Label label = (Label)instructionsList[i + 13].operand;
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                }
                else
                {
                    yield return ci;
                }
            }
        }

        public static bool get_Map(Thing __instance, ref Map __result)
        {
            __result = null;
            int mapIndexOrState = __instance.mapIndexOrState;
            if (mapIndexOrState >= 0)
                __result = Find.Maps[mapIndexOrState];
            //else
            //{
            //    lock (lastMapIndex)
            //    {
            //        if (lastMapIndex.TryGetValue(__instance, out sbyte lastIndex))
            //        {
            //            __result = Find.Maps[lastIndex];
            //        }
            //    }
            //}
            return false;
        }


    }
}
