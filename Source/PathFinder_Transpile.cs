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
    public class PathFinder_Transpile
    {
        public static IEnumerable<CodeInstruction> FindPath(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			/* Original PathFinder.FindPath

			TARGET0:
				// PathFinderNodeFast[] array = new PathFinderNodeFast[mapSizeX * mapSizeZ];
				// sequence point: (line 165, col 13) to (line 165, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
				IL_0000: ldarg.0
				IL_0001: ldfld int32 Verse.AI.PathFinder_Target::mapSizeX
				IL_0006: ldarg.0
				IL_0007: ldfld int32 Verse.AI.PathFinder_Target::mapSizeZ
				IL_000c: mul
				IL_000d: newarr Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_0012: stloc.0   (local_calcGrid.LocalIndex)
				// ushort num = 1;
				// sequence point: (line 166, col 13) to (line 166, col 41) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
				IL_0013: ldc.i4.1
				IL_0014: stloc.1   (local_statusOpenValue.LocalIndex)
				// ushort num2 = 2;
				// sequence point: (line 167, col 13) to (line 167, col 43) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
				IL_0015: ldc.i4.2
				IL_0016: stloc.2   (local_statusClosedValue.LocalIndex)


			ORIGINAL1:
				IL_031f: call instance void Verse.AI.PathFinder_Original::CalculateAndAddDisallowedCorners(valuetype ['Assembly-CSharp']Verse.TraverseParms, valuetype ['Assembly-CSharp']Verse.AI.PathEndMode, valuetype ['Assembly-CSharp']Verse.CellRect)
				// InitStatusesAndPushStartNode(ref curIndex, start);
			---START REMOVE---
				IL_0324: ldarg.0
				IL_0325: ldloca.s 3
				IL_0327: ldarg.1
				IL_0328: call instance void Verse.AI.PathFinder_Original::InitStatusesAndPushStartNode(int32&, valuetype ['Assembly-CSharp']Verse.IntVec3)
			---END REMOVE---


			TARGET1:
				// CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
				IL_0333: ldarg.0
				IL_0334: ldarg.3
				IL_0335: ldarg.s peMode
				IL_0337: ldloc.s 12
				IL_0339: call instance void Verse.AI.PathFinder_Target::CalculateAndAddDisallowedCorners(valuetype ['Assembly-CSharp']Verse.TraverseParms, valuetype ['Assembly-CSharp']Verse.AI.PathEndMode, valuetype ['Assembly-CSharp']Verse.CellRect)
			---START INSERT---
				// num = (ushort)(num + 2);
				IL_033e: ldloc.1   (local_statusOpenValue.LocalIndex)
				IL_033f: ldc.i4.2
				IL_0340: add
				IL_0341: conv.u2
				IL_0342: stloc.1   (local_statusOpenValue.LocalIndex)
				// num2 = (ushort)(num2 + 2);
				IL_0343: ldloc.2   (local_statusClosedValue.LocalIndex)
				IL_0344: ldc.i4.2
				IL_0345: add
				IL_0346: conv.u2
				IL_0347: stloc.2   (local_statusClosedValue.LocalIndex)
				// if (num2 >= 65435)
				IL_0348: ldloc.2   (local_statusClosedValue.LocalIndex)
				IL_0349: ldc.i4 65435
				IL_034e: blt.s IL_0374		(goto IL_0374)
				// for (int i = 0; i < array.Length; i++)
				IL_0350: ldc.i4.0
				// {
				IL_0351: stloc.s 33   (loopIndex.LocalIndex)
				// array[i].status = 0;
				IL_0353: br.s IL_0369		(goto IL_0369)
				// loop start (head: IL_0369)
					IL_0355: ldloc.0   (local_calcGrid.LocalIndex) (add label IL_0355)
					IL_0356: ldloc.s 33   (loopIndex.LocalIndex)
					IL_0358: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_035d: ldc.i4.0
					// {
					IL_035e: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
					// for (int i = 0; i < array.Length; i++)
					IL_0363: ldloc.s 33   (loopIndex.LocalIndex)
					IL_0365: ldc.i4.1
					IL_0366: add
					IL_0367: stloc.s 33   (loopIndex.LocalIndex)
					// for (int i = 0; i < array.Length; i++)
					IL_0369: ldloc.s 33   (loopIndex.LocalIndex) (add label IL_0369)
					IL_036b: ldloc.0   (local_calcGrid.LocalIndex)
					IL_036c: ldlen
					IL_036d: conv.i4
					IL_036e: blt.s IL_0355   (goto IL_0355)
				// end loop
				// num = 1;
				IL_0370: ldc.i4.1
				IL_0371: stloc.1   (local_statusOpenValue.LocalIndex)
				// num2 = 2;
				IL_0372: ldc.i4.2
				IL_0373: stloc.2   (local_statusClosedValue.LocalIndex)
				// curIndex = cellIndices.CellToIndex(start);
				IL_0374: ldarg.0	(add label1)
				IL_0375: ldfld class ['Assembly-CSharp']Verse.CellIndices Verse.AI.PathFinder_Target::cellIndices
				IL_037a: ldarg.1
				IL_037b: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(valuetype ['Assembly-CSharp']Verse.IntVec3)
				IL_0380: stloc.s 3
				// array[curIndex].knownCost = 0;
				IL_0382: ldloc.0   (local_calcGrid.LocalIndex)
				IL_0383: ldloc.s 3
				IL_0385: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_038a: ldc.i4.0
				IL_038b: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
				// array[curIndex].heuristicCost = 0;
				IL_0390: ldloc.0   (local_calcGrid.LocalIndex)
				IL_0391: ldloc.s 3
				IL_0393: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_0398: ldc.i4.0
				IL_0399: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::heuristicCost
				// array[curIndex].costNodeCost = 0;
				IL_039e: ldloc.0   (local_calcGrid.LocalIndex)
				IL_039f: ldloc.s 3
				IL_03a1: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_03a6: ldc.i4.0
				IL_03a7: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::costNodeCost
				// array[curIndex].parentIndex = curIndex;
				IL_03ac: ldloc.0   (local_calcGrid.LocalIndex)
				IL_03ad: ldloc.s 3
				IL_03af: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_03b4: ldloc.s 3
				IL_03b6: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
				// array[curIndex].status = num;
				IL_03bb: ldloc.0   (local_calcGrid.LocalIndex)
				IL_03bc: ldloc.s 3
				IL_03be: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
				IL_03c3: ldloc.1   (local_statusOpenValue.LocalIndex)
				IL_03c4: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
				// openList.Clear();
				IL_03c9: ldarg.0
				IL_03ca: ldfld class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
				IL_03cf: callvirt instance void class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Clear()
				// openList.Push(new CostNode(curIndex, 0));
				IL_03d4: ldarg.0
				IL_03d5: ldfld class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
				IL_03da: ldloc.s 3
				IL_03dc: ldc.i4.0
				IL_03dd: newobj instance void Verse.AI.PathFinder_Target/CostNode::.ctor(int32, int32)
				IL_03e2: callvirt instance void class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Push(!0)
			---END INSERT---
				// loop start (head: IL_03e7)
					// if (openList.Count <= 0)
					IL_03e7: ldarg.0
					IL_03e8: ldfld class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
					IL_03ed: callvirt instance int32 class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::get_Count()
					IL_03f2: ldc.i4.0
					IL_03f3: bgt IL_04b4




			ORIGINAL2:
					// if (curIndex == num)
					IL_047a: ldloc.3
					IL_047b: ldloc.s 4
					IL_047d: bne.un.s IL_04ac
			---START REMOVE---
					// return FinalizedPath(curIndex, flag8);
					IL_047f: ldarg.0
					IL_0480: ldloc.3
					IL_0481: ldloc.s 20
					IL_0483: call instance class ['Assembly-CSharp']Verse.AI.PawnPath Verse.AI.PathFinder_Original::FinalizedPath(int32, bool)
			---END REMOVE---
					IL_0488: ret
					// else if (cellRect.Contains(c) && !disallowedCornerIndices.Contains(curIndex))
					IL_0489: ldloca.s 9
					IL_048b: ldloc.s 30
					IL_048d: call instance bool ['Assembly-CSharp']Verse.CellRect::Contains(valuetype ['Assembly-CSharp']Verse.IntVec3)
					IL_0492: brfalse.s IL_04ac




			TARGET2:
					IL_052c: ldloc.s 3
					IL_052e: ldloc.s 7
					IL_0530: bne.un IL_0617
			---START INSERT---
					// PawnPath pawnPath = new PawnPath();
					IL_0535: newobj instance void ['Assembly-CSharp']Verse.AI.PawnPath::.ctor()
					// {
					IL_0545: stloc.s 39   (emptyPawnPath.LocalIndex)
					// int num13 = curIndex;
					IL_0547: ldloc.s 3
					IL_0549: stloc.s 40   (num1.LocalIndex)
					// loop start (head: IL_054b)
						// int parentIndex = array[num13].parentIndex;
						IL_054b: ldloc.0   (local_calcGrid.LocalIndex) (add label IL_054b)
						IL_054c: ldloc.s 40   (num1.LocalIndex)
						IL_054e: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
						IL_0553: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
						IL_0558: stloc.s 41   (parentIndex.LocalIndex)
						// emptyPawnPath.AddNode(map.cellIndices.IndexToCell(num13));
						IL_055a: ldloc.s 39   (emptyPawnPath.LocalIndex)
						IL_055c: ldarg.0
						IL_055d: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
						IL_0562: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
						IL_0567: ldloc.s 40   (num1.LocalIndex)
						IL_0569: callvirt instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellIndices::IndexToCell(int32)
						IL_056e: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::AddNode(valuetype ['Assembly-CSharp']Verse.IntVec3)
						// if (num13 == parentIndex)
						IL_0573: ldloc.s 40   (num1.LocalIndex)
						IL_0575: ldloc.s 41   (parentIndex.LocalIndex)
						IL_0577: beq.s IL_057f   (goto IL_057f)
						// num13 = parentIndex;
						IL_0579: ldloc.s 41   (parentIndex.LocalIndex)
						IL_057b: stloc.s 40   (num1.LocalIndex)
						// while (true)
						IL_057d: br.s IL_054b   (goto IL_054b)
					// end loop
					// emptyPawnPath.SetupFound(array[curIndex].knownCost, flag8);
					IL_057f: ldloc.s 39   (emptyPawnPath.LocalIndex) (add label IL_057f)
					IL_0581: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0582: ldloc.s 3
					IL_0584: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0589: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
					IL_058e: conv.r4
					IL_058f: ldloc.s 23
					IL_0591: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::SetupFound(float32, bool)
					// return emptyPawnPath;
					IL_0596: ldloc.s 39   (emptyPawnPath.LocalIndex)
			---END INSERT---
					IL_0598: ret
					// else if (cellRect.Contains(c) && !disallowedCornerIndices.Contains(curIndex))
					IL_0599: ldloca.s 12
					IL_059b: ldloc.s 34
					IL_059d: call instance bool ['Assembly-CSharp']Verse.CellRect::Contains(valuetype ['Assembly-CSharp']Verse.IntVec3)
					IL_05a2: brfalse.s IL_0617


			ORIGINAL3:
					IL_0494: ldarg.0
					IL_0495: ldfld class [mscorlib]System.Collections.Generic.List`1<int32> Verse.AI.PathFinder_Original::disallowedCornerIndices
					IL_049a: ldloc.3
					IL_049b: callvirt instance bool class [mscorlib]System.Collections.Generic.List`1<int32>::Contains(!0)
					IL_04a0: brtrue.s IL_04ac
			---START REMOVE---
					// return FinalizedPath(curIndex, flag8);
					IL_04a2: ldarg.0
					IL_04a3: ldloc.3
					IL_04a4: ldloc.s 20
					IL_04a6: call instance class ['Assembly-CSharp']Verse.AI.PawnPath Verse.AI.PathFinder_Original::FinalizedPath(int32, bool)
			---END REMOVE---
					IL_04ab: ret


			TARGET3:
					// if (curIndex == num4)
					IL_052c: ldloc.s 3
					IL_052e: ldloc.s 7
					IL_0530: bne.un IL_0617
			---START INSERT---
					// PawnPath emptyPawnPath2 = map.pawnPathPool.GetEmptyPawnPath();
					IL_0535: newobj instance void ['Assembly-CSharp']Verse.AI.PawnPath::.ctor()
					// {
					IL_05c3: stloc.s 42   (emptyPawnPath.LocalIndex)
					// int num14 = curIndex;
					IL_05c5: ldloc.s 3
					IL_05c7: stloc.s 43   (num1.LocalIndex)
					// loop start (head: IL_05c9)
						// int parentIndex2 = array[num14].parentIndex;
						IL_05c9: ldloc.0   (local_calcGrid.LocalIndex) (add label IL_05c9)
						IL_05ca: ldloc.s 43   (num1.LocalIndex)
						IL_05cc: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
						IL_05d1: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
						IL_05d6: stloc.s 44   (parentIndex.LocalIndex)
						// emptyPawnPath2.AddNode(map.cellIndices.IndexToCell(num14));
						IL_05d8: ldloc.s 42   (emptyPawnPath.LocalIndex)
						IL_05da: ldarg.0
						IL_05db: ldfld class ['Assembly-CSharp']Verse.Map Verse.AI.PathFinder_Target::map
						IL_05e0: ldfld class ['Assembly-CSharp']Verse.CellIndices ['Assembly-CSharp']Verse.Map::cellIndices
						IL_05e5: ldloc.s 43   (num1.LocalIndex)
						IL_05e7: callvirt instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellIndices::IndexToCell(int32)
						IL_05ec: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::AddNode(valuetype ['Assembly-CSharp']Verse.IntVec3)
						// if (num14 == parentIndex2)
						IL_05f1: ldloc.s 43   (num1.LocalIndex)
						IL_05f3: ldloc.s 44   (parentIndex.LocalIndex)
						IL_05f5: beq.s IL_05fd   (goto IL_05fd)
						// num14 = parentIndex2;
						IL_05f7: ldloc.s 44   (parentIndex.LocalIndex)
						IL_05f9: stloc.s 43   (num1.LocalIndex)
						// while (true)
						IL_05fb: br.s IL_05c9   (goto IL_05c9)
					// end loop
					// emptyPawnPath2.SetupFound(array[curIndex].knownCost, flag8);
					IL_05fd: ldloc.s 42   (emptyPawnPath.LocalIndex) (add label IL_05fd)
					IL_05ff: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0600: ldloc.s 3
					IL_0602: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0607: ldfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
					IL_060c: conv.r4
					IL_060d: ldloc.s 23
					IL_060f: callvirt instance void ['Assembly-CSharp']Verse.AI.PawnPath::SetupFound(float32, bool)
					// return emptyPawnPath2;
					IL_0614: ldloc.s 42   (emptyPawnPath.LocalIndex)
			---END INSERT---
					IL_0598: ret
			ORIGINAL4:
					IL_0aa2: callvirt instance void ['Assembly-CSharp']Verse.AI.RegionCostCalculatorWrapper::Init(valuetype ['Assembly-CSharp']Verse.CellRect, valuetype ['Assembly-CSharp']Verse.TraverseParms, int32, int32, class ['Assembly-CSharp']Verse.ByteGrid, class ['Assembly-CSharp']Verse.Area, bool, class [mscorlib]System.Collections.Generic.List`1<int32>)
			---START REMOVE---
					// InitStatusesAndPushStartNode(ref curIndex, start);
					IL_0aa7: ldarg.0
					IL_0aa8: ldloca.s 3
					IL_0aaa: ldarg.1
					IL_0aab: call instance void Verse.AI.PathFinder_Original::InitStatusesAndPushStartNode(int32&, valuetype ['Assembly-CSharp']Verse.IntVec3)
			---END REMOVE---
					// curIndex = 0;
					IL_0ab0: ldc.i4.0
					IL_0ab1: stloc.s 15
					// num2 = 0;
					IL_0ab3: ldc.i4.0
					IL_0ab4: stloc.s 14
			TARGET4:
					// flag8 = true;
					IL_0b9f: ldloc.s 23
					IL_0ba1: brtrue IL_03e7
					IL_0ba6: ldc.i4.1
					IL_0ba7: stloc.s 23
					// regionCostCalculator.Init(cellRect, traverseParms, num11, num12, byteGrid, allowedArea, flag9, disallowedCornerIndices);
					IL_0ba9: ldarg.0
					IL_0baa: ldfld class ['Assembly-CSharp']Verse.AI.RegionCostCalculatorWrapper Verse.AI.PathFinder_Target::regionCostCalculator
					IL_0baf: ldloc.s 12
					IL_0bb1: ldarg.3
					IL_0bb2: ldloc.s 29
					IL_0bb4: ldloc.s 30
					IL_0bb6: ldloc.s 8
					IL_0bb8: ldloc.s 19
					IL_0bba: ldloc.s 24
					IL_0bbc: ldarg.0
					IL_0bbd: ldfld class [mscorlib]System.Collections.Generic.List`1<int32> Verse.AI.PathFinder_Target::disallowedCornerIndices
					IL_0bc2: callvirt instance void ['Assembly-CSharp']Verse.AI.RegionCostCalculatorWrapper::Init(valuetype ['Assembly-CSharp']Verse.CellRect, valuetype ['Assembly-CSharp']Verse.TraverseParms, int32, int32, class ['Assembly-CSharp']Verse.ByteGrid, class ['Assembly-CSharp']Verse.Area, bool, class [mscorlib]System.Collections.Generic.List`1<int32>)
			---START INSERT---
					// num = (ushort)(num + 2);
					IL_0bc7: ldloc.1   (local_statusOpenValue.LocalIndex)
					IL_0bc8: ldc.i4.2
					IL_0bc9: add
					IL_0bca: conv.u2
					IL_0bcb: stloc.1   (local_statusOpenValue.LocalIndex)
					// num2 = (ushort)(num2 + 2);
					IL_0bcc: ldloc.2   (local_statusClosedValue.LocalIndex)
					IL_0bcd: ldc.i4.2
					IL_0bce: add
					IL_0bcf: conv.u2
					IL_0bd0: stloc.2   (local_statusClosedValue.LocalIndex)
					// if (num2 >= 65435)
					IL_0bd1: ldloc.2   (local_statusClosedValue.LocalIndex)
					IL_0bd2: ldc.i4 65435
					IL_0bd7: blt.s IL_0bfd   (goto IL_0bfd)
					// for (int l = 0; l < array.Length; l++)
					IL_0bd9: ldc.i4.0
					// {
					IL_0bda: stloc.s 66   (loopIndex.LocalIndex)
					// array[l].status = 0;
					IL_0bdc: br.s IL_0bf2   (goto IL_0bf2)
					// loop start (head: IL_0bf2)
						IL_0bde: ldloc.0   (local_calcGrid.LocalIndex) (add label IL_0bde)
						IL_0bdf: ldloc.s 66   (loopIndex.LocalIndex)
						IL_0be1: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
						IL_0be6: ldc.i4.0
						// {
						IL_0be7: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
						// for (int l = 0; l < array.Length; l++)
						IL_0bec: ldloc.s 66   (loopIndex.LocalIndex)
						IL_0bee: ldc.i4.1
						IL_0bef: add
						IL_0bf0: stloc.s 66   (loopIndex.LocalIndex)
						// for (int l = 0; l < array.Length; l++)
						IL_0bf2: ldloc.s 66   (loopIndex.LocalIndex) (add label IL_0bf2)
						IL_0bf4: ldloc.0   (local_calcGrid.LocalIndex)
						IL_0bf5: ldlen
						IL_0bf6: conv.i4
						IL_0bf7: blt.s IL_0bde   (goto IL_0bde)
					// end loop
					// num = 1;
					IL_0bf9: ldc.i4.1
					IL_0bfa: stloc.1   (local_statusOpenValue.LocalIndex)
					// num2 = 2;
					IL_0bfb: ldc.i4.2
					IL_0bfc: stloc.2   (local_statusClosedValue.LocalIndex)
					// curIndex = cellIndices.CellToIndex(start);
					IL_0bfd: ldarg.0   (add label IL_0bfd)
					IL_0bfe: ldfld class ['Assembly-CSharp']Verse.CellIndices Verse.AI.PathFinder_Target::cellIndices
					IL_0c03: ldarg.1
					IL_0c04: callvirt instance int32 ['Assembly-CSharp']Verse.CellIndices::CellToIndex(valuetype ['Assembly-CSharp']Verse.IntVec3)
					IL_0c09: stloc.s 3
					// array[curIndex].knownCost = 0;
					IL_0c0b: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0c0c: ldloc.s 3
					IL_0c0e: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0c13: ldc.i4.0
					IL_0c14: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::knownCost
					// array[curIndex].heuristicCost = 0;
					IL_0c19: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0c1a: ldloc.s 3
					IL_0c1c: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0c21: ldc.i4.0
					IL_0c22: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::heuristicCost
					// array[curIndex].costNodeCost = 0;
					IL_0c27: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0c28: ldloc.s 3
					IL_0c2a: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0c2f: ldc.i4.0
					IL_0c30: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::costNodeCost
					// array[curIndex].parentIndex = curIndex;
					IL_0c35: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0c36: ldloc.s 3
					IL_0c38: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0c3d: ldloc.s 3
					IL_0c3f: stfld int32 Verse.AI.PathFinder_Target/PathFinderNodeFast::parentIndex
					// array[curIndex].status = num;
					IL_0c44: ldloc.0   (local_calcGrid.LocalIndex)
					IL_0c45: ldloc.s 3
					IL_0c47: ldelema Verse.AI.PathFinder_Target/PathFinderNodeFast
					IL_0c4c: ldloc.1   (local_statusOpenValue.LocalIndex)
					IL_0c4d: stfld uint16 Verse.AI.PathFinder_Target/PathFinderNodeFast::status
					// openList.Clear();
					IL_0c52: ldarg.0
					IL_0c53: ldfld class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
					IL_0c58: callvirt instance void class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Clear()
					// openList.Push(new CostNode(curIndex, 0));
					IL_0c5d: ldarg.0
					IL_0c5e: ldfld class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode> Verse.AI.PathFinder_Target::openList
					IL_0c63: ldloc.s 3
					IL_0c65: ldc.i4.0
					IL_0c66: newobj instance void Verse.AI.PathFinder_Target/CostNode::.ctor(int32, int32)
					IL_0c6b: callvirt instance void class ['Assembly-CSharp']Verse.FastPriorityQueue`1<valuetype Verse.AI.PathFinder_Target/CostNode>::Push(!0)
			---END INSERT---
					// num6 = 0;
					IL_0c70: ldc.i4.0
					IL_0c71: stloc.s 18
					// num5 = 0;
					IL_0c73: ldc.i4.0
					IL_0c74: stloc.s 17



			ORIGINAL 5:
				ldsfld valuetype Verse.AI.PathFinder_Original/PathFinderNodeFast[] Verse.AI.PathFinder_Original::calcGrid

			TARGET 5:
				ldloc.0   (local_calcGrid.LocalIndex)


			ORIGINAL 6:
				ldsfld uint16 Verse.AI.PathFinder_Original::statusOpenValue

			TARGET 6:
				ldloc.1   (local_statusOpenValue.LocalIndex)


			ORIGINAL 7:
				ldsfld uint16 Verse.AI.PathFinder_Original::statusClosedValue

			TARGET 7:
				ldloc.2   (local_statusClosedValue.LocalIndex)
			*/
			Type nodeFastType = AccessTools.TypeByName("Verse.AI.PathFinder+PathFinderNodeFast");
			Type nodeFastTypeArray = nodeFastType.MakeArrayType();
			LocalBuilder local_calcGrid = iLGenerator.DeclareLocal(nodeFastTypeArray);
			LocalBuilder local_statusOpenValue = iLGenerator.DeclareLocal(typeof(int));
			LocalBuilder local_statusClosedValue = iLGenerator.DeclareLocal(typeof(int));
			Type costNodeType = AccessTools.TypeByName("Verse.AI.PathFinder+CostNode");
			Type fastPriorityQueueCostNodeType = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType);
			LocalBuilder local_openList = iLGenerator.DeclareLocal(fastPriorityQueueCostNodeType);
			
			List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
			/*
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Verse.AI.PathFinder_Target::mapSizeX
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Verse.AI.PathFinder_Target::mapSizeZ
			IL_000c: mul
			IL_000d: newarr Verse.AI.PathFinder_Target / PathFinderNodeFast
			IL_0012: stloc.0(local_calcGrid.LocalIndex)
			// ushort num = 1;
			// sequence point: (line 166, col 13) to (line 166, col 41) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
			IL_0013: ldc.i4.1
			IL_0014: stloc.1(local_statusOpenValue.LocalIndex)
			// ushort num2 = 2;
			// sequence point: (line 167, col 13) to (line 167, col 43) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\PathFinder_Target.cs
			IL_0015: ldc.i4.2
			IL_0016: stloc.2(local_statusClosedValue.LocalIndex)
			*/
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "mapSizeX"));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "mapSizeZ"));
			yield return new CodeInstruction(OpCodes.Mul);
			yield return new CodeInstruction(OpCodes.Newarr, nodeFastType);
			yield return new CodeInstruction(OpCodes.Stloc, local_calcGrid.LocalIndex);

			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Stloc, local_statusOpenValue.LocalIndex);

			yield return new CodeInstruction(OpCodes.Ldc_I4_2);
			yield return new CodeInstruction(OpCodes.Stloc, local_statusClosedValue.LocalIndex);

			Type costNodeComparer = AccessTools.TypeByName("Verse.AI.PathFinder+CostNodeComparer");
			yield return new CodeInstruction(OpCodes.Newobj, costNodeComparer.GetConstructor(Type.EmptyTypes));
			Type icomparerCostNodeType = typeof(IComparer<>).MakeGenericType(costNodeType);
			yield return new CodeInstruction(OpCodes.Newobj, fastPriorityQueueCostNodeType.GetConstructor(new Type[] { icomparerCostNodeType }));
			yield return new CodeInstruction(OpCodes.Stloc, local_openList.LocalIndex);

			while (i < instructionsList.Count)
			{
				// InitStatusesAndPushStartNode(ref curIndex, start);
				//IL_0363: ldarg.0
				//IL_0364: ldloca.s 3
				//IL_0366: ldarg.1
				//IL_0367: call instance void Verse.AI.PathFinder::InitStatusesAndPushStartNode(int32 &, valuetype Verse.IntVec3)
				if (					
					i+3 < instructionsList.Count && instructionsList[i+3].opcode == OpCodes.Call && instructionsList[i+3].operand.ToString().Equals("Void InitStatusesAndPushStartNode(Int32 ByRef, Verse.IntVec3)")
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
					yield return instructionsList[i+1];
					yield return instructionsList[i+2];
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
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(fastPriorityQueueCostNodeType, "Clear"));

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
					instructionsList[i].opcode = OpCodes.Nop;
					yield return instructionsList[i];
					yield return instructionsList[i + 1];
					yield return instructionsList[i + 2];
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PathFinder), "cellIndices"));
					yield return new CodeInstruction(OpCodes.Ldloc, local_calcGrid.LocalIndex);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_Patch), "FinalizedPath2"));
					i += 3;
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
