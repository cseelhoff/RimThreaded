using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.Sound;
using static HarmonyLib.AccessTools;


namespace RimThreaded
{
    class Sustainer_Patch
    {

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Sustainer);
            Type patched = typeof(Sustainer_Patch);
            ConstructorInfo oMethod = Constructor(original);
            HarmonyMethod transpilerMethod = new HarmonyMethod(Method(patched, nameof(TranspileCtor)));
            RimThreadedHarmony.harmony.Patch(oMethod, transpiler: transpilerMethod);
        }

        public static IEnumerable<CodeInstruction> TranspileCtor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> ciList = instructions.ToList();
            int i = 0;
            while(i < ciList.Count)
            {
                CodeInstruction ci = ciList[i];
                if(ci.opcode == OpCodes.Call && (MethodInfo)ci.operand == Method(typeof(Find),"get_SoundRoot"))
                {
                    CodeInstruction ci1 = ciList[i+1];
                    if(ci1.opcode == OpCodes.Ldfld && (FieldInfo)ci1.operand == Field(typeof(SoundRoot), nameof(SoundRoot.sustainerManager))) {
                        CodeInstruction ci2 = ciList[i + 2];
                        if(ci2.opcode == OpCodes.Ldarg_0)
                        {
                            CodeInstruction ci3 = ciList[i + 3];
                            if (ci3.opcode == OpCodes.Callvirt && (MethodInfo)ci3.operand == Method(typeof(SustainerManager), nameof(SustainerManager.RegisterSustainer))) {
                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                                yield return new CodeInstruction(OpCodes.Call, Method(typeof(Sustainer_Patch), nameof(SustainerManagerRegisterSustainer)));
                                i += 4;
                                continue;
                            }
                        } else if(ci2.opcode == OpCodes.Callvirt && (MethodInfo)ci2.operand == Method(typeof(SustainerManager), nameof(SustainerManager.UpdateAllSustainerScopes)))
                        {
                            yield return new CodeInstruction(OpCodes.Call, Method(typeof(Sustainer_Patch), nameof(SustainerManagerUpdateAllSustainerScopes)));
                            i += 3;
                            continue;
                        }
                    }
                }
                yield return ci;
                i++;
            }
        }
        public static void SustainerManagerRegisterSustainer(Sustainer sustainer)
        {
            SoundRoot soundRoot = Find.SoundRoot;
            if(soundRoot == null)
            {
                Log.Error("SoundRoot is null");
                return;
            }
            SustainerManager sustainerManager = soundRoot.sustainerManager;
            if (sustainerManager == null)
            {
                Log.Error("SustainerManager is null");
                return;
            }
            sustainerManager.RegisterSustainer(sustainer);
        }
        public static void SustainerManagerUpdateAllSustainerScopes()
        {
            SoundRoot soundRoot = Find.SoundRoot;
            if (soundRoot == null)
            {
                Log.Error("SoundRoot is null");
                return;
            }
            SustainerManager sustainerManager = soundRoot.sustainerManager;
            if (sustainerManager == null)
            {
                Log.Error("SustainerManager is null");
                return;
            }
            sustainerManager.UpdateAllSustainerScopes();
        }
    }
}
