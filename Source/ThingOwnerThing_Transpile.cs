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
    public class ThingOwnerThing_Transpile
    {
        public static IEnumerable<CodeInstruction> TryAdd(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original bool ThingOwner<T>.TryAdd
             * 
             * C#
             * this.innerList.Add(obj);
             * 
             * IL
             * [180 7 - 180 30]
             * IL_01a0: ldarg.0      // this   (OpCodes.Ldarg_0)
             * IL_01a1: ldfld        class [mscorlib]System.Collections.Generic.List`1<!0T> class Verse.ThingOwner`1<!0T>::innerList   (OpCode.Ldfld && Operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] innerList"))
             * IL_01a6: ldloc.0      // obj   (OpCodes.Ldloc_0)
             * IL_01a7: callvirt instance void class [mscorlib] System.Collections.Generic.List`1<!0T>::Add(!0T)   (OpCodes.Callvirt && operand.ToString().Equals("Void Add(Verse.Thing)"))
             * 
             * 
             * Replace with
             * 
             * C#
             * bool __lockWasTaken = false;
             * try {
             *      System.Threading.Monitor.Enter(innerList, ref __lockWasTaken);
             *      innerList.Add(val);
             * }
             * finally {
             *      if (__lockWasTaken) System.Threading.Monitor.Exit(innerList);
             * }
             * 
             * IL
             * IL_01a3: ldc.i4.0   (OpCodes.Ldc_I4_0)
             * IL_01a4: stloc.1      // __lockWasTaken   (OpCodes.Stloc, __lockWasTaken.LocalIndex)
             * .try (ExceptionBlockType.BeginExceptionBlock)
	         * {
	         * 
	         * // System.Threading.Monitor.Enter(innerList, ref __lockWasTaken);
		     * IL_01a5: ldarg.0   (Label: TryStart, OpCodes.Ldarg_0)
		     * IL_01a6: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner`1<!T>::innerList   (OpCodes.Ldfld, AccessTools.Field(typeof(ThingOwner<Thing>), "innerList"))
		     * IL_01ab: ldloca.s 1   (OpCodes.Ldloca_S, __lockWasTaken.LocalIndex)
		     * IL_01ad: call void [mscorlib]System.Threading.Monitor::Enter(object, bool&)   (OpCodes.Call, AccessTools.Method(typeof(System.Threading.Monitor), "Enter"));
		     *
             * // innerList.Add(val);
		     * IL_01b2: ldarg.0
		     * IL_01b3: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner`1<!T>::innerList
		     * IL_01b8: ldloc.0
		     * IL_01b9: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<!T>::Add(!0)
		     * IL_01be: leave.s IL_01cf
		     * }
		     * finally (ExceptionBlockType.BeginFinallyBlock)
	         * {
	         * 
	         * // if (__lockWasTaken)
		     * IL_01c0: ldloc.1 (Label: TryEndHandlerStart)
		     * IL_01c1: brfalse.s IL_01ce
		     * 
		     * // Monitor.Exit(innerList);
		     * IL_01c3: ldarg.0
		     * IL_01c4: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner`1<!T>::innerList
		     * IL_01c9: call void [mscorlib]System.Threading.Monitor::Exit(object)
		     * 
		     * IL_01ce: endfinally (Label: EndFinally)
		     * }   ExceptionBlockType.EndExceptionBlock
		     * 
		     * //Line after C# innerList.Add(val);
		     * IL_01cf: ldarg.0 (Label: HandlerEnd)
		     * 
		     * EXCEPTION HANDLER
		     * Handler Type: Finally
		     * 
            */
            LocalBuilder __lockWasTaken = iLGenerator.DeclareLocal(typeof(bool));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] innerList") &&
                    instructionsList[i + 2].opcode == OpCodes.Ldloc_0 && //TODO: Maybe replace with reference to Local Variable Index of innerList
                    instructionsList[i + 3].opcode == OpCodes.Callvirt && instructionsList[i + 3].operand.ToString().Equals("Void Add(Verse.Thing)")
                    )
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, __lockWasTaken.LocalIndex);
                    CodeInstruction tryStartLdarg_0 = new CodeInstruction(OpCodes.Ldarg_0); //TODO: Maybe replace with reference to Local Variable Index of innerList
                    tryStartLdarg_0.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                    yield return tryStartLdarg_0;
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingOwner<Thing>), "innerList"));
                    yield return new CodeInstruction(OpCodes.Ldloca_S, __lockWasTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));
                    yield return instructionsList[i];
                    yield return instructionsList[i + 1];
                    yield return instructionsList[i + 2];
                    yield return instructionsList[i + 3];
                    Label handlerEnd = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Leave_S, handlerEnd);
                    CodeInstruction tryEndHandlerStartLdloc = new CodeInstruction(OpCodes.Ldloc, __lockWasTaken.LocalIndex);
                    tryEndHandlerStartLdloc.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
                    yield return tryEndHandlerStartLdloc;
                    Label endFinally = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endFinally);
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //TODO: Maybe replace with reference to Local Variable Index of innerList
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingOwner<Thing>), "innerList"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit", new Type[] { typeof(object) }));
                    CodeInstruction endFinallyInstruction = new CodeInstruction(OpCodes.Endfinally);
                    endFinallyInstruction.labels.Add(endFinally);
                    endFinallyInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                    yield return endFinallyInstruction;
                    instructionsList[i + 4].labels.Add(handlerEnd);
                    yield return instructionsList[i + 4];
                    i += 4;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }
        public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original bool ThingOwner<T>.Remove
             * 
             * C#
             * int index = innerList.LastIndexOf((T)item);
             * innerList.RemoveAt(index);
             * 
             * IL
             * IL_001b: ldarg.0   (OpCodes.Ldarg_0)
	         * IL_001c: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner`1<!T>::innerList   (OpCode.Ldfld && Operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] innerList"))
	         * IL_0021: ldarg.1   (OpCodes.Ldarg_1)
	         * IL_0022: unbox.any !T
	         * IL_0027: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<!T>::LastIndexOf(!0)
	         * IL_002c: stloc.0
	         * // innerList.RemoveAt(index);
	         * IL_002d: ldarg.0
	         * IL_002e: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner`1<!T>::innerList
	         * IL_0033: ldloc.0
	         * IL_0034: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<!T>::RemoveAt(int32)
             * 
             * 
             * Replace with
             * 
             * C#
             * bool __lockWasTaken = false;
             * try {
             *      System.Threading.Monitor.Enter(innerList, ref __lockWasTaken);
             *      int index = innerList.LastIndexOf((T)item);
             *      if (removeIndex == -1)
             *          innerList.RemoveAt(index);
             * }
             * finally {
             *      if (__lockWasTaken) System.Threading.Monitor.Exit(innerList);
             * }
             * 
             * IL
             *  // bool lockTaken = false;
	            IL_001b: ldc.i4.0
	            IL_001c: stloc.1
	            .try
	            {
		            // Monitor.Enter(innerList, ref lockTaken);
		            IL_001d: ldarg.0
		            IL_001e: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner_Target`1<!T>::innerList
		            IL_0023: ldloca.s 1
		            IL_0025: call void [mscorlib]System.Threading.Monitor::Enter(object, bool&)
		            // int num = innerList.LastIndexOf((T)item);
		            IL_002a: ldarg.0
		            IL_002b: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner_Target`1<!T>::innerList
		            IL_0030: ldarg.1
		            IL_0031: unbox.any !T
		            IL_0036: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<!T>::LastIndexOf(!0)
		            IL_003b: stloc.0
		            // if (num == -1)
		            IL_003c: ldloc.0
		            IL_003d: ldc.i4.m1
		            IL_003e: bne.un.s IL_004c

		            // innerList.RemoveAt(num);
		            IL_0040: ldarg.0
		            IL_0041: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner_Target`1<!T>::innerList
		            IL_0046: ldloc.0
		            // {
		            IL_0047: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<!T>::RemoveAt(int32)

		            // }
		            IL_004c: leave.s IL_005d
	            } // end .try
	            finally
	            {
		            // if (lockTaken)
		            IL_004e: ldloc.0
		            IL_004f: brfalse.s IL_005c

		            // Monitor.Exit(innerList);
		            IL_0051: ldarg.0
		            IL_0052: ldfld class [mscorlib]System.Collections.Generic.List`1<!0> class Verse.ThingOwner_Target`1<!T>::innerList
		            // {
		            IL_0057: call void [mscorlib]System.Threading.Monitor::Exit(object)

		            // }
		            IL_005c: endfinally
	            } // end handler

	            // NotifyRemoved(item);
	            IL_005d: ldarg.0
		     * 
		     * EXCEPTION HANDLER
		     * Handler Type: Finally
		     * 
            */
            LocalBuilder __lockWasTaken = iLGenerator.DeclareLocal(typeof(bool));
            LocalBuilder thingList = iLGenerator.DeclareLocal(typeof(List<Thing>));
            LocalBuilder returnValue = iLGenerator.DeclareLocal(typeof(bool));
            List<CodeInstruction> instructionsList = instructions.ToList();
            Label returnLabel = iLGenerator.DefineLabel();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] innerList") &&
                    instructionsList[i + 2].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i + 3].opcode == OpCodes.Unbox_Any && instructionsList[i + 3].operand.ToString().Equals("Verse.Thing") &&
                    instructionsList[i + 4].opcode == OpCodes.Callvirt && instructionsList[i + 4].operand.ToString().Equals("Int32 LastIndexOf(Verse.Thing)") &&
                    instructionsList[i + 5].opcode == OpCodes.Stloc_0 && //TODO: Maybe replace with reference to Local Variable Index of num
                    instructionsList[i + 6].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 7].opcode == OpCodes.Ldfld && instructionsList[i + 7].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing] innerList") &&
                    instructionsList[i + 8].opcode == OpCodes.Ldloc_0 && //TODO: Maybe replace with reference to Local Variable Index of num
                    instructionsList[i + 9].opcode == OpCodes.Callvirt && instructionsList[i + 9].operand.ToString().Equals("Void RemoveAt(Int32)")
                    )
                {
                    CodeInstruction startCode = new CodeInstruction(OpCodes.Ldarg_0);
                    List<Label> startLabels = instructionsList[i].labels;
                    instructionsList[i].labels = startCode.labels;
                    startCode.labels = startLabels;
                    yield return startCode;
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingOwner<Thing>), "innerList"));
                    yield return new CodeInstruction(OpCodes.Stloc, thingList.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, __lockWasTaken.LocalIndex);
                    CodeInstruction tryStartLdarg_0 = new CodeInstruction(OpCodes.Ldloc, thingList.LocalIndex); //TODO: Maybe replace with reference to Local Variable Index of innerList
                    tryStartLdarg_0.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                    yield return tryStartLdarg_0;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, __lockWasTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));
                    yield return instructionsList[i];
                    yield return instructionsList[i + 1];
                    yield return instructionsList[i + 2];
                    yield return instructionsList[i + 3];
                    yield return instructionsList[i + 4];
                    yield return instructionsList[i + 5];
                    yield return new CodeInstruction(OpCodes.Ldloc_0);//TODO: Maybe replace with reference to Local Variable Index of num
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                    Label itemFound = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, itemFound);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, returnValue.LocalIndex);                    
                    yield return new CodeInstruction(OpCodes.Leave_S, returnLabel);
                    instructionsList[i + 6].labels.Add(itemFound);
                    yield return instructionsList[i + 6];
                    yield return instructionsList[i + 7];
                    yield return instructionsList[i + 8];
                    yield return instructionsList[i + 9];
                    Label handlerEnd = iLGenerator.DefineLabel();
                    CodeInstruction leaveForHandlerEnd = new CodeInstruction(OpCodes.Leave_S, handlerEnd);
                    yield return leaveForHandlerEnd;
                    CodeInstruction tryEndHandlerStartLdloc = new CodeInstruction(OpCodes.Ldloc, __lockWasTaken.LocalIndex);
                    tryEndHandlerStartLdloc.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
                    yield return tryEndHandlerStartLdloc;
                    Label endFinally = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endFinally);
                    yield return new CodeInstruction(OpCodes.Ldloc, thingList.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit", new Type[] { typeof(object) }));
                    CodeInstruction endFinallyInstruction = new CodeInstruction(OpCodes.Endfinally);
                    endFinallyInstruction.labels.Add(endFinally);
                    endFinallyInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                    yield return endFinallyInstruction;
                    instructionsList[i + 10].labels.Add(handlerEnd);
                    yield return instructionsList[i + 10];
                    i += 10;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            CodeInstruction returnCode = new CodeInstruction(OpCodes.Ldloc, returnValue.LocalIndex);
            returnCode.labels.Add(returnLabel);
            yield return returnCode;
            yield return new CodeInstruction(OpCodes.Ret);
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(ThingOwner<Thing>);
            Type patched = typeof(ThingOwnerThing_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
            RimThreadedHarmony.Transpile(original, patched, "Remove");
        }
    }
}
