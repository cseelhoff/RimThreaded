using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Diagnostics;
using UnityEngine;
using System.Threading;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class PathFinder_Patch
    {
        [ThreadStatic]
        static List<int> disallowedCornerIndices;

        public static Dictionary<int, RegionCostCalculatorWrapper> regionCostCalculatorWrappers =
            new Dictionary<int, RegionCostCalculatorWrapper>();
        public static Dictionary<int, PathFinderNodeFast[]> calcGrids =
            new Dictionary<int, PathFinderNodeFast[]>();
        public static Dictionary<int, FastPriorityQueue<CostNode2>> openLists =
            new Dictionary<int, FastPriorityQueue<CostNode2>>();
        public static Dictionary<int, ushort> openValues =
            new Dictionary<int, ushort>();
        public static Dictionary<int, ushort> closedValues =
            new Dictionary<int, ushort>();

        public static FieldRef<PathFinder, Map> mapField =
            FieldRefAccess<PathFinder, Map>("map");
        public static FieldRef<PathFinder, int> mapSizeXField =
            FieldRefAccess<PathFinder, int>("mapSizeX");
        public static FieldRef<PathFinder, int> mapSizeZField =
            FieldRefAccess<PathFinder, int>("mapSizeZ");
        public static FieldRef<PathFinder, CellIndices> cellIndicesField =
            FieldRefAccess<PathFinder, CellIndices>("cellIndices");
        public static FieldRef<PathFinder, PathGrid> pathGridField =
            FieldRefAccess<PathFinder, PathGrid>("pathGrid");
        public static FieldRef<PathFinder, Building[]> edificeGridField =
            FieldRefAccess<PathFinder, Building[]>("edificeGrid");
        public static FieldRef<PathFinder, List<Blueprint>[]> blueprintGridField =
            FieldRefAccess<PathFinder, List<Blueprint>[]>("blueprintGrid");
        //public static AccessTools.FieldRef<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorField =
            //AccessTools.FieldRefAccess<PathFinder, RegionCostCalculatorWrapper>("regionCostCalculator");
        public static Type costNodeType = TypeByName("Verse.AI.PathFinder+CostNode");
        public static ConstructorInfo constructorCostNode = costNodeType.GetConstructors()[0];
        public static Dictionary<int, PathFinderNodeFast[]> calcGridDict2 =
            new Dictionary<int, PathFinderNodeFast[]>();

        public static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve =
            StaticFieldRefAccess<SimpleCurve>(typeof(PathFinder), "NonRegionBasedHeuristicStrengthHuman_DistanceCurve");
        public static readonly int[] Directions =
            StaticFieldRefAccess<int[]>(typeof(PathFinder), "Directions");

        static readonly MethodInfo methodIsCornerTouchAllowed =
            Method(typeof(PathFinder), "IsCornerTouchAllowed", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
        static readonly Func<PathFinder, int, int, int, int, int, int, bool> funcIsCornerTouchAllowed =
            (Func<PathFinder, int, int, int, int, int, int, bool>)Delegate.CreateDelegate(typeof(Func<PathFinder, int, int, int, int, int, int, bool>), methodIsCornerTouchAllowed);


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
        public struct PathFinderNodeFast
        {
            public int knownCost;

            public int heuristicCost;

            public int parentIndex;

            public int costNodeCost;

            public ushort status;
        }
        [Conditional("PFPROFILE")]
        private static void PfProfilerBeginSample(string s)
        {
        }
        private static CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
        {
            CellRect result = (dest.HasThing && peMode != PathEndMode.OnCell) ? dest.Thing.OccupiedRect() : CellRect.SingleCell(dest.Cell);
            if (peMode == PathEndMode.Touch)
            {
                result = result.ExpandedBy(1);
            }

            return result;
        }
        private static Area GetAllowedArea(Pawn pawn)
        {
            if (pawn != null && pawn.playerSettings != null && !pawn.Drafted && ForbidUtility.CaresAboutForbidden(pawn, cellTarget: true))
            {
                Area area = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
                if (area != null && area.TrueCount <= 0)
                {
                    area = null;
                }

                return area;
            }

            return null;
        }
        private static float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
        {
            if (pawn != null && pawn.RaceProps.Animal)
            {
                return 1.75f;
            }

            float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
            return Mathf.RoundToInt(NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
        }
        public static void CalculateAndAddDisallowedCorners2(List<int> disallowedCornerIndices, Map map, PathEndMode peMode, CellRect destinationRect)
        {
            disallowedCornerIndices.Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!IsCornerTouchAllowed2(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1, map))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, minZ));
                }

                if (!IsCornerTouchAllowed2(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1, map))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, maxZ));
                }

                if (!IsCornerTouchAllowed2(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1, map))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, maxZ));
                }

                if (!IsCornerTouchAllowed2(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1, map))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, minZ));
                }
            }
        }
        private static bool IsCornerTouchAllowed2(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, Map map)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, map);
        }
        public static void InitStatusesAndPushStartNode2(ref int curIndex, IntVec3 start, CellIndices cellIndices, PathFinderNodeFast[] pathFinderNodeFast, FastPriorityQueue<CostNode2> fastPriorityQueue, ref ushort local_statusOpenValue, ref ushort local_statusClosedValue)
        {
            local_statusOpenValue += 2;
            local_statusClosedValue += 2;
            if (local_statusClosedValue >= 65435)
            {
                int num = pathFinderNodeFast.Length;
                for (int i = 0; i < num; i++)
                {
                    pathFinderNodeFast[i].status = 0;
                }

                local_statusOpenValue = 1;
                local_statusClosedValue = 2;
            }
            curIndex = cellIndices.CellToIndex(start);
            pathFinderNodeFast[curIndex].knownCost = 0;
            pathFinderNodeFast[curIndex].heuristicCost = 0;
            pathFinderNodeFast[curIndex].costNodeCost = 0;
            pathFinderNodeFast[curIndex].parentIndex = curIndex;
            pathFinderNodeFast[curIndex].status = local_statusOpenValue;
            fastPriorityQueue.Clear();

            //CostNode2 newCostNode = constructorCostNode.Invoke(new object[] { curIndex, 0 });
            fastPriorityQueue.Push(new CostNode2(curIndex, 0));
        }
        public static void InitStatusesAndPushStartNode3(ref int curIndex, IntVec3 start, CellIndices cellIndices, PathFinderNodeFast[] calcGrid, ref ushort local_statusOpenValue, ref ushort local_statusClosedValue)
        {
            local_statusOpenValue += 2;
            local_statusClosedValue += 2;
            if (local_statusClosedValue >= 65435)
            {
                int num = calcGrid.Length;
                for (int i = 0; i < num; i++)
                {
                    calcGrid[i].status = 0;
                }

                local_statusOpenValue = 1;
                local_statusClosedValue = 2;
            }
            openValues[Thread.CurrentThread.ManagedThreadId] = local_statusOpenValue;
            closedValues[Thread.CurrentThread.ManagedThreadId] = local_statusClosedValue;
            curIndex = cellIndices.CellToIndex(start);
            calcGrid[curIndex].knownCost = 0;
            calcGrid[curIndex].heuristicCost = 0;
            calcGrid[curIndex].costNodeCost = 0;
            calcGrid[curIndex].parentIndex = curIndex;
            calcGrid[curIndex].status = local_statusOpenValue;

            //fastPriorityQueue.Clear();
            //object newCostNode = constructorCostNode.Invoke(new object[] { curIndex, 0 });
            //fastPriorityQueue.Push(newCostNode);
        }

        private static void DebugDrawRichData()
        {
        }
        [Conditional("PFPROFILE")]
        private static void PfProfilerEndSample()
        {
        }
        public static PawnPath FinalizedPath2(int finalIndex, bool usedRegionHeuristics, CellIndices cellIndices, PathFinderNodeFast[] calcGrid)
        {
            //HACK - fix pool
            //PawnPath emptyPawnPath = map(__instance).pawnPathPool.GetEmptyPawnPath();
            PawnPath emptyPawnPath = new PawnPath();
            int num = finalIndex;
            while (true)
            {
                int parentIndex = calcGrid[num].parentIndex;
                emptyPawnPath.AddNode(cellIndices.IndexToCell(num));
                if (num == parentIndex)
                {
                    break;
                }

                num = parentIndex;
            }
            emptyPawnPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
            return emptyPawnPath;
        }

        private static readonly SimpleCurve RegionHeuristicWeightByNodesOpened = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(3500f, 1f),
            new CurvePoint(4500f, 5f),
            new CurvePoint(30000f, 50f),
            new CurvePoint(100000f, 500f)
        };

        public class CostNodeComparer2 : IComparer<CostNode2>
        {
            public int Compare(CostNode2 a, CostNode2 b)
            {
                return a.cost.CompareTo(b.cost);
            }
        }

        //public static FastPriorityQueue<CostNode2> openList = new FastPriorityQueue<CostNode2>(new CostNodeComparer2());
        //public static PathFinderNodeFast[] calcGrid = new PathFinderNodeFast[40000];
        //public static ushort statusOpenValue = 1;
        //public static ushort statusClosedValue = 2;
        public static bool FindPath(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            FastPriorityQueue<CostNode2> openList = getOpenList();
            ushort statusOpenValue = getOpenValue();
            ushort statusClosedValue = getClosedValue();
            PathFinderNodeFast[] calcGrid = getCalcGrid(__instance);
            RegionCostCalculatorWrapper regionCostCalculator = getRegionCostCalculatorWrapper(__instance);

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

            PfProfilerBeginSample(string.Concat("FindPath for ", pawn, " from ", start, " to ", dest, dest.HasThing ? (" at " + dest.Cell) : ""));
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
            CellRect destinationRect = CalculateDestinationRect(dest, peMode);
            bool flag4 = destinationRect.Width == 1 && destinationRect.Height == 1;
            int[] array = mapField(__instance).pathGrid.pathGrid;
            TerrainDef[] topGrid = mapField(__instance).terrainGrid.topGrid;
            EdificeGrid edificeGrid = mapField(__instance).edificeGrid;
            int num2 = 0;
            int num3 = 0;
            Area allowedArea = GetAllowedArea(pawn);
            bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = !flag && start.GetRegion(mapField(__instance)) != null && flag2;
            bool flag7 = !flag || !flag3;
            bool flag8 = false;
            bool flag9 = pawn?.Drafted ?? false;
            int num4 = (pawn?.IsColonist ?? false) ? 100000 : 2000;
            int num5 = 0;
            int num6 = 0;
            float num7 = DetermineHeuristicStrength(pawn, start, dest);
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
            List<int> localDisallowedCornerIndices = getDisallowedCornerIndices(__instance, peMode, destinationRect);
            //CalculateAndAddDisallowedCorners2(disallowedCornerIndicesField(__instance), mapField(__instance), peMode, destinationRect);
            InitStatusesAndPushStartNode3(ref curIndex, start, cellIndicesField(__instance), calcGrid, ref statusOpenValue, ref statusClosedValue);
            openList.Clear();
            openList.Push(new CostNode2(curIndex, 0));
            while (true)
            {
                PfProfilerBeginSample("Open cell");
                if (openList.Count <= 0)
                {
                    string text = (pawn != null && pawn.CurJob != null) ? pawn.CurJob.ToString() : "null";
                    string text2 = (pawn != null && pawn.Faction != null) ? pawn.Faction.ToString() : "null";
                    Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " ran out of cells to process.\nJob:", text, "\nFaction: ", text2));
                    DebugDrawRichData();
                    PfProfilerEndSample();
                    PfProfilerEndSample();
                    __result = PawnPath.NotFound;
                    return false;
                }

                num5 += openList.Count;
                num6++;
                CostNode2 costNode = openList.Pop();
                curIndex = costNode.index;
                if (costNode.cost != calcGrid[curIndex].costNodeCost)
                {
                    PfProfilerEndSample();
                    continue;
                }

                if (calcGrid[curIndex].status == statusClosedValue)
                {
                    PfProfilerEndSample();
                    continue;
                }

                IntVec3 c = cellIndicesField(__instance).IndexToCell(curIndex);
                int x2 = c.x;
                int z2 = c.z;
                if (flag4)
                {
                    if (curIndex == num)
                    {
                        PfProfilerEndSample();
                        PawnPath result = FinalizedPath2(curIndex, flag8, cellIndicesField(__instance), calcGrid);
                        PfProfilerEndSample();
                        __result = result;
                        return false;
                    }
                }
                else if (destinationRect.Contains(c) && !localDisallowedCornerIndices.Contains(curIndex))
                {
                    PfProfilerEndSample();
                    PawnPath result2 = FinalizedPath2(curIndex, flag8, cellIndicesField(__instance), calcGrid);
                    PfProfilerEndSample();
                    __result = result2;
                    return false;
                }

                if (num2 > 160000)
                {
                    break;
                }

                PfProfilerEndSample();
                PfProfilerBeginSample("Neighbor consideration");
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
                        PfProfilerBeginSample("Edifices");
                        int buildingCost = PathFinder.GetBuildingCost(building2, traverseParms, pawn);
                        if (buildingCost == int.MaxValue)
                        {
                            PfProfilerEndSample();
                            continue;
                        }

                        num16 += buildingCost;
                        PfProfilerEndSample();
                    }

                    List<Blueprint> list = blueprintGridField(__instance)[num14];
                    if (list != null)
                    {
                        PfProfilerBeginSample("Blueprints");
                        int num17 = 0;
                        for (int j = 0; j < list.Count; j++)
                        {
                            num17 = Mathf.Max(num17, PathFinder.GetBlueprintCost(list[j], pawn));
                        }

                        if (num17 == int.MaxValue)
                        {
                            PfProfilerEndSample();
                            continue;
                        }

                        num16 += num17;
                        PfProfilerEndSample();
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
                        calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)regionCostCalculator.GetPathCostFromDestToRegion(num14) * RegionHeuristicWeightByNodesOpened.Evaluate(num3));
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
                        calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)num20 * num7);
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

                PfProfilerEndSample();
                num2++;
                calcGrid[curIndex].status = statusClosedValue;
                if (num3 >= num4 && flag6 && !flag8)
                {
                    flag8 = true;
                    regionCostCalculator.Init(destinationRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, localDisallowedCornerIndices);
                    InitStatusesAndPushStartNode3(ref curIndex, start, cellIndicesField(__instance), calcGrid, ref statusOpenValue, ref statusClosedValue);
                    openList.Clear();
                    openList.Push(new CostNode2(curIndex, 0));
                    num3 = 0;
                    num2 = 0;
                }
            }

            Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " hit search limit of ", 160000, " cells."));
            DebugDrawRichData();
            PfProfilerEndSample();
            PfProfilerEndSample();
            __result = PawnPath.NotFound;
            return false;
        }

        public static ushort getClosedValue()
        {            
            if (!closedValues.TryGetValue(Thread.CurrentThread.ManagedThreadId, out ushort local_statusClosedValue))
            {
                local_statusClosedValue = 2;
            }
            return local_statusClosedValue;
        }

        public static ushort getOpenValue()
        {
            if (!openValues.TryGetValue(Thread.CurrentThread.ManagedThreadId, out ushort local_statusOpenValue))
            {
                local_statusOpenValue = 1;
            }
            return local_statusOpenValue;
        }

        public static FastPriorityQueue<CostNode2> getOpenList()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!openLists.TryGetValue(tID, out FastPriorityQueue<CostNode2> local_openList))
            {
                local_openList = new FastPriorityQueue<CostNode2>(new CostNodeComparer2());
                lock (openLists)
                {
                    openLists[tID] = local_openList;
                }
            }
            return local_openList;
        }

        public static PathFinderNodeFast[] getCalcGrid(PathFinder __instance)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            int size = mapSizeXField(__instance) * mapSizeZField(__instance);
            if (!calcGrids.TryGetValue(tID, out PathFinderNodeFast[] local_calcGrid) || local_calcGrid.Length < size)
            {
                local_calcGrid = new PathFinderNodeFast[size];
                lock (calcGrids)
                {
                    calcGrids[tID] = local_calcGrid;
                }
            }
            return local_calcGrid;
        }
        public static RegionCostCalculatorWrapper getRegionCostCalculatorWrapper(PathFinder __instance)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!regionCostCalculatorWrappers.TryGetValue(tID, out RegionCostCalculatorWrapper regionCostCalculatorWrapper))
            {
                regionCostCalculatorWrapper = new RegionCostCalculatorWrapper(mapField(__instance));
                lock (regionCostCalculatorWrappers)
                {
                    regionCostCalculatorWrappers[tID] = regionCostCalculatorWrapper;
                }
            }
            Map map = RegionCostCalculatorWrapper_Patch.map(regionCostCalculatorWrapper);
            if(map.regionGrid == null)
            {
                regionCostCalculatorWrapper = new RegionCostCalculatorWrapper(mapField(__instance));
                lock (regionCostCalculatorWrappers)
                {
                    regionCostCalculatorWrappers[tID] = regionCostCalculatorWrapper;
                }
            }
            return regionCostCalculatorWrapper;
        }

        public static List<int> getDisallowedCornerIndices(PathFinder __instance, PathEndMode peMode, CellRect destinationRect)
        {
            if (disallowedCornerIndices == null)
            {
                disallowedCornerIndices = new List<int>(4);
            }
            else
            {
                disallowedCornerIndices.Clear();
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
            return disallowedCornerIndices;
        }
    }
}
