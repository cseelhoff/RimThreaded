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
using Verse.AI;

namespace RimThreaded
{
    public class WorkGiver_ConstructDeliverResources_Transpile
	{
        public static IEnumerable<CodeInstruction> ResourceDeliverJobFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			
			LocalBuilder list2 = iLGenerator.DeclareLocal(typeof(List<Thing>));
			LocalBuilder minNum = iLGenerator.DeclareLocal(typeof(int));
			//LocalBuilder resTotalAvailable = iLGenerator.DeclareLocal(typeof(int));   8
			LocalBuilder thingEnum = iLGenerator.DeclareLocal(typeof(IEnumerator<Thing>));
			LocalBuilder item = iLGenerator.DeclareLocal(typeof(Thing));

			// List<Thing> list2 = new List<Thing>();
			//IL_011a: newobj instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::.ctor()
			//IL_011f: stloc.s 8
			yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(List<Thing>)));
			yield return new CodeInstruction(OpCodes.Stloc_S, list2.LocalIndex);

			List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
			while (i < instructionsList.Count)
			{

				// FindAvailableNearbyResources(foundRes, pawn, out int resTotalAvailable);
				//IL_0118: ldarg.0
				//IL_0119: ldloc.s 7
				//IL_011b: ldfld class Verse.Thing RimWorld.WorkGiver_ConstructDeliverResources/'<>c__DisplayClass9_1'::foundRes
				//IL_0120: ldloc.s 7
				//IL_0122: ldfld class RimWorld.WorkGiver_ConstructDeliverResources/'<>c__DisplayClass9_0' RimWorld.WorkGiver_ConstructDeliverResources/'<>c__DisplayClass9_1'::'CS$<>8__locals1'
				//IL_0127: ldfld class Verse.Pawn RimWorld.WorkGiver_ConstructDeliverResources/'<>c__DisplayClass9_0'::pawn
				//IL_012c: ldloca.s 8
				//IL_012e: call instance void RimWorld.WorkGiver_ConstructDeliverResources::FindAvailableNearbyResources(class Verse.Thing, class Verse.Pawn, int32&)

				//AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_0")
				if (
					i + 7 < instructionsList.Count && instructionsList[i + 7].opcode == OpCodes.Call &&
					(MethodInfo)instructionsList[i + 7].operand == (AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), "FindAvailableNearbyResources"))
					)
					{

					instructionsList[i].opcode = OpCodes.Ldloc_S;
					instructionsList[i].operand = list2.LocalIndex;
					yield return instructionsList[i];
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "Clear"));

					i += 7;
					
					//int num = Mathf.Min(foundRes.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(foundRes.def));
					//IL_013d: ldloc.s 7
					//IL_013f: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_0144: ldfld class ['Assembly-CSharp']Verse.ThingDef['Assembly-CSharp'] Verse.Thing::def
					//IL_0149: ldfld int32 ['Assembly-CSharp']Verse.ThingDef::stackLimit
					//IL_014e: ldloc.s 7
					//IL_0150: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
					//IL_0155: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
					//IL_015a: ldfld class ['Assembly-CSharp']Verse.Pawn_CarryTracker['Assembly-CSharp'] Verse.Pawn::carryTracker
					//IL_015f: ldloc.s 7
					//IL_0161: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_0166: ldfld class ['Assembly-CSharp']Verse.ThingDef['Assembly-CSharp'] Verse.Thing::def
					//IL_016b: callvirt instance int32['Assembly-CSharp'] Verse.Pawn_CarryTracker::MaxStackSpaceEver(class ['Assembly-CSharp'] Verse.ThingDef)
					//IL_0170: call int32[UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32, int32)
					//IL_0175: stloc.s 10
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "stackLimit"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "CS$<>8__locals1"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_0"), "pawn"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "carryTracker"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Pawn_CarryTracker), "MaxStackSpaceEver"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Min", new Type[] { typeof(int), typeof(int) }));
					yield return new CodeInstruction(OpCodes.Stloc_S, minNum.LocalIndex);

					// resTotalAvailable = 0;
					//IL_0177: ldc.i4.0
					//IL_0178: stloc.s 9
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Stloc_S, 8); //resTotalAvailable

					// list2.Add(foundRes);
					//IL_017a: ldloc.s 8
					//IL_017c: ldloc.s 7
					//IL_017e: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_0167: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::Add(!0)		
					yield return new CodeInstruction(OpCodes.Ldloc_S, list2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "Add"));

					// resTotalAvailable += foundRes.stackCount;
					//IL_0183: callvirt instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::Add(!0)
					//IL_0188: ldloc.s 9
					//IL_018a: ldloc.s 7
					//IL_018c: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_0191: ldfld int32 ['Assembly-CSharp']Verse.Thing::stackCount
					//IL_0196: add
					//IL_0197: stloc.s 9
					yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes")); 
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "stackCount"));
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Stloc_S, 8);

					// if (resTotalAvailable < num)
					//IL_0199: ldloc.s 9
					//IL_019b: ldloc.s 10
					//IL_019d: bge IL_0235
					yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minNum.LocalIndex);
					Label IL_0235 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Bge, IL_0235);

					// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
					//IL_01a2: ldloc.s 7
					//IL_01a4: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_01a9: callvirt instance valuetype['Assembly-CSharp'] Verse.IntVec3['Assembly-CSharp'] Verse.Thing::get_Position()
					//IL_01ae: ldloc.s 7
					//IL_01b0: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_01b5: callvirt instance class ['Assembly-CSharp']Verse.Map['Assembly-CSharp'] Verse.Thing::get_Map()
					//IL_01ba: ldc.r4 5
					//IL_01bf: ldc.i4.0
					//IL_01c0: call class [mscorlib] System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp'] Verse.Thing> ['Assembly-CSharp'] Verse.GenRadial::RadialDistinctThingsAround(valuetype['Assembly-CSharp'] Verse.IntVec3, class ['Assembly-CSharp'] Verse.Map, float32, bool)
					//IL_01c5: callvirt instance class [mscorlib] System.Collections.Generic.IEnumerator`1<!0> class [mscorlib] System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp'] Verse.Thing>::GetEnumerator()
					//IL_01ca: stloc.s 18
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes")); 
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Position"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Map"));
					yield return new CodeInstruction(OpCodes.Ldc_R4, 5f);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenRadial), "RadialDistinctThingsAround"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IEnumerable<Thing>), "GetEnumerator"));
					yield return new CodeInstruction(OpCodes.Stloc_S, thingEnum.LocalIndex);

					//.try
					//{
					// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
					//IL_01cc: br.s IL_021e
					Label IL_021E = iLGenerator.DefineLabel();
					CodeInstruction tryStart = new CodeInstruction(OpCodes.Br_S, IL_021E);
					tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
					yield return tryStart;

					// loop start (head: IL_021e)
					//label 01ce
					//IL_01ce: ldloc.s 18
					//IL_01d0: callvirt instance !0 class [mscorlib] System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp'] Verse.Thing>::get_Current()
					//IL_01d5: stloc.s 19
					Label IL_01CE = iLGenerator.DefineLabel();
					CodeInstruction Ldloc_S_IL_01CE = new CodeInstruction(OpCodes.Ldloc_S, thingEnum.LocalIndex);
					Ldloc_S_IL_01CE.labels.Add(IL_01CE);
					yield return Ldloc_S_IL_01CE;
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IEnumerator<Thing>), "get_Current"));
					yield return new CodeInstruction(OpCodes.Stloc_S, item.LocalIndex);

					// if (resTotalAvailable >= num)
					//IL_01d7: ldloc.s 9
					//IL_01d9: ldloc.s 10
					//IL_01db: blt.s IL_01df
					Label IL_01DF = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minNum.LocalIndex);
					yield return new CodeInstruction(OpCodes.Blt_S, IL_01DF);

					// break;
					//IL_01dd: leave.s IL_0235
					yield return new CodeInstruction(OpCodes.Leave_S, IL_0235);

					// if (item.def == foundRes.def && GenAI.CanUseItemForWork(pawn, item))
					//label 01df
					//IL_01df: ldloc.s 19
					//IL_01e1: ldfld Verse.Thing::def
					//IL_01e6: ldloc.s 7
					//IL_01e8: ldfld Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_01ed: ldfld Verse.Thing::def
					//IL_01f2: bne.un.s IL_021e
					CodeInstruction ldloc_s_IL_01DF = new CodeInstruction(OpCodes.Ldloc_S, item.LocalIndex);
					ldloc_s_IL_01DF.labels.Add(IL_01DF);
					yield return ldloc_s_IL_01DF;
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "foundRes"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def"));
					yield return new CodeInstruction(OpCodes.Bne_Un_S, IL_021E);
					//IL_01f4: ldloc.s 7
					//IL_01f6: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
					//IL_01fb: ldfld class Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
					//IL_0200: ldloc.s 19
					//IL_0202: call bool['Assembly-CSharp'] Verse.AI.GenAI::CanUseItemForWork(class ['Assembly-CSharp'] Verse.Pawn, class ['Assembly-CSharp'] Verse.Thing)
					//IL_0207: brfalse.s IL_021e
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_1"), "CS$<>8__locals1"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.TypeByName("RimWorld.WorkGiver_ConstructDeliverResources+<>c__DisplayClass9_0"), "pawn"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, item.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenAI), "CanUseItemForWork"));
					yield return new CodeInstruction(OpCodes.Brfalse_S, IL_021E);

					//list2.add(item)
					//IL_0209: ldloc.s 8
					//IL_020b: ldloc.s 19
					//IL_020d: callvirt instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::Add(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, list2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, item.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "Add"));

					// resTotalAvailable += item.stackCount;
					//IL_0212: ldloc.s 9
					//IL_0214: ldloc.s 19
					//IL_0216: ldfld int32['Assembly-CSharp']Verse.Thing::stackCount
					//IL_021b: add
					//IL_021c: stloc.s 9
					yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
					yield return new CodeInstruction(OpCodes.Ldloc_S, item.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "stackCount"));
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Stloc_S, 8);

					// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
					//IL_021e: ldloc.s 18 thingenum
					//IL_0220: callvirt instance bool[mscorlib] System.Collections.IEnumerator::MoveNext()
					//IL_0225: brtrue.s IL_01ce
					// end loop
					CodeInstruction CI_IL_021E = new CodeInstruction(OpCodes.Ldloc_S, thingEnum.LocalIndex);
					CI_IL_021E.labels.Add(IL_021E);
					yield return CI_IL_021E;
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(System.Collections.IEnumerator), "MoveNext"));
					yield return new CodeInstruction(OpCodes.Brtrue_S, IL_01CE);

					//IL_0227: leave.s IL_0235
					yield return new CodeInstruction(OpCodes.Leave_S, IL_0235);

					//} // end .try
					//finally
					//{
					//IL_0229: ldloc.s 18 thingenum
					//IL_022b: brfalse.s IL_0234
					CodeInstruction startFinally = new CodeInstruction(OpCodes.Ldloc_S, thingEnum.LocalIndex);
					startFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
					yield return startFinally;
					Label IL_0234 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brfalse_S, IL_0234);

					//IL_022d: ldloc.s 18 thingenum
					//IL_022f: callvirt instance void[mscorlib] System.IDisposable::Dispose()
					yield return new CodeInstruction(OpCodes.Ldloc_S, thingEnum.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IDisposable), "Dispose"));
					CodeInstruction endFinally = new CodeInstruction(OpCodes.Endfinally);
					endFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
					endFinally.labels.Add(IL_0234);
					yield return endFinally;

					//label 0235
					instructionsList[i + 1].labels.Add(IL_0235);
					yield return instructionsList[i + 1];
					//IL_0234: endfinally
					//} // end handler
					//label 0235
					i += 1;
				}
				//ldsfld class [mscorlib]System.Collections.Generic.List`1<class Verse.Thing> RimWorld.WorkGiver_ConstructDeliverResources::resourcesAvailable
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.Equals(AccessTools.Field(typeof(WorkGiver_ConstructDeliverResources), "resourcesAvailable"))
					)
				{
					instructionsList[i].opcode = OpCodes.Ldloc_S;
					instructionsList[i].operand = list2.LocalIndex;
					yield return instructionsList[i];
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
