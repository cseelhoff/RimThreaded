using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using System.Reflection.Emit;
using System.Reflection;
using static Verse.AI.PathFinder;
using System.Linq;

namespace RimThreaded
{

    public class PathFinder_Patch
    {
        [ThreadStatic] public static List<int> disallowedCornerIndices;
        [ThreadStatic] public static PathFinder.PathFinderNodeFast[] calcGrid;
        [ThreadStatic] public static FastPriorityQueue<PathFinder.CostNode> openList;
        [ThreadStatic] public static ushort statusOpenValue;
        [ThreadStatic] public static ushort statusClosedValue;
        //[ThreadStatic] public static Dictionary<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorDict;
        
        public static void InitializeThreadStatics()
        {
            openList = new FastPriorityQueue<PathFinder.CostNode>(new PathFinder.CostNodeComparer());
            statusOpenValue = 1;
            statusClosedValue = 2;
            disallowedCornerIndices = new List<int>(4);
            //regionCostCalculatorDict = new Dictionary<PathFinder, RegionCostCalculatorWrapper>();
        }

        public static bool FindPath(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            if (DebugSettings.pathThroughWalls)
            {
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            }

            Pawn pawn = traverseParms.pawn;
            if (pawn != null && pawn.Map != __instance.map)
            {
                Log.Error(string.Concat("Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=", pawn, " pawn.Map=", pawn.Map, " map=", __instance.map));
                __result = PawnPath.NotFound;
                return false;
            }

            if (!start.IsValid)
            {
                Log.Error(string.Concat("Tried to FindPath with invalid start ", start, ", pawn= ", pawn));
                __result = PawnPath.NotFound;
                return false;
            }

            if (!dest.IsValid)
            {
                Log.Error(string.Concat("Tried to FindPath with invalid dest ", dest, ", pawn= ", pawn));
                __result = PawnPath.NotFound;
                return false;
            }

            if (traverseParms.mode == TraverseMode.ByPawn)
            {
                if (!pawn.CanReach(dest, peMode, Danger.Deadly, traverseParms.canBash, traverseParms.mode))
                {
                    __result = PawnPath.NotFound;
                    return false;
                }
            }
            else if (!__instance.map.reachability.CanReach(start, dest, peMode, traverseParms))
            {
                __result = PawnPath.NotFound;
                return false;
            }

            __instance.PfProfilerBeginSample(string.Concat("FindPath for ", pawn, " from ", start, " to ", dest, dest.HasThing ? (" at " + dest.Cell) : ""));
            __instance.cellIndices = __instance.map.cellIndices;
            __instance.pathGrid = __instance.map.pathGrid;
            __instance.edificeGrid = __instance.map.edificeGrid.InnerArray;
            __instance.blueprintGrid = __instance.map.blueprintGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int curIndex = __instance.cellIndices.CellToIndex(start);
            int num = __instance.cellIndices.CellToIndex(dest.Cell);
            ByteGrid byteGrid = pawn?.GetAvoidGrid();
            bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag3 = !flag;
            CellRect destinationRect = __instance.CalculateDestinationRect(dest, peMode);
            bool flag4 = destinationRect.Width == 1 && destinationRect.Height == 1;
            int[] array = __instance.map.pathGrid.pathGrid;
            TerrainDef[] topGrid = __instance.map.terrainGrid.topGrid;
            EdificeGrid edificeGrid = __instance.map.edificeGrid;
            int num2 = 0;
            int num3 = 0;
            Area allowedArea = __instance.GetAllowedArea(pawn);
            bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = !flag && start.GetRegion(__instance.map) != null && flag2;
            bool flag7 = !flag || !flag3;
            bool flag8 = false;
            bool flag9 = pawn?.Drafted ?? false;
            int num4 = (pawn?.IsColonist ?? false) ? 100000 : 2000;
            int num5 = 0;
            int num6 = 0;
            float num7 = __instance.DetermineHeuristicStrength(pawn, start, dest);
            int num8;
            int num9;
            if (pawn != null)
            {
                num8 = pawn.TicksPerMoveCardinal;
                num9 = pawn.TicksPerMoveDiagonal;
            }
            else
            {
                num8 = 13;
                num9 = 18;
            }
            __instance.CalculateAndAddDisallowedCorners(traverseParms, peMode, destinationRect);
            __instance.InitStatusesAndPushStartNode(ref curIndex, start);
            while (true)
            {
                __instance.PfProfilerBeginSample("Open cell");
                if (openList.Count <= 0)
                {
                    string text = (pawn != null && pawn.CurJob != null) ? pawn.CurJob.ToString() : "null";
                    string text2 = (pawn != null && pawn.Faction != null) ? pawn.Faction.ToString() : "null";
                    Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " ran out of cells to process.\nJob:", text, "\nFaction: ", text2));
                    __instance.DebugDrawRichData();
                    __instance.PfProfilerEndSample();
                    __instance.PfProfilerEndSample();
                    __result = PawnPath.NotFound;
                    return false;
                }

                num5 += openList.Count;
                num6++;
                PathFinder.CostNode costNode = openList.Pop();
                curIndex = costNode.index;
                if (costNode.cost != calcGrid[curIndex].costNodeCost)
                {
                    __instance.PfProfilerEndSample();
                    continue;
                }

                if (calcGrid[curIndex].status == statusClosedValue)
                {
                    __instance.PfProfilerEndSample();
                    continue;
                }

                IntVec3 c = __instance.cellIndices.IndexToCell(curIndex);
                int x2 = c.x;
                int z2 = c.z;
                if (flag4)
                {
                    if (curIndex == num)
                    {
                        __instance.PfProfilerEndSample();
                        PawnPath result = __instance.FinalizedPath(curIndex, flag8);
                        __instance.PfProfilerEndSample();
                        __result = result;
                        return false;
                    }
                }
                else if (destinationRect.Contains(c) && !disallowedCornerIndices.Contains(curIndex))
                {
                    __instance.PfProfilerEndSample();
                    PawnPath result2 = __instance.FinalizedPath(curIndex, flag8);
                    __instance.PfProfilerEndSample();
                    __result = result2;
                    return false;
                }

                if (num2 > 160000)
                {
                    break;
                }

                __instance.PfProfilerEndSample();
                __instance.PfProfilerBeginSample("Neighbor consideration");
                for (int i = 0; i < 8; i++)
                {
                    uint num10 = (uint)(x2 + PathFinder.Directions[i]);
                    uint num11 = (uint)(z2 + PathFinder.Directions[i + 8]);
                    if (num10 >= __instance.mapSizeX || num11 >= __instance.mapSizeZ)
                    {
                        continue;
                    }

                    int num12 = (int)num10;
                    int num13 = (int)num11;
                    int num14 = __instance.cellIndices.CellToIndex(num12, num13);
                    if (calcGrid[num14].status == statusClosedValue && !flag8)
                    {
                        continue;
                    }

                    int num15 = 0;
                    bool flag10 = false;
                    if (!flag2 && new IntVec3(num12, 0, num13).GetTerrain(__instance.map).HasTag("Water"))
                    {
                        continue;
                    }

                    if (!__instance.pathGrid.WalkableFast(num14))
                    {
                        if (!flag)
                        {
                            continue;
                        }

                        flag10 = true;
                        num15 += 70;
                        Building building = edificeGrid[num14];
                        if (building == null || !PathFinder.IsDestroyable(building))
                        {
                            continue;
                        }

                        num15 += (int)(building.HitPoints * 0.2f);
                    }

                    switch (i)
                    {
                        case 4:
                            if (PathFinder.BlocksDiagonalMovement(curIndex - __instance.mapSizeX, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex + 1, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 5:
                            if (PathFinder.BlocksDiagonalMovement(curIndex + __instance.mapSizeX, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex + 1, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 6:
                            if (PathFinder.BlocksDiagonalMovement(curIndex + __instance.mapSizeX, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex - 1, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 7:
                            if (PathFinder.BlocksDiagonalMovement(curIndex - __instance.mapSizeX, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex - 1, __instance.map))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                    }

                    int num16 = (i > 3) ? num9 : num8;
                    num16 += num15;
                    if (!flag10)
                    {
                        num16 += array[num14];
                        num16 = ((!flag9) ? (num16 + topGrid[num14].extraNonDraftedPerceivedPathCost) : (num16 + topGrid[num14].extraDraftedPerceivedPathCost));
                    }

                    if (byteGrid != null)
                    {
                        num16 += byteGrid[num14] * 8;
                    }

                    if (allowedArea != null && !allowedArea[num14])
                    {
                        num16 += 600;
                    }

                    if (flag5 && PawnUtility.AnyPawnBlockingPathAt(new IntVec3(num12, 0, num13), pawn, actAsIfHadCollideWithPawnsJob: false, collideOnlyWithStandingPawns: false, forPathFinder: true))
                    {
                        num16 += 175;
                    }

                    Building building2 = __instance.edificeGrid[num14];
                    if (building2 != null)
                    {
                        __instance.PfProfilerBeginSample("Edifices");
                        int buildingCost = PathFinder.GetBuildingCost(building2, traverseParms, pawn);
                        if (buildingCost == int.MaxValue)
                        {
                            __instance.PfProfilerEndSample();
                            continue;
                        }

                        num16 += buildingCost;
                        __instance.PfProfilerEndSample();
                    }

                    List<Blueprint> list = __instance.blueprintGrid[num14];
                    if (list != null)
                    {
                        __instance.PfProfilerBeginSample("Blueprints");
                        int num17 = 0;
                        for (int j = 0; j < list.Count; j++)
                        {
                            num17 = Mathf.Max(num17, PathFinder.GetBlueprintCost(list[j], pawn));
                        }

                        if (num17 == int.MaxValue)
                        {
                            __instance.PfProfilerEndSample();
                            continue;
                        }

                        num16 += num17;
                        __instance.PfProfilerEndSample();
                    }

                    int num18 = num16 + calcGrid[curIndex].knownCost;
                    ushort status = calcGrid[num14].status;
                    if (status == statusClosedValue || status == statusOpenValue)
                    {
                        int num19 = 0;
                        if (status == statusClosedValue)
                        {
                            num19 = num8;
                        }

                        if (calcGrid[num14].knownCost <= num18 + num19)
                        {
                            continue;
                        }
                    }

                    if (flag8)
                    {
                        calcGrid[num14].heuristicCost = Mathf.RoundToInt(__instance.regionCostCalculator.GetPathCostFromDestToRegion(num14) * PathFinder.RegionHeuristicWeightByNodesOpened.Evaluate(num3));
                        if (calcGrid[num14].heuristicCost < 0)
                        {
                            Log.ErrorOnce(string.Concat("Heuristic cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0xB8DC389);
                            calcGrid[num14].heuristicCost = 0;
                        }
                    }
                    else if (status != statusClosedValue && status != statusOpenValue)
                    {
                        int dx = Math.Abs(num12 - x);
                        int dz = Math.Abs(num13 - z);
                        int num20 = GenMath.OctileDistance(dx, dz, num8, num9);
                        calcGrid[num14].heuristicCost = Mathf.RoundToInt(num20 * num7);
                    }

                    int num21 = num18 + calcGrid[num14].heuristicCost;
                    if (num21 < 0)
                    {
                        Log.ErrorOnce(string.Concat("Node cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0x53CB9DE);
                        num21 = 0;
                    }

                    calcGrid[num14].parentIndex = curIndex;
                    calcGrid[num14].knownCost = num18;
                    calcGrid[num14].status = statusOpenValue;
                    calcGrid[num14].costNodeCost = num21;
                    num3++;
                    openList.Push(new PathFinder.CostNode(num14, num21));
                }

                __instance.PfProfilerEndSample();
                num2++;
                calcGrid[curIndex].status = statusClosedValue;
                if (num3 >= num4 && flag6 && !flag8)
                {
                    flag8 = true;
                    __instance.regionCostCalculator.Init(destinationRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, disallowedCornerIndices);
                    __instance.InitStatusesAndPushStartNode(ref curIndex, start);
                    openList.Clear();
                    openList.Push(new PathFinder.CostNode(curIndex, 0));
                    num3 = 0;
                    num2 = 0;
                }
            }

            Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " hit search limit of ", 160000, " cells."));
            __instance.DebugDrawRichData();
            __instance.PfProfilerEndSample();
            __instance.PfProfilerEndSample();
            __result = PawnPath.NotFound;
            return false;
        }


        static readonly Type costNodeType = TypeByName("Verse.AI.PathFinder+CostNode");
        static readonly Type icomparerCostNodeType1 = typeof(IComparer<>).MakeGenericType(costNodeType);
        static readonly Type fastPriorityQueueCostNodeType1 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType);

        static readonly Type costNodeType2 = typeof(CostNode);
        static readonly Type icomparerCostNodeType2 = typeof(IComparer<>).MakeGenericType(costNodeType2);
        static readonly Type fastPriorityQueueCostNodeType2 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType2);

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(PathFinder);
            Type patched = typeof(PathFinder_Patch);
            //RimThreadedHarmony.Transpile(original, patched, "FindPath", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) });
        }

        public static IEnumerable<CodeInstruction> FindPath(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            //calcGrid
            //statusOpenValue
            //statusClosedValue
            //disallowedCornerIndices
            //openList
            //InitStatusesAndPushStartNode2
            //FinalizedPath2
            //CalculateAndAddDisallowedCorners2
            //FastPriorityQueueCostNode.get_Count
            //FastPriorityQueueCostNode.pop
            //FastPriorityQueueCostNode.push
            //FastPriorityQueueCostNode.Clear
            //CostNode.index
            //CostNode.cost
            //CostNode(int, int)
            //FastPriorityQueueCostNode(icomparerCostNode)

            int[] matchesFound = new int[14];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "calcGrid")
                    )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "calcGrid");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;

                if (
                   instructionsList[i].opcode == OpCodes.Ldsfld &&
                   (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "statusOpenValue")
                   )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "statusOpenValue");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Ldsfld &&
                   (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "statusClosedValue")
                   )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "statusClosedValue");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                  i + 1 < instructionsList.Count &&
                  instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                  (FieldInfo)instructionsList[i + 1].operand == Field(typeof(PathFinder), "disallowedCornerIndices")
                  )
                {
                    instructionsList[i].opcode = OpCodes.Ldsfld;
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "disallowedCornerIndices");
                    yield return instructionsList[i++];
                    i++;
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "regionCostCalculator")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "get_regionCostCalculator");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                if (
                    i + 1 < instructionsList.Count &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i + 1].operand == Field(typeof(PathFinder), "openList")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldsfld;
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "openList");
                    yield return instructionsList[i++];
                    i++;
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "InitStatusesAndPushStartNode")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "InitStatusesAndPushStartNode2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Call &&
                   (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "FinalizedPath")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "FinalizedPath2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Call &&
                   (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "CalculateAndAddDisallowedCorners")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "CalculateAndAddDisallowedCorners2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "get_Count")
                    )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "get_Count");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Pop")
                )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Pop");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Push")
                )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Push");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                /*
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Clear")
				)
				{
					instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Clear");
					yield return instructionsList[i++];
					matchesFound[matchIndex]++;
					continue;
				}
				*/
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(costNodeType, "index")
                )
                {
                    instructionsList[i].operand = Field(costNodeType2, "index");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(costNodeType, "cost")
)
                {
                    instructionsList[i].operand = Field(costNodeType2, "cost");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj &&
                    (ConstructorInfo)instructionsList[i].operand == costNodeType.GetConstructor(new Type[] { typeof(int), typeof(int) })
                )
                {
                    instructionsList[i].operand = costNodeType2.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj &&
                    (ConstructorInfo)instructionsList[i].operand == fastPriorityQueueCostNodeType1.GetConstructor(new Type[] { icomparerCostNodeType1 })
                )
                {
                    instructionsList[i].operand = fastPriorityQueueCostNodeType2.GetConstructor(new Type[] { icomparerCostNodeType2 });
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }

                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
        }

    }
}
