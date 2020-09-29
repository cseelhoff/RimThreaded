using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Threading;

namespace RimThreaded
{
    public class JobDriver_Wait_Transpile
    {
        public static IEnumerable<CodeInstruction> CheckForAutoAttack(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original void CheckForAutoAttack()
             * 
             * C#
             *      for (int j = 0; j < thingList.Count; j++) {
             * 
             * IL
                    // for (int j = 0; j < thingList.Count; j++)
		    i-3     IL_00c7: ldc.i4.0
		    i-2     IL_00c8: stloc.s 6
		    i-1     IL_00ca: br IL_017d

             * Replace with
             * 
             * C#
             *      for (int j = 0; j < thingList.Count; j++) {
                        Thing thing2;
                        try
                        {
                            thing2 = thingList[j];
                        }
                        catch (ArgumentOutOfRangeException) 
                        { 
                            break; 
                        }
             * 
             * IL
             * 		IL_00c4: ldc.i4.0 
		            IL_00c5: stloc.s 6
		            IL_00c7: br IL_0180
		            // loop start (head: IL_0180)
			        // thing = thingList[j];
			        IL_00cc: nop <-- take labels from i
                    .try
			        {
				        IL_00cd: ldloc.s 5
				        IL_00cf: ldloc.s 6
				        IL_00d1: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::get_Item(int32)
				        IL_00d6: stloc.s 7 (Thing)
				        // }
				        IL_00d8: leave.s IL_00e0
			        } // end .try
			        catch [mscorlib]System.ArgumentOutOfRangeException
			        {
				        // {
				        IL_00da: pop
				        // goto IL_018e;
				        IL_00db: leave IL_018e
			        } // end handler
            i
            i+1
		     * 
            */
            LocalBuilder thing = iLGenerator.DeclareLocal(typeof(Thing));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < 9)
            {
                yield return instructionsList[i];
                i++;
            }
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i - 9].opcode == OpCodes.Brfalse &&
                    instructionsList[i - 3].opcode == OpCodes.Ldc_I4_0 &&
                    instructionsList[i - 2].opcode == OpCodes.Stloc_S && instructionsList[i - 2].operand.ToString().Equals("System.Int32 (6)") &&
                    instructionsList[i - 1].opcode == OpCodes.Br
                    )
                {
                    CodeInstruction startCode = new CodeInstruction(OpCodes.Nop);
                    List<Label> startLabels = instructionsList[i].labels;
                    instructionsList[i].labels = startCode.labels;
                    startCode.labels = startLabels;
                    yield return startCode;
                    CodeInstruction beginTry = new CodeInstruction(OpCodes.Ldloc_S, 5);
                    beginTry.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                    yield return beginTry;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "get_Item", new Type[] { typeof(int) }));
                    yield return new CodeInstruction(OpCodes.Stloc_S, thing.LocalIndex);
                    Label handlerEnd = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Leave_S, handlerEnd);
                    CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
                    pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
                    yield return pop;
                    Label exitLoop = (Label)instructionsList[i - 9].operand;
                    CodeInstruction leaveLoopEnd = new CodeInstruction(OpCodes.Leave, exitLoop);
                    leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                    yield return leaveLoopEnd;
                    instructionsList[i].labels.Add(handlerEnd);
                    yield return instructionsList[i];
                }
                else if (
                    instructionsList[i].opcode == OpCodes.Ldloc_S && instructionsList[i].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] (5)") &&
                    instructionsList[i + 1].opcode == OpCodes.Ldloc_S && instructionsList[i + 1].operand.ToString().Equals("System.Int32 (6)") &&
                    instructionsList[i + 2].opcode == OpCodes.Callvirt && instructionsList[i + 2].operand.ToString().Equals("Verse.Thing get_Item(Int32)")
                    )
                {
                    instructionsList[i].operand = thing.LocalIndex;
                    yield return instructionsList[i];
                    //yield return instructionsList[i + 1];
                    //yield return instructionsList[i + 2];
                    i += 2;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }        
    }
}
