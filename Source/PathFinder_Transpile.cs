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
using static RimThreaded.PathFinder_Patch;

namespace RimThreaded
{
    public class PathFinder_Transpile
    {
        public static IEnumerable<CodeInstruction> FindPath(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			//Type nodeFastType = AccessTools.TypeByName("Verse.AI.PathFinder+PathFinderNodeFast");
			Type nodeFastType = typeof(PathFinderNodeFast);
			Type nodeFastTypeArray = nodeFastType.MakeArrayType();
			LocalBuilder local_calcGrid = iLGenerator.DeclareLocal(nodeFastTypeArray);
			LocalBuilder local_statusOpenValue = iLGenerator.DeclareLocal(typeof(ushort));
			LocalBuilder local_statusClosedValue = iLGenerator.DeclareLocal(typeof(ushort));
			LocalBuilder local_regionCostCalculatorWrapper = iLGenerator.DeclareLocal(typeof(RegionCostCalculatorWrapper));
			Type costNodeType = AccessTools.TypeByName("Verse.AI.PathFinder+CostNode");
			Type costNodeType2 = typeof(CostNode2);
			Type icomparerCostNodeType1 = typeof(IComparer<>).MakeGenericType(costNodeType);
			Type icomparerCostNodeType2 = typeof(IComparer<>).MakeGenericType(costNodeType2);
			Type fastPriorityQueueCostNodeType1 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType);
			Type fastPriorityQueueCostNodeType2 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType2);
			LocalBuilder local_openList = iLGenerator.DeclareLocal(fastPriorityQueueCostNodeType2);
			//LocalBuilder tID = iLGenerator.DeclareLocal(typeof(int));
			//LocalBuilder size = iLGenerator.DeclareLocal(typeof(int));

			List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;

			// int managedThreadId = Thread.CurrentThread.ManagedThreadId;
			//IL_0000: call class [mscorlib]System.Threading.Thread[mscorlib] System.Threading.Thread::get_CurrentThread()
			//IL_0005: callvirt instance int32[mscorlib] System.Threading.Thread::get_ManagedThreadId()
			//IL_000a: stloc.0
			//yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thread), "get_CurrentThread"));
			//yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thread), "get_ManagedThreadId"));
			//yield return new CodeInstruction(OpCodes.Stloc, tID.LocalIndex);

			// PathFinderNodeFast[] calcGrid = getCalcGrid(managedThreadId, __instance);
			//IL_000b: ldloc.0
			//IL_000c: ldarg.0
			//IL_000d: call valuetype RimThreaded.PathFinder_Patch / PathFinderNodeFast[] RimThreaded.PathFinder_Patch::getCalcGrid(int32, class ['Assembly-CSharp'] Verse.AI.PathFinder)
			//IL_0012: stloc.1
			//yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "getCalcGrid"));
			yield return new CodeInstruction(OpCodes.Stloc, local_calcGrid.LocalIndex);

			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "getRegionCostCalculatorWrapper"));
			yield return new CodeInstruction(OpCodes.Stloc, local_regionCostCalculatorWrapper.LocalIndex);

			// FastPriorityQueue<CostNode2> openList = getOpenList(managedThreadId);
			//IL_0013: ldloc.0
			//IL_0014: call class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2> RimThreaded.PathFinder_Patch::getOpenList(int32)
			//IL_0019: stloc.2
			//yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "getOpenList"));
			yield return new CodeInstruction(OpCodes.Stloc, local_openList.LocalIndex);

			// ushort local_statusOpenValue = getOpenValue(managedThreadId);
			//IL_001a: ldloc.0
			//IL_001b: call uint16 RimThreaded.PathFinder_Patch::getOpenValue(int32)
			//IL_0020: stloc.3
			//yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "getOpenValue"));
			yield return new CodeInstruction(OpCodes.Stloc, local_statusOpenValue.LocalIndex);

			// ushort local_statusClosedValue = getClosedValue(managedThreadId);
			//IL_0021: ldloc.0
			//IL_0022: call uint16 RimThreaded.PathFinder_Patch::getClosedValue(int32)
			//IL_0027: stloc.s 4
			//yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "getClosedValue"));
			yield return new CodeInstruction(OpCodes.Stloc, local_statusClosedValue.LocalIndex);

			/*
			// int num = mapSizeXField(__instance) * mapSizeZField(__instance);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "mapSizeX"));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "mapSizeZ"));
			yield return new CodeInstruction(OpCodes.Mul);
			yield return new CodeInstruction(OpCodes.Stloc, size.LocalIndex);
			
			// if (!calcGrids.TryGetValue(managedThreadId, out PathFinderNodeFast[] value) || value.Length < num)
			
			//IL_0025: ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<int32, valuetype RimThreaded.PathFinder_Patch/PathFinderNodeFast[]> RimThreaded.PathFinder_Patch::calcGrids
			//IL_002a: ldloc.0
			//IL_002b: ldloca.s 2
			//IL_002d: callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<int32, valuetype RimThreaded.PathFinder_Patch/PathFinderNodeFast[]>::TryGetValue(!0, !1&)
			//IL_0032: brfalse.s IL_003a
			//IL_0034: ldloc.2
			//IL_0035: ldlen
			//IL_0036: conv.i4
			//IL_0037: ldloc.1
			//IL_0038: bge.s IL_004d
			
			yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "calcGrids"));
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloca, local_calcGrid.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, PathFinder_Patch.PathFinderNodeFast[]>), "TryGetValue"));
			Label il_003a = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, il_003a);

			yield return new CodeInstruction(OpCodes.Ldloc, local_calcGrid.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldlen);
			yield return new CodeInstruction(OpCodes.Conv_I4);
			yield return new CodeInstruction(OpCodes.Ldloc, size.LocalIndex);
			Label il_004d = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Bge_S, il_004d);
			
			// value = new PathFinderNodeFast[num];
			//IL_003a: ldloc.1
			//IL_003b: newarr RimThreaded.PathFinder_Patch / PathFinderNodeFast
			//IL_0040: stloc.2
			CodeInstruction ci_003a = new CodeInstruction(OpCodes.Ldloc, size.LocalIndex);
			ci_003a.labels.Add(il_003a);
			yield return ci_003a;

			yield return new CodeInstruction(OpCodes.Newarr, nodeFastType);
			yield return new CodeInstruction(OpCodes.Stloc, local_calcGrid.LocalIndex);
						
			// calcGrids[managedThreadId] = value;
			//IL_0041: ldsfld class [mscorlib] System.Collections.Generic.Dictionary`2<int32, valuetype RimThreaded.PathFinder_Patch/PathFinderNodeFast[]> RimThreaded.PathFinder_Patch::calcGrids
			//IL_0046: ldloc.0
			//IL_0047: ldloc.2
			//IL_0048: callvirt instance void class [mscorlib] System.Collections.Generic.Dictionary`2<int32, valuetype RimThreaded.PathFinder_Patch/PathFinderNodeFast[]>::set_Item(!0, !1)
			yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "calcGrids"));
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloc, local_calcGrid.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, 
				AccessTools.Method(typeof(Dictionary<int, PathFinder_Patch.PathFinderNodeFast[]>), "set_Item"));

			
			// if (!openLists.TryGetValue(managedThreadId, out FastPriorityQueue<CostNode2> value2))
			//IL_004d: ldsfld class [mscorlib] System.Collections.Generic.Dictionary`2<int32, class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2>> RimThreaded.PathFinder_Patch::openLists
			//IL_0052: ldloc.0
			//IL_0053: ldloca.s 3
			//IL_0055: callvirt instance bool class [mscorlib] System.Collections.Generic.Dictionary`2<int32, class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2>>::TryGetValue(!0, !1&)
			//IL_005a: brtrue.s IL_0073
			CodeInstruction ci_004d = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "openLists"));
			ci_004d.labels.Add(il_004d);
			yield return ci_004d;
						
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloca, local_statusOpenValue);
			yield return new CodeInstruction(OpCodes.Callvirt, 
				AccessTools.Method(typeof(Dictionary<int, FastPriorityQueue<PathFinder_Patch.CostNode2>>), "TryGetValue"));
			Label il_0073 = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brtrue, il_0073);
						
			// value2 = new FastPriorityQueue<CostNode2>(new CostNodeComparer2());
			//IL_005c: newobj instance void RimThreaded.PathFinder_Patch / CostNodeComparer2::.ctor()
			//IL_0061: newobj instance void class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2>::.ctor(class [mscorlib] System.Collections.Generic.IComparer`1<!0>)
			//IL_0066: stloc.3
			yield return new CodeInstruction(OpCodes.Newobj, typeof(CostNodeComparer2).GetConstructor(Type.EmptyTypes));
			yield return new CodeInstruction(OpCodes.Newobj, fastPriorityQueueCostNodeType2.GetConstructor(new Type[] { icomparerCostNodeType2 }));
			yield return new CodeInstruction(OpCodes.Stloc, local_openList.LocalIndex);

			// openLists[managedThreadId] = value2;
			//IL_0067: ldsfld class [mscorlib] System.Collections.Generic.Dictionary`2<int32, class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2>> RimThreaded.PathFinder_Patch::openLists
			//IL_006c: ldloc.0
			//IL_006d: ldloc.3
			//IL_006e: callvirt instance void class [mscorlib] System.Collections.Generic.Dictionary`2<int32, class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode2>>::set_Item(!0, !1)
			yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "openLists"));
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloc, local_openList.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, FastPriorityQueue<PathFinder_Patch.CostNode2>>), "set_Item"));

			// if (!openValues.TryGetValue(managedThreadId, out ushort value3))
			//IL_0073: ldsfld class [mscorlib] System.Collections.Generic.Dictionary`2<int32, uint16> RimThreaded.PathFinder_Patch::openValues
			//IL_0078: ldloc.0
			//IL_0079: ldloca.s 4
			//IL_007b: callvirt instance bool class [mscorlib] System.Collections.Generic.Dictionary`2<int32, uint16>::TryGetValue(!0, !1&)
			//IL_0080: brtrue.s IL_0085
			CodeInstruction ci_0073 = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "openValues"));
			ci_0073.labels.Add(il_0073);
			yield return ci_0073;
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloca, local_statusOpenValue.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, ushort>), "TryGetValue"));
			Label il_0085 = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brtrue, il_0085);

			// value3 = 1;
			//IL_0082: ldc.i4.1
			//IL_0083: stloc.s 4
			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Stloc, local_statusOpenValue.LocalIndex);

			// if (!closedValues.TryGetValue(managedThreadId, out ushort value4))
			//IL_0085: ldsfld class [mscorlib] System.Collections.Generic.Dictionary`2<int32, uint16> RimThreaded.PathFinder_Patch::closedValues
			//IL_008a: ldloc.0
			//IL_008b: ldloca.s 5
			//IL_008d: callvirt instance bool class [mscorlib] System.Collections.Generic.Dictionary`2<int32, uint16>::TryGetValue(!0, !1&)
			//IL_0092: brtrue.s IL_0097
			CodeInstruction ci__0085 = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "closedValues"));
			ci__0085.labels.Add(il_0085);
			yield return ci__0085;
			yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
			yield return new CodeInstruction(OpCodes.Ldloca, local_statusClosedValue.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, ushort>), "TryGetValue"));
			Label il_0097 = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brtrue, il_0097);

			// value4 = 2;
			//IL_0094: ldc.i4.2	
			//IL_0095: stloc.s 5
			yield return new CodeInstruction(OpCodes.Ldc_I4_2);
			yield return new CodeInstruction(OpCodes.Stloc, local_statusClosedValue.LocalIndex);
			instructionsList[0].labels.Add(il_0097);

			//yield return new CodeInstruction(OpCodes.Newobj, costNodeType2.GetConstructor(Type.EmptyTypes));
			//yield return new CodeInstruction(OpCodes.Newobj, fastPriorityQueueCostNodeType2.GetConstructor(new Type[] { icomparerCostNodeType2 }));
			//yield return new CodeInstruction(OpCodes.Stloc, local_openList.LocalIndex);
			*/

			while (i < instructionsList.Count)
			{
				// InitStatusesAndPushStartNode(ref curIndex, start);
				//IL_0363: ldarg.0
				//IL_0364: ldloca.s 3
				//IL_0366: ldarg.1
				//IL_0367: call instance void Verse.AI.PathFinder::InitStatusesAndPushStartNode(int32 &, valuetype Verse.IntVec3)
				if (
					i + 3 < instructionsList.Count && 
					instructionsList[i + 3].opcode == OpCodes.Call && 
					instructionsList[i + 3].operand.ToString().Equals("Void InitStatusesAndPushStartNode(Int32 ByRef, Verse.IntVec3)")
					) {
					//IL_043b: ldsfld class ['0Harmony'] HarmonyLib.AccessTools/FieldRef`2<class ['Assembly-CSharp'] Verse.AI.PathFinder, class ['Assembly-CSharp'] Verse.CellIndices> RimThreaded.PathFinder_Patch::cellIndicesField
					//IL_0440: ldarg.0
					//IL_0441: callvirt instance !1& class ['0Harmony'] HarmonyLib.AccessTools/FieldRef`2<class ['Assembly-CSharp'] Verse.AI.PathFinder, class ['Assembly-CSharp'] Verse.CellIndices>::Invoke(!0)
					//IL_0446: ldind.ref
					//IL_0447: ldloca.s 7
					//IL_0449: ldarg.2
					//IL_044a: ldloc.0
					//IL_044b: ldloc.3
					//IL_044c: ldloca.s 1
					//IL_044e: ldloca.s 2
					//IL_0450: call void RimThreaded.PathFinder_Patch::InitStatusesAndPushStartNode2(class ['Assembly-CSharp'] Verse.CellIndices, int32&, valuetype['Assembly-CSharp'] Verse.IntVec3, valuetype RimThreaded.PathFinder_Patch/PathFinderNodeFast[], class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype RimThreaded.PathFinder_Patch/CostNode>, uint16&, uint16&)

					instructionsList[i].opcode = OpCodes.Nop;
					yield return instructionsList[i];
					i++;
					yield return instructionsList[i];
					i++;
					yield return instructionsList[i];
					i+=2;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc, local_calcGrid.LocalIndex);
					//yield return new CodeInstruction(OpCodes.Ldarg_0);
					//yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "openList"));
					yield return new CodeInstruction(OpCodes.Ldloca, local_statusOpenValue.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloca, local_statusClosedValue.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "InitStatusesAndPushStartNode3"));

					// openList.Clear();
					// fastPriorityQueue.Clear();
					//IL_0502: ldloc.3
					//IL_0503: callvirt instance void class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Clear()
					yield return new CodeInstruction(OpCodes.Ldloc_S, local_openList.LocalIndex);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(fastPriorityQueueCostNodeType2, "Clear"));

					// openList.Push(new CostNode(curIndex, 0));
					// fastPriorityQueue.Push(new CostNode(num3, 0));
					//IL_0508: ldloc.3
					//IL_0509: ldloc.s 9
					//IL_050b: ldc.i4.0
					//IL_050c: newobj instance void Verse.AI.PathFinder_Target / CostNode::.ctor(int32, int32)
					//IL_0511: callvirt instance void class ['Assembly-CSharp'] Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Push(!0)
					yield return new CodeInstruction(OpCodes.Ldloc_S, local_openList.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc_3); //curIndex
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					Log.Message(costNodeType2.GetConstructor(new Type[] { typeof(Int32), typeof(Int32) }).ToString());
					yield return new CodeInstruction(OpCodes.Newobj, costNodeType2.GetConstructor(new Type[] { typeof(Int32), typeof(Int32) }));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(fastPriorityQueueCostNodeType2, "Push"));
					//yield return new CodeInstruction(OpCodes.Ldloc_S, openList.LocalIndex);
				}
				//call instance class ['Assembly-CSharp']Verse.AI.PawnPath Verse.AI.PathFinder_Original::FinalizedPath(int32, bool)
				else if (
					i + 3 < instructionsList.Count &&
					instructionsList[i + 3].opcode == OpCodes.Call &&
					instructionsList[i + 3].operand.ToString().Equals("Verse.AI.PawnPath FinalizedPath(Int32, Boolean)")
					)
				{
					// PawnPath pawnPath = new PawnPath();
					//IL_0535: newobj instance void ['Assembly-CSharp']Verse.AI.PawnPath::.ctor()
					//IL_0545: stloc.s 39   (emptyPawnPath.LocalIndex)
					// int num13 = curIndex;
					//IL_0547: ldloc.3   (curIndex)
					//IL_0549: stloc.s 40   (num1.LocalIndex)
					instructionsList[i].opcode = OpCodes.Nop;
					yield return instructionsList[i];
					i++;
					yield return instructionsList[i];
					i++;
					yield return instructionsList[i];
					i+=2;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc, local_calcGrid.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "FinalizedPath2"));
				}

				//ldsfld valuetype Verse.AI.PathFinder_Original/PathFinderNodeFast[] Verse.AI.PathFinder_Original::calcGrid
				else if (
						instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("Verse.AI.PathFinder+PathFinderNodeFast[] calcGrid")
					)
				{
					//ldloc.0   (local_calcGrid.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = local_calcGrid.LocalIndex;
					yield return instructionsList[i];
					i++;
				}

				//ldsfld uint16 Verse.AI.PathFinder_Original::statusOpenValue
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.UInt16 statusOpenValue")
					)
				{
					//ldloc.1   (local_statusOpenValue.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = local_statusOpenValue.LocalIndex;
					yield return instructionsList[i];
					i++;
				}

				//ldsfld uint16 Verse.AI.PathFinder_Original::statusClosedValue
				else if (
					instructionsList[i].opcode == OpCodes.Ldsfld && instructionsList[i].operand.ToString().Equals("System.UInt16 statusClosedValue")
					)
				{
					//ldloc.2(local_statusClosedValue.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = local_statusClosedValue.LocalIndex;
					yield return instructionsList[i];
					i++;
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
					instructionsList[i].operand = local_openList.LocalIndex;
					yield return instructionsList[i];
					i+=2;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == AccessTools.Method(fastPriorityQueueCostNodeType1, "get_Count")
					)
				{
					instructionsList[i].operand = AccessTools.Method(fastPriorityQueueCostNodeType2, "get_Count");
					yield return instructionsList[i];
					i++;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == AccessTools.Method(fastPriorityQueueCostNodeType1, "Pop")
				)
				{
					instructionsList[i].operand = AccessTools.Method(fastPriorityQueueCostNodeType2, "Pop");
					yield return instructionsList[i];
					i++;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == AccessTools.Method(fastPriorityQueueCostNodeType1, "Push")
				)
				{
					instructionsList[i].operand = AccessTools.Method(fastPriorityQueueCostNodeType2, "Push");
					yield return instructionsList[i];
					i++;
				}
				
				else if (
					instructionsList[i].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(costNodeType, "index")
				)
				{
					instructionsList[i].operand = AccessTools.Field(costNodeType2, "index");
					yield return instructionsList[i];
					i++;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(costNodeType, "cost")
)
				{
					instructionsList[i].operand = AccessTools.Field(costNodeType2, "cost");
					yield return instructionsList[i];
					i++;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Newobj &&
					(ConstructorInfo)instructionsList[i].operand == costNodeType.GetConstructor(new Type[] { typeof(int), typeof(int) })
				)
				{
					instructionsList[i].operand = costNodeType2.GetConstructor(new Type[] { typeof(int), typeof(int) });
					yield return instructionsList[i];
					i++;
				}
				else if (
					instructionsList[i].opcode == OpCodes.Newobj &&
					(ConstructorInfo)instructionsList[i].operand == fastPriorityQueueCostNodeType1.GetConstructor(new Type[] { icomparerCostNodeType1 })
				)
				{
					instructionsList[i].operand = fastPriorityQueueCostNodeType2.GetConstructor(new Type[] { icomparerCostNodeType2 });
					yield return instructionsList[i];
					i++;
				}
				else if (i+1 < instructionsList.Count &&
					instructionsList[i+1].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i+1].operand == AccessTools.Field(typeof(PathFinder), "regionCostCalculator")
				)
				{
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = local_regionCostCalculatorWrapper.LocalIndex;
					yield return instructionsList[i++];
					i++;
				}
				/*
				else if (i == instructionsList.Count - 1)
                {
					// openValues[managedThreadId] = value3;
					//IL_0c30: ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<int32, uint16> RimThreaded.PathFinder_Patch::openValues
					//IL_0c35: ldloc.0
					//IL_0c36: ldloc.s 4
					//IL_0c38: callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, uint16>::set_Item(!0, !1)
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "openValues"));
					yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc, local_statusOpenValue);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, ushort>), "set_Item"));

					// closedValues[managedThreadId] = value4;
					//IL_0c3d: ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<int32, uint16> RimThreaded.PathFinder_Patch::closedValues
					//IL_0c42: ldloc.0
					//IL_0c43: ldloc.s 5
					//IL_0c45: callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, uint16>::set_Item(!0, !1)
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PathFinder_Patch), "closedValues"));
					yield return new CodeInstruction(OpCodes.Ldloc, tID.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldloc, local_statusClosedValue);
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<int, ushort>), "set_Item"));
					yield return instructionsList[i];
					i++;
				}
				*/
				else
				{
                    yield return instructionsList[i];
					i++;
				}
			}

		}
	}
}
