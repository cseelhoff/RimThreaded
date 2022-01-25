using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    class DubsBadHygiene_Patch
    {
        public static void Patch()
        {
            Type DubsBadHygiene_JobDriver_UseToilet = TypeByName("DubsBadHygiene.JobDriver_UseToilet");
            if (DubsBadHygiene_JobDriver_UseToilet != null)
            {
                string methodName = "<MakeNewToils>b__1_2";
                Log.Message("RimThreaded is patching " + DubsBadHygiene_JobDriver_UseToilet.FullName + " " + methodName);
                RimThreadedHarmony.Transpile(DubsBadHygiene_JobDriver_UseToilet, typeof(DubsBadHygiene_Patch), methodName, nameof(MakeNewToils_b__1_2));
            }
        }

        public static IEnumerable<CodeInstruction> MakeNewToils_b__1_2(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction ci = instructionsList[i];
                if (ci.opcode == OpCodes.Ldfld && (FieldInfo)ci.operand == Field(typeof(Room), nameof(Room.uniqueContainedThings)))
                {
                    ci.opcode = OpCodes.Call;
                    ci.operand = Method(typeof(Room), "get_ContainedAndAdjacentThings");
                }
                yield return ci;
            }
        }
    }
}
