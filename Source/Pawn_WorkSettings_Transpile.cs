using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class Pawn_WorkSettings_Transpile
    {

        public static IEnumerable<CodeInstruction> CacheWorkGiversInOrder(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;

            Type workTypeDefList = typeof(List<WorkTypeDef>);
            LocalBuilder local_wtsByPrio = iLGenerator.DeclareLocal(workTypeDefList);

            while (i < instructionsList.Count)
            {
                /*
                	// wtsByPrio.Clear();
                	IL_0000: ldsfld class [mscorlib]System.Collections.Generic.List`1<class Verse.WorkTypeDef> RimWorld.Pawn_WorkSettings::wtsByPrio
	                IL_0005: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class Verse.WorkTypeDef>::Clear()
                 */
                if (i + 1 < instructionsList.Count &&
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                        (FieldInfo)(instructionsList[i].operand) == AccessTools.Field(typeof(Pawn_WorkSettings), "wtsByPrio") &&
                    instructionsList[i + 1].opcode == OpCodes.Callvirt &&
                        (MethodInfo)(instructionsList[i + 1].operand) == AccessTools.Method(typeof(List<WorkTypeDef>), "Clear")
                    )
                {
                    // List<WorkTypeDef> list = new List<WorkTypeDef>();
                    //IL_000d: newobj instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.WorkTypeDef>::.ctor()
                    //IL_0012: stloc.1
                    yield return new CodeInstruction(OpCodes.Newobj, typeof(List<WorkTypeDef>).GetConstructor(Type.EmptyTypes));
                    yield return new CodeInstruction(OpCodes.Stloc, local_wtsByPrio.LocalIndex);
                    i += 1;
                }
                /*
                    // workGiversInOrderEmerg.Clear();
	                IL_0098: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class RimWorld.WorkGiver>::Clear()	                  
                */
                else if (
                        instructionsList[i].opcode == OpCodes.Callvirt &&
                        (MethodInfo)(instructionsList[i].operand) == AccessTools.Method(typeof(List<WorkGiver>), "Clear")
                    )
                {
                    /*
                        // WorkGiverListClear(workGiversInOrderEmerg(__instance));
                        IL_00ac: callvirt instance !1& class ['0Harmony']HarmonyLib.AccessTools/FieldRef`2<class ['Assembly-CSharp']RimWorld.Pawn_WorkSettings, class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']RimWorld.WorkGiver>>::Invoke(!0)
                    */
                    instructionsList[i].operand = AccessTools.Method(typeof(Pawn_WorkSettings_Patch), "WorkGiverListClear");
                    yield return instructionsList[i];
                }
                /*
                // workGiversInOrderEmerg.Add(worker);
			        IL_00ed: ldarg.0
			        IL_00ee: ldfld class [mscorlib]System.Collections.Generic.List`1<class RimWorld.WorkGiver> RimWorld.Pawn_WorkSettings::workGiversInOrderEmerg
			        IL_00f3: ldloc.s 8
			        // {
			        IL_00f5: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class RimWorld.WorkGiver>::Add(!0)
                */
                else if (
                        instructionsList[i].opcode == OpCodes.Callvirt &&
                        (MethodInfo)(instructionsList[i].operand) == AccessTools.Method(typeof(List<WorkGiver>), "Add")
                    )
                {
                    /*
                        // WorkGiverListAdd(workGiversInOrderEmerg(__instance), worker);
			            IL_010b: ldsfld class ['0Harmony']HarmonyLib.AccessTools/FieldRef`2<class ['Assembly-CSharp']RimWorld.Pawn_WorkSettings, class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']RimWorld.WorkGiver>> RimThreaded.Pawn_WorkSettings_Patch::workGiversInOrderEmerg
			            IL_0110: ldloc.0
			            IL_0111: ldfld class ['Assembly-CSharp']RimWorld.Pawn_WorkSettings RimThreaded.Pawn_WorkSettings_Patch/'<>c__DisplayClass5_0'::__instance
			            IL_0116: callvirt instance !1& class ['0Harmony']HarmonyLib.AccessTools/FieldRef`2<class ['Assembly-CSharp']RimWorld.Pawn_WorkSettings, class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']RimWorld.WorkGiver>>::Invoke(!0)
			            IL_011b: ldind.ref
			            IL_011c: ldloc.s 10
			            IL_011e: call void RimThreaded.Pawn_WorkSettings_Patch::WorkGiverListAdd(class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']RimWorld.WorkGiver>, class ['Assembly-CSharp']RimWorld.WorkGiver)
		            */
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = AccessTools.Method(typeof(Pawn_WorkSettings_Patch), "WorkGiverListAdd");
                    yield return instructionsList[i];
                }
            //ldsfld uint16 Verse.AI.PathFinder_Original::statusClosedValue
            // wtsByPrio.Add(workTypeDef);
            //IL_0064: ldsfld class [mscorlib] System.Collections.Generic.List`1<class Verse.WorkTypeDef> RimWorld.Pawn_WorkSettings::wtsByPrio
                else if (
                    instructionsList[i].opcode == OpCodes.Ldsfld && 
                    (FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(Pawn_WorkSettings), "wtsByPrio")
                    )
                {
                    //ldloc.2(local_statusClosedValue.LocalIndex)
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = local_wtsByPrio.LocalIndex;
                    yield return instructionsList[i];
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }            
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Pawn_WorkSettings);
            Type patched = typeof(Pawn_WorkSettings_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "CacheWorkGiversInOrder");
        }
    }
}
