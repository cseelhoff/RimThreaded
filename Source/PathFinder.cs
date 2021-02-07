using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class PathFinder_Patch
    {
        [ThreadStatic]
        public static List<int> disallowedCornerIndices;
        [ThreadStatic]
        static PathFinderNodeFast2[] calcGrid;
        [ThreadStatic]
        public static FastPriorityQueue<CostNode2> openList;
        [ThreadStatic]
        public static ushort statusOpenValue;
        [ThreadStatic]
        public static ushort statusClosedValue;
        [ThreadStatic]
        public static Dictionary<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorDict;

        static readonly FieldRef<PathFinder, Map> mapField =
            FieldRefAccess<PathFinder, Map>("map");
        static readonly FieldRef<PathFinder, int> mapSizeXField =
            FieldRefAccess<PathFinder, int>("mapSizeX");
        static readonly FieldRef<PathFinder, int> mapSizeZField =
            FieldRefAccess<PathFinder, int>("mapSizeZ");
        static readonly FieldRef<PathFinder, CellIndices> cellIndicesField =
            FieldRefAccess<PathFinder, CellIndices>("cellIndices");
        static readonly FieldRef<PathFinder, PathGrid> pathGridField =
            FieldRefAccess<PathFinder, PathGrid>("pathGrid");
        static readonly FieldRef<PathFinder, Building[]> edificeGridField =
            FieldRefAccess<PathFinder, Building[]>("edificeGrid");
        static readonly FieldRef<PathFinder, List<Blueprint>[]> blueprintGridField =
            FieldRefAccess<PathFinder, List<Blueprint>[]>("blueprintGrid");
        //static readonly FieldRef<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorField =
            //FieldRefAccess<PathFinder, RegionCostCalculatorWrapper>("regionCostCalculator");

        static readonly int[] Directions =
            StaticFieldRefAccess<int[]>(typeof(PathFinder), "Directions");

        static readonly MethodInfo methodIsCornerTouchAllowed =
            Method(typeof(PathFinder), "IsCornerTouchAllowed", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
        static readonly Func<PathFinder, int, int, int, int, int, int, bool> funcIsCornerTouchAllowed =
            (Func<PathFinder, int, int, int, int, int, int, bool>)Delegate.CreateDelegate(typeof(Func<PathFinder, int, int, int, int, int, int, bool>), methodIsCornerTouchAllowed);

        static readonly MethodInfo methodCalculateDestinationRect =
            Method(typeof(PathFinder), "CalculateDestinationRect");
        static readonly Func<PathFinder, LocalTargetInfo, PathEndMode, CellRect> funcCalculateDestinationRect =
            (Func<PathFinder, LocalTargetInfo, PathEndMode, CellRect>)Delegate.CreateDelegate(typeof(Func<PathFinder, LocalTargetInfo, PathEndMode, CellRect>), methodCalculateDestinationRect);

        static readonly MethodInfo methodGetAllowedArea =
            Method(typeof(PathFinder), "GetAllowedArea");
        static readonly Func<PathFinder, Pawn, Area> funcGetAllowedArea =
            (Func<PathFinder, Pawn, Area>)Delegate.CreateDelegate(typeof(Func<PathFinder, Pawn, Area>), methodGetAllowedArea);

        static readonly MethodInfo methodDetermineHeuristicStrength =
            Method(typeof(PathFinder), "DetermineHeuristicStrength");
        static readonly Func<PathFinder, Pawn, IntVec3, LocalTargetInfo, float> funcDetermineHeuristicStrength =
            (Func<PathFinder, Pawn, IntVec3, LocalTargetInfo, float>)Delegate.CreateDelegate(typeof(Func<PathFinder, Pawn, IntVec3, LocalTargetInfo, float>), methodDetermineHeuristicStrength);

        private static readonly MethodInfo methodDebugDrawRichData =
            Method(typeof(PathFinder), "DebugDrawRichData");
        private static readonly Action<PathFinder> actionDebugDrawRichData =
            (Action<PathFinder>)Delegate.CreateDelegate(typeof(Action<PathFinder>), methodDebugDrawRichData);

        private static readonly MethodInfo methodPfProfilerEndSample =
            Method(typeof(PathFinder), "PfProfilerEndSample");
        private static readonly Action<PathFinder> actionPfProfilerEndSample =
            (Action<PathFinder>)Delegate.CreateDelegate(typeof(Action<PathFinder>), methodPfProfilerEndSample);

        private static readonly MethodInfo methodPfProfilerBeginSample =
            Method(typeof(PathFinder), "PfProfilerBeginSample");
        private static readonly Action<PathFinder, string> actionPfProfilerBeginSample =
            (Action<PathFinder, string>)Delegate.CreateDelegate(typeof(Action<PathFinder, string>), methodPfProfilerBeginSample);

        private static readonly SimpleCurve RegionHeuristicWeightByNodesOpened =
            StaticFieldRefAccess<SimpleCurve>(typeof(PathFinder), "RegionHeuristicWeightByNodesOpened");

        public class CostNodeComparer2 : IComparer<CostNode2>
        {
            public int Compare(CostNode2 a, CostNode2 b)
            {
                return a.cost.CompareTo(b.cost);
            }
        }
        public struct CostNode2
        {
            public int index;

            public int cost;

            public CostNode2(int index, int cost)
            {
                this.index = index;
                this.cost = cost;
            }
        }
        public struct PathFinderNodeFast2
        {
            public int knownCost;

            public int heuristicCost;

            public int parentIndex;

            public int costNodeCost;

            public ushort status;
        }

        public static void InitStatusesAndPushStartNode2(PathFinder __instance, ref int curIndex, IntVec3 start)
        {
            statusOpenValue += 2;
            statusClosedValue += 2;

            int size = mapSizeXField(__instance) * mapSizeZField(__instance);
            if (calcGrid == null || calcGrid.Length < size)
            {
                calcGrid = new PathFinderNodeFast2[size];
            }

            if (statusClosedValue >= 65435)
            {
                int num = calcGrid.Length;
                for (int i = 0; i < num; i++)
                {
                    calcGrid[i].status = 0;
                }

                statusOpenValue = 1;
                statusClosedValue = 2;
            }
            curIndex = cellIndicesField(__instance).CellToIndex(start);
            calcGrid[curIndex].knownCost = 0;
            calcGrid[curIndex].heuristicCost = 0;
            calcGrid[curIndex].costNodeCost = 0;
            calcGrid[curIndex].parentIndex = curIndex;
            calcGrid[curIndex].status = statusOpenValue;
            if(openList == null)
            {
                openList = new FastPriorityQueue<CostNode2>();
            }
            openList.Clear();
            openList.Push(new CostNode2(curIndex, 0));
        }

        public static PawnPath FinalizedPath2(PathFinder __instance, int finalIndex, bool usedRegionHeuristics)
        {
            //HACK - fix pool
            //PawnPath emptyPawnPath = map(__instance).pawnPathPool.GetEmptyPawnPath();
            PawnPath emptyPawnPath = new PawnPath();
            int num = finalIndex;
            while (true)
            {
                int parentIndex = calcGrid[num].parentIndex;
                emptyPawnPath.AddNode(cellIndicesField(__instance).IndexToCell(num));
                if (num == parentIndex)
                {
                    break;
                }

                num = parentIndex;
            }
            emptyPawnPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
            return emptyPawnPath;
        }

        public static void CalculateAndAddDisallowedCorners2(PathFinder __instance, TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
        {
            if (disallowedCornerIndices == null)
            {
                disallowedCornerIndices = new List<int>();
            }
            else
            {
                disallowedCornerIndices.Clear();
            }
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                Map map = mapField(__instance);
                if (!funcIsCornerTouchAllowed(__instance, minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, minZ));
                }

                if (!funcIsCornerTouchAllowed(__instance, minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, maxZ));
                }

                if (!funcIsCornerTouchAllowed(__instance, maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, maxZ));
                }

                if (!funcIsCornerTouchAllowed(__instance, maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, minZ));
                }
            }
        }

        public static bool FindPath(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            if (DebugSettings.pathThroughWalls)
            {
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            }

            Pawn pawn = traverseParms.pawn;
            if (pawn != null && pawn.Map != mapField(__instance))
            {
                Log.Error(string.Concat("Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=", pawn, " pawn.Map=", pawn.Map, " map=", mapField(__instance)));
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
            else if (!mapField(__instance).reachability.CanReach(start, dest, peMode, traverseParms))
            {
                __result = PawnPath.NotFound;
                return false;
            }

            actionPfProfilerBeginSample(__instance, string.Concat("FindPath for ", pawn, " from ", start, " to ", dest, dest.HasThing ? (" at " + dest.Cell) : ""));
            cellIndicesField(__instance) = mapField(__instance).cellIndices;
            pathGridField(__instance) = mapField(__instance).pathGrid;
            edificeGridField(__instance) = mapField(__instance).edificeGrid.InnerArray;
            blueprintGridField(__instance) = mapField(__instance).blueprintGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int curIndex = cellIndicesField(__instance).CellToIndex(start);
            int num = cellIndicesField(__instance).CellToIndex(dest.Cell);
            ByteGrid byteGrid = pawn?.GetAvoidGrid();
            bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag3 = !flag;
            CellRect destinationRect = funcCalculateDestinationRect(__instance, dest, peMode);
            bool flag4 = destinationRect.Width == 1 && destinationRect.Height == 1;
            int[] array = mapField(__instance).pathGrid.pathGrid;
            TerrainDef[] topGrid = mapField(__instance).terrainGrid.topGrid;
            EdificeGrid edificeGrid = mapField(__instance).edificeGrid;
            int num2 = 0;
            int num3 = 0;
            Area allowedArea = funcGetAllowedArea(__instance, pawn);
            bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = !flag && start.GetRegion(mapField(__instance)) != null && flag2;
            bool flag7 = !flag || !flag3;
            bool flag8 = false;
            bool flag9 = pawn?.Drafted ?? false;
            int num4 = (pawn?.IsColonist ?? false) ? 100000 : 2000;
            int num5 = 0;
            int num6 = 0;
            float num7 = funcDetermineHeuristicStrength(__instance, pawn, start, dest);
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
            CalculateAndAddDisallowedCorners2(__instance, traverseParms, peMode, destinationRect);
            InitStatusesAndPushStartNode2(__instance, ref curIndex, start);
            while (true)
            {
                actionPfProfilerBeginSample(__instance, "Open cell");
                if (openList.Count <= 0)
                {
                    string text = (pawn != null && pawn.CurJob != null) ? pawn.CurJob.ToString() : "null";
                    string text2 = (pawn != null && pawn.Faction != null) ? pawn.Faction.ToString() : "null";
                    Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " ran out of cells to process.\nJob:", text, "\nFaction: ", text2));
                    actionDebugDrawRichData(__instance);
                    actionPfProfilerEndSample(__instance);
                    actionPfProfilerEndSample(__instance);
                    __result = PawnPath.NotFound;
                    return false;
                }

                num5 += openList.Count;
                num6++;
                CostNode2 costNode = openList.Pop();
                curIndex = costNode.index;
                if (costNode.cost != calcGrid[curIndex].costNodeCost)
                {
                    actionPfProfilerEndSample(__instance);
                    continue;
                }

                if (calcGrid[curIndex].status == statusClosedValue)
                {
                    actionPfProfilerEndSample(__instance);
                    continue;
                }

                IntVec3 c = cellIndicesField(__instance).IndexToCell(curIndex);
                int x2 = c.x;
                int z2 = c.z;
                if (flag4)
                {
                    if (curIndex == num)
                    {
                        actionPfProfilerEndSample(__instance);
                        PawnPath result = FinalizedPath2(__instance, curIndex, flag8);
                        actionPfProfilerEndSample(__instance);
                        __result = result;
                        return false;
                    }
                }
                else if (destinationRect.Contains(c) && !disallowedCornerIndices.Contains(curIndex))
                {
                    actionPfProfilerEndSample(__instance);
                    PawnPath result2 = FinalizedPath2(__instance, curIndex, flag8);
                    actionPfProfilerEndSample(__instance);
                    __result = result2;
                    return false;
                }

                if (num2 > 160000)
                {
                    break;
                }

                actionPfProfilerEndSample(__instance);
                actionPfProfilerBeginSample(__instance, "Neighbor consideration");
                for (int i = 0; i < 8; i++)
                {
                    uint num10 = (uint)(x2 + Directions[i]);
                    uint num11 = (uint)(z2 + Directions[i + 8]);
                    if (num10 >= mapSizeXField(__instance) || num11 >= mapSizeZField(__instance))
                    {
                        continue;
                    }

                    int num12 = (int)num10;
                    int num13 = (int)num11;
                    int num14 = cellIndicesField(__instance).CellToIndex(num12, num13);
                    if (calcGrid[num14].status == statusClosedValue && !flag8)
                    {
                        continue;
                    }

                    int num15 = 0;
                    bool flag10 = false;
                    if (!flag2 && new IntVec3(num12, 0, num13).GetTerrain(mapField(__instance)).HasTag("Water"))
                    {
                        continue;
                    }

                    if (!pathGridField(__instance).WalkableFast(num14))
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
                            if (PathFinder.BlocksDiagonalMovement(curIndex - mapSizeXField(__instance), mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex + 1, mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 5:
                            if (PathFinder.BlocksDiagonalMovement(curIndex + mapSizeXField(__instance), mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex + 1, mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 6:
                            if (PathFinder.BlocksDiagonalMovement(curIndex + mapSizeXField(__instance), mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex - 1, mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 7:
                            if (PathFinder.BlocksDiagonalMovement(curIndex - mapSizeXField(__instance), mapField(__instance)))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (PathFinder.BlocksDiagonalMovement(curIndex - 1, mapField(__instance)))
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

                    Building building2 = edificeGridField(__instance)[num14];
                    if (building2 != null)
                    {
                        actionPfProfilerBeginSample(__instance, "Edifices");
                        int buildingCost = PathFinder.GetBuildingCost(building2, traverseParms, pawn);
                        if (buildingCost == int.MaxValue)
                        {
                            actionPfProfilerEndSample(__instance);
                            continue;
                        }

                        num16 += buildingCost;
                        actionPfProfilerEndSample(__instance);
                    }

                    List<Blueprint> list = blueprintGridField(__instance)[num14];
                    if (list != null)
                    {
                        actionPfProfilerBeginSample(__instance, "Blueprints");
                        int num17 = 0;
                        for (int j = 0; j < list.Count; j++)
                        {
                            num17 = Mathf.Max(num17, PathFinder.GetBlueprintCost(list[j], pawn));
                        }

                        if (num17 == int.MaxValue)
                        {
                            actionPfProfilerEndSample(__instance);
                            continue;
                        }

                        num16 += num17;
                        actionPfProfilerEndSample(__instance);
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
                        calcGrid[num14].heuristicCost = Mathf.RoundToInt(get_regionCostCalculator(__instance).GetPathCostFromDestToRegion(num14) * RegionHeuristicWeightByNodesOpened.Evaluate(num3));
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
                    openList.Push(new CostNode2(num14, num21));
                }

                actionPfProfilerEndSample(__instance);
                num2++;
                calcGrid[curIndex].status = statusClosedValue;
                if (num3 >= num4 && flag6 && !flag8)
                {
                    flag8 = true;
                    get_regionCostCalculator(__instance).Init(destinationRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, disallowedCornerIndices);
                    InitStatusesAndPushStartNode2(__instance, ref curIndex, start);
                    openList.Clear();
                    openList.Push(new CostNode2(curIndex, 0));
                    num3 = 0;
                    num2 = 0;
                }
            }

            Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " hit search limit of ", 160000, " cells."));
            actionDebugDrawRichData(__instance);
            actionPfProfilerEndSample(__instance);
            actionPfProfilerEndSample(__instance);
            __result = PawnPath.NotFound;
            return false;
        }

        public static RegionCostCalculatorWrapper get_regionCostCalculator(PathFinder __instance)
        {
            if (!regionCostCalculatorDict.TryGetValue(__instance, out RegionCostCalculatorWrapper regionCostCalculatorWrapper)) {
                regionCostCalculatorWrapper = new RegionCostCalculatorWrapper(mapField(__instance));
                regionCostCalculatorDict[__instance] = regionCostCalculatorWrapper;
            }
            return regionCostCalculatorWrapper;
        }
    }
}
