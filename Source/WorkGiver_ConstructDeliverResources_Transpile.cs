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
			/* ORIG:
			 * 
			 * // FindAvailableNearbyResources2(foundRes, pawn, out int resTotalAvailable, list2);
			IL_0121: ldloc.s 7
			IL_0123: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_0128: ldloc.s 7
			IL_012a: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
			IL_012f: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
			IL_0134: ldloca.s 9
			IL_0136: ldloc.s 8
			// (no C# code)
			IL_0138: call void RimThreaded.WorkGiver_ConstructDeliverResources_Patch::FindAvailableNearbyResources2(class ['Assembly-CSharp']Verse.Thing, class ['Assembly-CSharp']Verse.Pawn, int32&, class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>)

			 * 
			 * Change to:
			 * 
			// List<Thing> list2 = new List<Thing>();
			IL_011a: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::.ctor()
			// {
			IL_011f: stloc.s 8
			// FindAvailableNearbyResources2(foundRes, pawn, out int resTotalAvailable, list2);
			IL_0121: ldloc.s 7
			IL_0123: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_0128: ldloc.s 7
			IL_012a: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
			IL_012f: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
			IL_0134: ldloca.s 9
			IL_0136: ldloc.s 8
			// (no C# code)
			IL_0138: call void RimThreaded.WorkGiver_ConstructDeliverResources_Patch::FindAvailableNearbyResources2(class ['Assembly-CSharp']Verse.Thing, class ['Assembly-CSharp']Verse.Pawn, int32&, class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>)
			// int num = Mathf.Min(foundRes.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(foundRes.def));
			IL_013d: ldloc.s 7
			IL_013f: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_0144: ldfld class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
			IL_0149: ldfld int32 ['Assembly-CSharp']Verse.ThingDef::stackLimit
			IL_014e: ldloc.s 7
			IL_0150: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
			IL_0155: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
			IL_015a: ldfld class ['Assembly-CSharp']Verse.Pawn_CarryTracker ['Assembly-CSharp']Verse.Pawn::carryTracker
			IL_015f: ldloc.s 7
			IL_0161: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_0166: ldfld class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
			IL_016b: callvirt instance int32 ['Assembly-CSharp']Verse.Pawn_CarryTracker::MaxStackSpaceEver(class ['Assembly-CSharp']Verse.ThingDef)
			IL_0170: call int32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32, int32)
			IL_0175: stloc.s 10
			// resTotalAvailable = 0;
			IL_0177: ldc.i4.0
			IL_0178: stloc.s 9
			// list2.Add(foundRes);
			IL_017a: ldloc.s 8
			IL_017c: ldloc.s 7
			IL_017e: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			// resTotalAvailable += foundRes.stackCount;
			IL_0183: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::Add(!0)
			IL_0188: ldloc.s 9
			IL_018a: ldloc.s 7
			IL_018c: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_0191: ldfld int32 ['Assembly-CSharp']Verse.Thing::stackCount
			IL_0196: add
			IL_0197: stloc.s 9
			// if (resTotalAvailable < num)
			IL_0199: ldloc.s 9
			IL_019b: ldloc.s 10
			IL_019d: bge IL_0235

			// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
			IL_01a2: ldloc.s 7
			IL_01a4: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_01a9: callvirt instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
			// (no C# code)
			IL_01ae: ldloc.s 7
			IL_01b0: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
			IL_01b5: callvirt instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
			IL_01ba: ldc.r4 5
			IL_01bf: ldc.i4.0
			IL_01c0: call class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Thing> ['Assembly-CSharp']Verse.GenRadial::RadialDistinctThingsAround(valuetype ['Assembly-CSharp']Verse.IntVec3, class ['Assembly-CSharp']Verse.Map, float32, bool)
			IL_01c5: callvirt instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Thing>::GetEnumerator()
			// {
			IL_01ca: stloc.s 18
			.try
			{
				// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
				IL_01cc: br.s IL_021e
				// loop start (head: IL_021e)
				IL_01ce: ldloc.s 18
				IL_01d0: callvirt instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Thing>::get_Current()
				// {
				IL_01d5: stloc.s 19
				// if (resTotalAvailable >= num)
				IL_01d7: ldloc.s 9
				IL_01d9: ldloc.s 10
				IL_01db: blt.s IL_01df

				// break;
				IL_01dd: leave.s IL_0235

				// if (item.def == foundRes.def && GenAI.CanUseItemForWork(pawn, item))
				IL_01df: ldloc.s 19
				IL_01e1: ldfld class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
				IL_01e6: ldloc.s 7
				IL_01e8: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
				IL_01ed: ldfld class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
				IL_01f2: bne.un.s IL_021e

				// list2.Add(item);
				IL_01f4: ldloc.s 7
				IL_01f6: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
				IL_01fb: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
				IL_0200: ldloc.s 19
				IL_0202: call bool ['Assembly-CSharp']Verse.AI.GenAI::CanUseItemForWork(class ['Assembly-CSharp']Verse.Pawn, class ['Assembly-CSharp']Verse.Thing)
				IL_0207: brfalse.s IL_021e

				IL_0209: ldloc.s 8
				IL_020b: ldloc.s 19
				// {
				IL_020d: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::Add(!0)
				// resTotalAvailable += item.stackCount;
				IL_0212: ldloc.s 9
				IL_0214: ldloc.s 19
				IL_0216: ldfld int32 ['Assembly-CSharp']Verse.Thing::stackCount
				IL_021b: add
				IL_021c: stloc.s 9

				// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
				IL_021e: ldloc.s 18
				IL_0220: callvirt instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
				IL_0225: brtrue.s IL_01ce
				// end loop

				// (no C# code)
				IL_0227: leave.s IL_0235
				} // end .try
				finally
				{
					IL_0229: ldloc.s 18
					IL_022b: brfalse.s IL_0234

					IL_022d: ldloc.s 18
					IL_022f: callvirt instance void [mscorlib]System.IDisposable::Dispose()

					IL_0234: endfinally
				} // end handler
			*/

			LocalBuilder list2 = iLGenerator.DeclareLocal(typeof(List<Thing>));
			LocalBuilder minNum = iLGenerator.DeclareLocal(typeof(int));
			LocalBuilder resTotalAvailable = iLGenerator.DeclareLocal(typeof(int));
			LocalBuilder thingEnum = iLGenerator.DeclareLocal(typeof(IEnumerator<Thing>));

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

				if (
					i + 7 < instructionsList.Count && instructionsList[i + 7].opcode == OpCodes.Call &&
					(MethodInfo)instructionsList[i + 7].operand == (AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), "FindAvailableNearbyResources"))
					)
					{

					// List<Thing> list2 = new List<Thing>();
					//IL_011a: newobj instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::.ctor()
					//IL_011f: stloc.s 8
					instructionsList[i].opcode = OpCodes.Newobj;
					instructionsList[i].operand = AccessTools.Constructor(typeof(List<Thing>));
					yield return instructionsList[i];
					yield return new CodeInstruction(OpCodes.Stloc_S, list2);

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
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "def"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "stackLimit"));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "carryTracker"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Pawn_CarryTracker), "MaxStackSpaceEver"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Min"));
					yield return new CodeInstruction(OpCodes.Stloc_S, minNum.LocalIndex);

					// resTotalAvailable = 0;
					//IL_0177: ldc.i4.0
					//IL_0178: stloc.s 9
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Stloc_S, resTotalAvailable.LocalIndex);

					// list2.Add(foundRes);
					//IL_017a: ldloc.s 8
					//IL_017c: ldloc.s 7
					//IL_017e: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					yield return new CodeInstruction(OpCodes.Ldloc_S, list2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);

					// resTotalAvailable += foundRes.stackCount;
					//IL_0183: callvirt instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::Add(!0)
					//IL_0188: ldloc.s 9
					//IL_018a: ldloc.s 7
					//IL_018c: ldfld class ['Assembly-CSharp']Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_0191: ldfld int32 ['Assembly-CSharp']Verse.Thing::stackCount
					//IL_0196: add
					//IL_0197: stloc.s 9
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Thing>), "Add"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, resTotalAvailable.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "stackLimit"));
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Stloc_S, resTotalAvailable.LocalIndex);

					// if (resTotalAvailable < num)
					//IL_0199: ldloc.s 9
					//IL_019b: ldloc.s 10
					//IL_019d: bge IL_0235
					yield return new CodeInstruction(OpCodes.Ldloc_S, resTotalAvailable.LocalIndex);
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
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Position"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Map"));
					yield return new CodeInstruction(OpCodes.Ldc_R4, 5);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenRadial), "RadialDistinctThingsAround"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "GetEnumerator"));
					yield return new CodeInstruction(OpCodes.Stloc_S, thingEnum.LocalIndex);

					//.try
					//{
					// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
					//IL_01cc: br.s IL_021e
					Label IL_021E = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Br_S, IL_021E);

					// loop start (head: IL_021e)
					//label 021e
					//IL_01ce: ldloc.s 18
					//IL_01d0: callvirt instance !0 class [mscorlib] System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp'] Verse.Thing>::get_Current()
					//IL_01d5: stloc.s 19


					// if (resTotalAvailable >= num)
					//IL_01d7: ldloc.s 9
					//IL_01d9: ldloc.s 10
					//IL_01db: blt.s IL_01df

					// break;
					//IL_01dd: leave.s IL_0235

					// if (item.def == foundRes.def && GenAI.CanUseItemForWork(pawn, item))
					//label 01df
					//IL_01df: ldloc.s 19
					//IL_01e1: ldfld Verse.Thing::def
					//IL_01e6: ldloc.s 7
					//IL_01e8: ldfld Verse.Thing RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::foundRes
					//IL_01ed: ldfld Verse.Thing::def
					//IL_01f2: bne.un.s IL_021e

					//IL_01f4: ldloc.s 7
					//IL_01f6: ldfld class RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0' RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_1'::'CS$<>8__locals1'
					//IL_01fb: ldfld class ['Assembly-CSharp']
					//Verse.Pawn RimThreaded.WorkGiver_ConstructDeliverResources_Patch/'<>c__DisplayClass10_0'::pawn
					//IL_0200: ldloc.s 19
					//IL_0202: call bool['Assembly-CSharp'] Verse.AI.GenAI::CanUseItemForWork(class ['Assembly-CSharp'] Verse.Pawn, class ['Assembly-CSharp'] Verse.Thing)
					//IL_0207: brfalse.s IL_021e

					//list2.add(item)
					//IL_0209: ldloc.s 8
					//IL_020b: ldloc.s 19
					//IL_020d: callvirt instance void class [mscorlib] System.Collections.Generic.List`1<class ['Assembly-CSharp'] Verse.Thing>::Add(!0)

					// resTotalAvailable += item.stackCount;
					//IL_0212: ldloc.s 9
					//IL_0214: ldloc.s 19
					//IL_0216: ldfld int32['Assembly-CSharp']Verse.Thing::stackCount
					//IL_021b: add
					//IL_021c: stloc.s 9

					// foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
					//IL_021e: ldloc.s 18 thingenum
					//IL_0220: callvirt instance bool[mscorlib] System.Collections.IEnumerator::MoveNext()
					//IL_0225: brtrue.s IL_01ce
					// end loop

					
					//IL_0227: leave.s IL_0235
					//} // end .try
					//finally
					//{
					//IL_0229: ldloc.s 18 thingenum
					//IL_022b: brfalse.s IL_0234

					//IL_022d: ldloc.s 18 thingenum
					//IL_022f: callvirt instance void[mscorlib] System.IDisposable::Dispose()

					//label 0234
					//IL_0234: endfinally
					//} // end handler
					//label 0235







					Label IL_0477 = iLGenerator.DefineLabel();

					instructionsList[i].opcode = OpCodes.Ldarg_S;
					instructionsList[i].operand = 4;
					yield return instructionsList[i];
					yield return new CodeInstruction(OpCodes.Ldc_I4_2);
					yield return new CodeInstruction(OpCodes.Bne_Un, IL_0477);

					// int minX = end.minX;
					// sequence point: (line 257, col 17) to (line 257, col 42) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0377: ldloc.s 11 cellRect
					//IL_0379: ldfld int32 ['Assembly-CSharp']Verse.CellRect::minX
					// {
					//IL_037e: stloc.s 36
					yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CellRect), "minX"));
					yield return new CodeInstruction(OpCodes.Stloc_S, minNum.LocalIndex);

					// int minZ = end.minZ;
					// sequence point: (line 258, col 17) to (line 258, col 42) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0380: ldloc.s 11
					//IL_0382: ldfld int32 ['Assembly-CSharp']Verse.CellRect::minZ
					//IL_0387: stloc.s 37
					yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CellRect), "minZ"));
					yield return new CodeInstruction(OpCodes.Stloc_S, minZ.LocalIndex);

					// int maxX = end.maxX;
					// sequence point: (line 259, col 17) to (line 259, col 42) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0389: ldloc.s 15
					//IL_038b: ldfld int32 ['Assembly-CSharp']Verse.CellRect::maxX
					//IL_0390: stloc.s 38
					yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CellRect), "maxX"));
					yield return new CodeInstruction(OpCodes.Stloc_S, maxX.LocalIndex);

					// int maxZ = end.maxZ;
					// sequence point: (line 260, col 17) to (line 260, col 42) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0392: ldloc.s 15
					//IL_0394: ldfld int32 ['Assembly-CSharp']Verse.CellRect::maxZ
					//IL_0399: stloc.s 39
					yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CellRect), "maxZ"));
					yield return new CodeInstruction(OpCodes.Stloc_S, maxZ.LocalIndex);


					// if (!IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
					// sequence point: (line 261, col 17) to (line 261, col 95) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_039b: ldarg.0
					//IL_039c: ldloc.s 36
					//IL_039e: ldc.i4.1
					//IL_039f: add
					//IL_03a0: ldloc.s 37
					//IL_03a2: ldc.i4.1
					//IL_03a3: add
					//IL_03a4: ldloc.s 36
					//IL_03a6: ldc.i4.1
					//IL_03a7: add
					//IL_03a8: ldloc.s 37
					//IL_03aa: ldloc.s 36
					//IL_03ac: ldloc.s 37
					//IL_03ae: ldc.i4.1
					//IL_03af: add
					//IL_03b0: call instance bool Verse.AI.PathFinder_Target::IsCornerTouchAllowed(int32, int32, int32, int32, int32, int32)
					//IL_03b5: brtrue.s IL_03d2
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder), "IsCornerTouchAllowed"));
					Label IL_03d2 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brtrue_S, IL_03d2);

					//CodeInstruction nop = new CodeInstruction(OpCodes.Nop);
					//nop.labels.Add(IL_03d2);					
					//yield return nop;

					// list.Add(map.cellIndices.CellToIndex(minX, minZ));
					// sequence point: (line 263, col 21) to (line 263, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_03b7: ldloc.s 5
					//IL_03b9: ldarg.0
					//IL_03ba: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
					//IL_03bf: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
					//IL_03c4: ldloc.s 36
					//IL_03c6: ldloc.s 37
					//IL_03c8: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(int32, int32)
					// {
					//IL_03cd: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, disallowedCornerIndices.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(int), typeof(int) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Int32>), "Add"));

					// if (!IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
					// sequence point: (line 266, col 17) to (line 266, col 95) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_03d2: ldarg.0
					//IL_03d3: ldloc.s 36
					//IL_03d5: ldc.i4.1
					//IL_03d6: add
					//IL_03d7: ldloc.s 39
					//IL_03d9: ldc.i4.1
					//IL_03da: sub
					//IL_03db: ldloc.s 36
					//IL_03dd: ldc.i4.1
					//IL_03de: add
					//IL_03df: ldloc.s 39
					//IL_03e1: ldloc.s 36
					//IL_03e3: ldloc.s 39
					//IL_03e5: ldc.i4.1
					//IL_03e6: sub
					//IL_03e7: call instance bool Verse.AI.PathFinder_Target::IsCornerTouchAllowed(int32, int32, int32, int32, int32, int32)
					//IL_03ec: brtrue.s IL_0409
					CodeInstruction ldarg_0_IL_03d2 = new CodeInstruction(OpCodes.Ldarg_0);
					ldarg_0_IL_03d2.labels.Add(IL_03d2);
					yield return ldarg_0_IL_03d2;
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder), "IsCornerTouchAllowed"));
					Label IL_0409 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brtrue_S, IL_0409);

					// list.Add(map.cellIndices.CellToIndex(minX, maxZ));
					// sequence point: (line 268, col 21) to (line 268, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_03ee: ldloc.s 5
					//IL_03f0: ldarg.0
					//IL_03f1: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
					//IL_03f6: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
					//IL_03fb: ldloc.s 36
					//IL_03fd: ldloc.s 39
					//IL_03ff: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(int32, int32)
					// {
					//IL_0404: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, disallowedCornerIndices.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, minX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(int), typeof(int) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Int32>), "Add"));

					// if (!IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
					// sequence point: (line 271, col 17) to (line 271, col 95) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0409: ldarg.0
					//IL_040a: ldloc.s 38
					//IL_040c: ldc.i4.1
					//IL_040d: sub
					//IL_040e: ldloc.s 39
					//IL_0410: ldc.i4.1
					//IL_0411: sub
					//IL_0412: ldloc.s 38
					//IL_0414: ldc.i4.1
					//IL_0415: sub
					//IL_0416: ldloc.s 39
					//IL_0418: ldloc.s 38
					//IL_041a: ldloc.s 39
					//IL_041c: ldc.i4.1
					//IL_041d: sub
					//IL_041e: call instance bool Verse.AI.PathFinder_Target::IsCornerTouchAllowed(int32, int32, int32, int32, int32, int32)
					//IL_0423: brtrue.s IL_0440
					CodeInstruction ldarg_0_IL_0409 = new CodeInstruction(OpCodes.Ldarg_0);
					ldarg_0_IL_0409.labels.Add(IL_0409);
					yield return ldarg_0_IL_0409;
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder), "IsCornerTouchAllowed"));
					Label IL_0440 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brtrue_S, IL_0440);

					// list.Add(map.cellIndices.CellToIndex(maxX, maxZ));
					// sequence point: (line 273, col 21) to (line 273, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0425: ldloc.s 5
					//IL_0427: ldarg.0
					//IL_0428: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
					//IL_042d: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
					//IL_0432: ldloc.s 38
					//IL_0434: ldloc.s 39
					//IL_0436: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(int32, int32)
					// {
					//IL_043b: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, disallowedCornerIndices.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(int), typeof(int) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Int32>), "Add"));

					// if (!IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
					// sequence point: (line 276, col 17) to (line 276, col 95) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_0440: ldarg.0
					//IL_0441: ldloc.s 38
					//IL_0443: ldc.i4.1
					//IL_0444: sub
					//IL_0445: ldloc.s 37
					//IL_0447: ldc.i4.1
					//IL_0448: add
					//IL_0449: ldloc.s 38
					//IL_044b: ldc.i4.1
					//IL_044c: sub
					//IL_044d: ldloc.s 37
					//IL_044f: ldloc.s 38
					//IL_0451: ldloc.s 37
					//IL_0453: ldc.i4.1
					//IL_0454: add
					//IL_0455: call instance bool Verse.AI.PathFinder_Target::IsCornerTouchAllowed(int32, int32, int32, int32, int32, int32)
					//IL_045a: brtrue.s IL_0477
					CodeInstruction ldarg_0_IL_0440 = new CodeInstruction(OpCodes.Ldarg_0);
					ldarg_0_IL_0440.labels.Add(IL_0440);
					yield return ldarg_0_IL_0440;
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Sub);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder), "IsCornerTouchAllowed"));
					yield return new CodeInstruction(OpCodes.Brtrue_S, IL_0477);

					// list.Add(map.cellIndices.CellToIndex(maxX, minZ));
					// sequence point: (line 278, col 21) to (line 278, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
					//IL_045c: ldloc.s 5
					//IL_045e: ldarg.0
					//IL_045f: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
					//IL_0464: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
					//IL_0469: ldloc.s 38
					//IL_046b: ldloc.s 37
					//IL_046d: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(int32, int32)
					// {
					//IL_0472: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, disallowedCornerIndices.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, maxX.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, minZ.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(int), typeof(int) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Int32>), "Add"));
					instructionsList[i + 5].labels.Add(IL_0477);
					i += 4;
				} else if (					
					i + 3 < instructionsList.Count && instructionsList[i + 3].opcode == OpCodes.Call && instructionsList[i + 3].operand.ToString().Equals("Void InitStatusesAndPushStartNode(Int32 ByRef, Verse.IntVec3)")
					) { 
					// num = (ushort)(num + 2);
					//IL_033e: ldloc.1   (statusOpenValue2.LocalIndex)
					//IL_033f: ldc.i4.2
					//IL_0340: add
					//IL_0341: conv.u2
					//IL_0342: stloc.1   (statusOpenValue2.LocalIndex)	
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = statusOpenValue2.LocalIndex;
					//instructionsList[i + 5].opcode = OpCodes.Nop;
					//instructionsList[i + 5].operand = null;
					//instructionsList[i].labels.Add(IL_0477);
					yield return instructionsList[i];					
					yield return new CodeInstruction(OpCodes.Ldc_I4_2);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Conv_U2);
					yield return new CodeInstruction(OpCodes.Stloc, statusOpenValue2.LocalIndex);
					
					// num2 = (ushort)(num2 + 2);
					//IL_0343: ldloc.2   (statusClosedValue2.LocalIndex)
					//IL_0344: ldc.i4.2
					//IL_0345: add
					//IL_0346: conv.u2
					//IL_0347: stloc.2   (statusClosedValue2.LocalIndex)					
					yield return new CodeInstruction(OpCodes.Ldloc, statusClosedValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_2);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Conv_U2);
					yield return new CodeInstruction(OpCodes.Stloc, statusClosedValue2.LocalIndex);
										
					// if (num2 >= 65435)
					//IL_0348: ldloc.2   (statusClosedValue2.LocalIndex)
					//IL_0349: ldc.i4 65435
					//IL_034e: blt.s IL_04ad		(goto IL_04ad)
					yield return new CodeInstruction(OpCodes.Ldloc, statusClosedValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4, 65435);
					Label IL_04AD = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Blt_S, IL_04AD);
										
					// for (int i = 0; i < array.Length; i++)
					//IL_0350: ldc.i4.0
					// {
					//IL_0351: stloc.s 33   (loopIndex.LocalIndex)
					// array[i].status = 0;
					//IL_0353: br.s IL_0369		(goto IL_0369)
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Stloc_S, loopIndex.LocalIndex);

					Label IL_04A2 = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Br_S, IL_04A2);					

					// loop start (head: IL_0369)
						//IL_0355: ldloc.0   (calcGrid2.LocalIndex) (add label IL_0355)
						//IL_0356: ldloc.s 33   (loopIndex.LocalIndex)
						//IL_0358: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
						//IL_035d: ldc.i4.0		
						// {
						//IL_035e: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
					CodeInstruction ldloc_0_IL_048E = new CodeInstruction(OpCodes.Ldloc, calcGrid2.LocalIndex);
					Label IL_048E = iLGenerator.DefineLabel();
					ldloc_0_IL_048E.labels.Add(IL_048E);
					yield return ldloc_0_IL_048E;
					yield return new CodeInstruction(OpCodes.Ldloc_S, loopIndex.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "status"));

					// for (int i = 0; i < array.Length; i++)
					//IL_0363: ldloc.s 33   (loopIndex.LocalIndex)
					//IL_0365: ldc.i4.1
					//IL_0366: add
					//IL_0367: stloc.s 33   (loopIndex.LocalIndex)
					yield return new CodeInstruction(OpCodes.Ldloc_S, loopIndex.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Add);
					yield return new CodeInstruction(OpCodes.Stloc_S, loopIndex.LocalIndex);

					// for (int i = 0; i < array.Length; i++)
					//IL_0369: ldloc.s 33   (loopIndex.LocalIndex) (add label IL_0369)
					//IL_036b: ldloc.0   (calcGrid2.LocalIndex)
					//IL_036c: ldlen
					//IL_036d: conv.i4
					//IL_036e: blt.s IL_0355   (goto IL_0355)
					CodeInstruction ldloc_S_IL_04A2 = new CodeInstruction(OpCodes.Ldloc_S, loopIndex.LocalIndex);
					ldloc_S_IL_04A2.labels.Add(IL_04A2);
					yield return ldloc_S_IL_04A2;
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldlen);
					yield return new CodeInstruction(OpCodes.Conv_I4);
					yield return new CodeInstruction(OpCodes.Blt_S, IL_048E);

					//CodeInstruction ci = new CodeInstruction(OpCodes.Nop);
					//ci.labels.Add(IL_0374);
					//yield return ci;

					// end loop
					// num = 1;
					//IL_0370: ldc.i4.1
					//IL_0371: stloc.1   (statusOpenValue2.LocalIndex)
					// num2 = 2;
					//IL_0372: ldc.i4.2
					//IL_0373: stloc.2   (statusClosedValue2.LocalIndex)					
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Stloc_S, statusOpenValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldc_I4_2);
					yield return new CodeInstruction(OpCodes.Stloc_S, statusClosedValue2.LocalIndex);
					
					// curIndex = cellIndices.CellToIndex(start);
					//IL_0374: ldarg.0	(add IL_0374)
					//IL_0375: ldfld class ['Assembly-CSharp']Verse.CellIndices Verse.AI.PathFinder_Target::cellIndices
					//IL_037a: ldarg.1
					//IL_037b: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(valuetype ['Assembly-CSharp']Verse.IntVec3)
					//IL_0380: stloc 3
					CodeInstruction ldarg_0_IL_04AD = new CodeInstruction(OpCodes.Ldarg_0);
					ldarg_0_IL_04AD.labels.Add(IL_04AD);
					yield return ldarg_0_IL_04AD;
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(IntVec3) }));
					yield return new CodeInstruction(OpCodes.Stloc_3); //curIndex
					
					// array[curIndex].knownCost = 0;
					//IL_0382: ldloc.0   (calcGrid2.LocalIndex)
					//IL_0383: ldloc 3
					//IL_0385: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_038a: ldc.i4.0
					//IL_038b: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "knownCost"));

					// array[curIndex].heuristicCost = 0;
					//IL_0390: ldloc.0   (calcGrid2.LocalIndex)
					//IL_0391: ldloc 3
					//IL_0393: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_0398: ldc.i4.0
					//IL_0399: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::heuristicCost
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0, statusClosedValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "heuristicCost"));

					// array[curIndex].costNodeCost = 0;
					//IL_039e: ldloc.0   (calcGrid2.LocalIndex)
					//IL_039f: ldloc.s 3
					//IL_03a1: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_03a6: ldc.i4.0
					//IL_03a7: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::costNodeCost
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0, statusClosedValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "costNodeCost"));

					// array[curIndex].parentIndex = curIndex;
					//IL_03ac: ldloc.0   (calcGrid2.LocalIndex)
					//IL_03ad: ldloc 3
					//IL_03af: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_03b4: ldloc 3
					//IL_03b6: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "parentIndex"));

					// array[curIndex].status = num;
					//IL_03bb: ldloc.0   (calcGrid2.LocalIndex)
					//IL_03bc: ldloc 3
					//IL_03be: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_03c3: ldloc.1   (statusOpenValue2.LocalIndex)
					//IL_03c4: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldloc_S, statusOpenValue2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(nodeFastType, "status"));

					// openList.Clear();
					// fastPriorityQueue.Clear();
					//IL_0502: ldloc.3
					//IL_0503: callvirt instance void class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Clear()
					yield return new CodeInstruction(OpCodes.Ldloc_S, openList.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(fastPriorityQueueCostNodeType, "Clear"));

					// openList.Push(new CostNode(curIndex, 0));
					// fastPriorityQueue.Push(new CostNode(num3, 0));
					//IL_0508: ldloc.3
					//IL_0509: ldloc.s 9
					//IL_050b: ldc.i4.0
					//IL_050c: newobj instance void Verse.AI.PathFinder_Target / CostNode::.ctor(int32, int32)
					//IL_0511: callvirt instance void class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Push(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, openList.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Newobj, costNodeType.GetConstructor(new Type[] { typeof(Int32), typeof(Int32) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(fastPriorityQueueCostNodeType, "Push"));
					//yield return new CodeInstruction(OpCodes.Ldloc_S, openList.LocalIndex);

					i += 3;
					
				}
				//call instance class ['Assembly-CSharp']Verse.AI.PawnPath Verse.AI.PathFinder_Original::FinalizedPath(int32, bool)
				else if (
					i + 3 < instructionsList.Count && instructionsList[i+3].opcode == OpCodes.Call && instructionsList[i+3].operand.ToString().Equals("Verse.AI.PawnPath FinalizedPath(Int32, Boolean)")
                    )
                {
					// PawnPath pawnPath = new PawnPath();
					//IL_0535: newobj instance void ['Assembly-CSharp']Verse.AI.PawnPath::.ctor()
					// {
					//IL_0545: stloc.s 39   (emptyPawnPath.LocalIndex)
					// int num13 = curIndex;
					//IL_0547: ldloc.3   (curIndex)
					//IL_0549: stloc.s 40   (num1.LocalIndex)
					instructionsList[i].opcode = OpCodes.Newobj;
					instructionsList[i].operand = AccessTools.Constructor(typeof(PawnPath));
					yield return instructionsList[i];
					yield return new CodeInstruction(OpCodes.Stloc_S, emptyPawnPath.LocalIndex);

					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Stloc_S, num1.LocalIndex);

					// loop start (head: IL_054b)
					// int parentIndex = array[num13].parentIndex;
					//IL_054b: ldloc.0   (calcGrid2.LocalIndex) (add label IL_054b)
					//IL_054c: ldloc.s 40   (num1.LocalIndex)
					//IL_054e: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_0553: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
					//IL_0558: stloc.s 41   (parentIndex.LocalIndex)
					CodeInstruction ldloc_0_IL_0667 = new CodeInstruction(OpCodes.Ldloc, calcGrid2.LocalIndex);
					Label IL_0667 = iLGenerator.DefineLabel();
					ldloc_0_IL_0667.labels.Add(IL_0667);
					yield return ldloc_0_IL_0667;
					yield return new CodeInstruction(OpCodes.Ldloc_S, num1.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(nodeFastType, "parentIndex"));
					yield return new CodeInstruction(OpCodes.Stloc_S, parentIndex.LocalIndex);

					// emptyPawnPath.AddNode(map.cellIndices.IndexToCell(num13));
					//IL_055a: ldloc.s 39   (emptyPawnPath.LocalIndex)
					//IL_055c: ldarg.0
					//IL_055d: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
					//IL_0562: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
					//IL_0567: ldloc.s 40   (num1.LocalIndex)
					//IL_0569: callvirt instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellIndices::IndexToCell(int32)
					//IL_056e: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::AddNode(valuetype ['Assembly-CSharp']Verse.IntVec3)
					yield return new CodeInstruction(OpCodes.Ldloc_S, emptyPawnPath.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "map"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc_S, num1.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "IndexToCell"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PawnPath), "AddNode"));

					// if (num13 == parentIndex)
					//IL_0573: ldloc.s 40   (num1.LocalIndex)
					//IL_0575: ldloc.s 41   (parentIndex.LocalIndex)
					//IL_0577: beq.s IL_057f   (goto IL_057f)
					yield return new CodeInstruction(OpCodes.Ldloc_S, num1.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_S, parentIndex.LocalIndex);
					Label IL_069b = iLGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Beq_S, IL_069b);

					// num13 = parentIndex;
					//IL_0579: ldloc.s 41   (parentIndex.LocalIndex)
					//IL_057b: stloc.s 40   (num1.LocalIndex)
					// while (true)
					//IL_057d: br.s IL_054b   (goto IL_054b)
					// end loop
					yield return new CodeInstruction(OpCodes.Ldloc_S, parentIndex.LocalIndex);
					yield return new CodeInstruction(OpCodes.Stloc_S, num1.LocalIndex);

					yield return new CodeInstruction(OpCodes.Br_S, IL_0667);

					// emptyPawnPath.SetupFound(array[curIndex].knownCost, flag8);
					//IL_057f: ldloc.s 39   (emptyPawnPath.LocalIndex) (add label IL_057f)
					//IL_0581: ldloc.0   (calcGrid2.LocalIndex)
					//IL_0582: ldloc_3   (curIndex)
					//IL_0584: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					//IL_0589: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
					//IL_058e: conv.r4
					//IL_058f: ldloc.s 20   (flag8)
					//IL_0591: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::SetupFound(float32, bool)
					CodeInstruction ldloc_s_IL_069b = new CodeInstruction(OpCodes.Ldloc_S, emptyPawnPath.LocalIndex);
					ldloc_s_IL_069b.labels.Add(IL_069b);
					yield return ldloc_s_IL_069b;
					yield return new CodeInstruction(OpCodes.Ldloc_S, calcGrid2.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3);
					yield return new CodeInstruction(OpCodes.Ldelema, nodeFastType);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(nodeFastType, "knownCost"));
					yield return new CodeInstruction(OpCodes.Conv_R4);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 20); //flag8
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PawnPath), "SetupFound"));

					// return emptyPawnPath;
					//IL_0596: ldloc.s 39   (emptyPawnPath.LocalIndex)
					yield return new CodeInstruction(OpCodes.Ldloc_S, emptyPawnPath.LocalIndex);
					i += 3;
				}

				//ldsfld valuetype Verse.AI.PathFinder_Original/PathFinderNodeFast[] Verse.AI.PathFinder_Original::calcGrid
				else if (
						instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("Verse.AI.PathFinder+PathFinderNodeFast[] calcGrid")
					)
				{
					//ldloc.0   (calcGrid2.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = calcGrid2.LocalIndex;
					yield return instructionsList[i];
				}

				//ldsfld uint16 Verse.AI.PathFinder_Original::statusOpenValue
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.UInt16 statusOpenValue")
					)
				{
					//ldloc.1   (statusOpenValue2.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = statusOpenValue2.LocalIndex;
					yield return instructionsList[i];
				}

				//ldsfld uint16 Verse.AI.PathFinder_Original::statusClosedValue
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.UInt16 statusClosedValue")
					)
				{
					//ldloc.2(statusClosedValue2.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = statusClosedValue2.LocalIndex;
					yield return instructionsList[i];
				}
				// ldarg.0
				// ldfld class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
				else if (
					i + 1 < instructionsList.Count &&
					instructionsList[i].opcode == OpCodes.Ldarg_0 &&
					instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("Verse.FastPriorityQueue`1[Verse.AI.PathFinder+CostNode] openList")
					)
				{
					// ldloc.3 (openlist)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = openList.LocalIndex;
					yield return instructionsList[i];
					i++;
				}
				/*
				// ldarg.0
				// ldfld class [mscorlib] System.Collections.Generic.List`1<int32> Verse.AI.PathFinder_Target::disallowedCornerIndices
				else if (
					i + 1 < instructionsList.Count &&
					instructionsList[i].opcode == OpCodes.Ldarg_0 &&
					instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("List<int32> disallowedCornerIndices")
					)
				{
					// ldloc.3 (openlist)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = disallowedCornerIndices.LocalIndex;
					yield return instructionsList[i];
					i++;
				}
				*/
				// ldarg.0
				// ldfld class ['Assembly-CSharp']Verse.AI.RegionCostCalculatorWrapper Verse.AI.PathFinder_Target::regionCostCalculator
				else if (
					i + 1 < instructionsList.Count &&
					instructionsList[i].opcode == OpCodes.Ldarg_0 &&
					instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("Verse.AI.RegionCostCalculatorWrapper regionCostCalculator")
					)
				{
					// ldloc.3 (openlist)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = regionCostCalculator.LocalIndex;
					yield return instructionsList[i];
					i++;
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
