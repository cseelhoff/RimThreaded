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

namespace RimThreaded
{

    public class PathFinder_Patch
    {
        public static Dictionary<int, Dictionary<PathFinder, PathFinderNodeFast[]>> calcGrids = 
            new Dictionary<int, Dictionary<PathFinder, PathFinderNodeFast[]>>();
        public static Dictionary<int, FastPriorityQueue<CostNode2>> openLists = 
            new Dictionary<int, FastPriorityQueue<CostNode2>>();

        public static AccessTools.FieldRef<PathFinder, Map> mapField =
            AccessTools.FieldRefAccess<PathFinder, Map>("map");
        public static AccessTools.FieldRef<PathFinder, int> mapSizeXField =
            AccessTools.FieldRefAccess<PathFinder, int>("mapSizeX");
        public static AccessTools.FieldRef<PathFinder, int> mapSizeZField =
            AccessTools.FieldRefAccess<PathFinder, int>("mapSizeZ");
        public static AccessTools.FieldRef<PathFinder, CellIndices> cellIndicesField =
            AccessTools.FieldRefAccess<PathFinder, CellIndices>("cellIndices");
        public static AccessTools.FieldRef<PathFinder, PathGrid> pathGridField =
            AccessTools.FieldRefAccess<PathFinder, PathGrid>("pathGrid");
        public static AccessTools.FieldRef<PathFinder, Building[]> edificeGridField =
            AccessTools.FieldRefAccess<PathFinder, Building[]>("edificeGrid");
        public static AccessTools.FieldRef<PathFinder, List<Blueprint>[]> blueprintGridField =
            AccessTools.FieldRefAccess<PathFinder, List<Blueprint>[]>("blueprintGrid");
        public static AccessTools.FieldRef<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorField =
            AccessTools.FieldRefAccess<PathFinder, RegionCostCalculatorWrapper>("regionCostCalculator");
        public static AccessTools.FieldRef<PathFinder, List<int>> disallowedCornerIndicesField =
            AccessTools.FieldRefAccess<PathFinder, List<int>>("disallowedCornerIndices");
        public static Type costNodeType = AccessTools.TypeByName("Verse.AI.PathFinder+CostNode");
        //public static Type fastPriorityQueueCostNodeType = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType);
        public static ConstructorInfo constructorCostNode = costNodeType.GetConstructors()[0];
        //public static AccessTools.FieldRef<PathFinder, FastPriorityQueue<object>> openListField = 
            //AccessTools.FieldRefAccess<PathFinder, FastPriorityQueue<object>>("disallowedCornerIndices");
        /*
        public static AccessTools.FieldRef<PathFinder, PathFinderNodeFast[]> calcGridField =
            AccessTools.FieldRefAccess<PathFinder, PathFinderNodeFast[]>("calcGrid");
        */
        public static Dictionary<int, PathFinderNodeFast[]> calcGridDict2 =
            new Dictionary<int, PathFinderNodeFast[]>();

        public static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve =
            AccessTools.StaticFieldRefAccess<SimpleCurve>(typeof(PathFinder), "NonRegionBasedHeuristicStrengthHuman_DistanceCurve");
        public static readonly int[] Directions =
            AccessTools.StaticFieldRefAccess<int[]>(typeof(PathFinder), "Directions");

        public static object pLock = new object();
        
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
        public static void CalculateAndAddDisallowedCorners1(PathFinder __instance, TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
        {
            disallowedCornerIndicesField(__instance).Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!IsCornerTouchAllowed2(mapField(__instance), minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                {
                    disallowedCornerIndicesField(__instance).Add(mapField(__instance).cellIndices.CellToIndex(minX, minZ));
                }

                if (!IsCornerTouchAllowed2(mapField(__instance), minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                {
                    disallowedCornerIndicesField(__instance).Add(mapField(__instance).cellIndices.CellToIndex(minX, maxZ));
                }

                if (!IsCornerTouchAllowed2(mapField(__instance), maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                {
                    disallowedCornerIndicesField(__instance).Add(mapField(__instance).cellIndices.CellToIndex(maxX, maxZ));
                }

                if (!IsCornerTouchAllowed2(mapField(__instance), maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                {
                    disallowedCornerIndicesField(__instance).Add(mapField(__instance).cellIndices.CellToIndex(maxX, minZ));
                }
            }
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
        public static void CalculateAndAddDisallowedCorners2(List<int> disallowedCornerIndices2, Map map2, PathEndMode peMode, CellRect destinationRect)
        {
            disallowedCornerIndices2.Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!IsCornerTouchAllowed2(map2, minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                {
                    disallowedCornerIndices2.Add(map2.cellIndices.CellToIndex(minX, minZ));
                }

                if (!IsCornerTouchAllowed2(map2, minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                {
                    disallowedCornerIndices2.Add(map2.cellIndices.CellToIndex(minX, maxZ));
                }

                if (!IsCornerTouchAllowed2(map2, maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                {
                    disallowedCornerIndices2.Add(map2.cellIndices.CellToIndex(maxX, maxZ));
                }

                if (!IsCornerTouchAllowed2(map2, maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                {
                    disallowedCornerIndices2.Add(map2.cellIndices.CellToIndex(maxX, minZ));
                }
            }
        }
        private static bool IsCornerTouchAllowed2(Map map2, int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, map2);
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
        public static void InitStatusesAndPushStartNode3(ref int curIndex, IntVec3 start, CellIndices cellIndices, PathFinderNodeFast[] pathFinderNodeFast, ref ushort local_statusOpenValue, ref ushort local_statusClosedValue)
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
        public static PawnPath FinalizedPath2(int finalIndex, bool usedRegionHeuristics, CellIndices cellIndices, PathFinderNodeFast[] pathFinderNodeFast)
        {
            //HACK - fix pool
            //PawnPath emptyPawnPath = map(__instance).pawnPathPool.GetEmptyPawnPath();
            PawnPath emptyPawnPath = new PawnPath();
            int num = finalIndex;
            while (true)
            {
                int parentIndex = pathFinderNodeFast[num].parentIndex;
                emptyPawnPath.AddNode(cellIndices.IndexToCell(num));
                if (num == parentIndex)
                {
                    break;
                }

                num = parentIndex;
            }
            emptyPawnPath.SetupFound(pathFinderNodeFast[finalIndex].knownCost, usedRegionHeuristics);
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

        /*
        public static void Postfix_Constructor(PathFinder __instance, Map map)
        {
            int num = mapSizeX(__instance) * mapSizeZ(__instance);
            calcGridDict[__instance] = new PathFinderNodeFast[num];
            openListDict[__instance] = new FastPriorityQueue<CostNode>(new CostNodeComparer());
        }
        */
        public static bool FindPath(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            //Dictionary<PathFinder, PathFinderNodeFast[]> calcGrids1 = calcGrids[tID];
            //if (!calcGrids1.TryGetValue(__instance, out PathFinderNodeFast[] local_calcGrid))
            //{
                //local_calcGrid = new PathFinderNodeFast[mapSizeXField(__instance) * mapSizeZField(__instance)];
                //calcGrids1.Add(__instance, local_calcGrid);
            //}
            PathFinderNodeFast[] local_calcGrid = new PathFinderNodeFast[mapSizeXField(__instance) * mapSizeZField(__instance)]; //CHANGE

            //only local because CostNode is an internal struct
            FastPriorityQueue<CostNode2> local_openList = new FastPriorityQueue<CostNode2>(new CostNodeComparer2());
            //FastPriorityQueue<CostNode2> local_openList = openLists[tID];
            //FastPriorityQueue<object> local_openList = openListField(__instance);

            ushort local_statusOpenValue = 1;
            ushort local_statusClosedValue = 2;

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
            CellRect cellRect = CalculateDestinationRect(dest, peMode);
            bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
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

            CalculateAndAddDisallowedCorners1(__instance, traverseParms, peMode, cellRect);
            //InitStatusesAndPushStartNode(ref curIndex, start);
            InitStatusesAndPushStartNode2(ref curIndex, start, cellIndicesField(__instance), local_calcGrid, local_openList, ref local_statusOpenValue, ref local_statusClosedValue);

            while (true)
            {
                PfProfilerBeginSample("Open cell");
                if (local_openList.Count <= 0)
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

                num5 += local_openList.Count;
                num6++;
                //CostNode2 costNode = (CostNode2)local_openList.Pop();
                CostNode2 costNode = local_openList.Pop();
                curIndex = costNode.index;
                if (costNode.cost != local_calcGrid[curIndex].costNodeCost) //CHANGE
                {
                    PfProfilerEndSample();
                    continue;
                }

                if (local_calcGrid[curIndex].status == local_statusClosedValue) //CHANGE
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
                        //PawnPath result = FinalizedPath(curIndex, flag8);
                        PawnPath result = FinalizedPath2(curIndex, flag8, cellIndicesField(__instance), local_calcGrid); //CHANGE
                        PfProfilerEndSample();
                        __result = result;
                        return false;
                    }
                }
                else if (cellRect.Contains(c) && !disallowedCornerIndicesField(__instance).Contains(curIndex))
                {
                    PfProfilerEndSample();
                    //PawnPath result2 = FinalizedPath(curIndex, flag8);
                    PawnPath result2 = FinalizedPath2(curIndex, flag8, cellIndicesField(__instance), local_calcGrid); //CHANGE
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
                    if (local_calcGrid[num14].status == local_statusClosedValue && !flag8) //CHANGE
                    {
                        continue;
                    }

                    int num15 = 0;
                    bool flag10 = false;
                    //if (!flag2 && new IntVec3(num12, 0, num13).GetTerrain(mapField(__instance)).HasTag("Water"))                        
                    if (!flag2 && mapField(__instance).terrainGrid.topGrid[num13 * mapSizeXField(__instance) + num12].HasTag("Water"))
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

                        num15 += (int)((float)building.HitPoints * 0.2f);
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

                    int num18 = num16 + local_calcGrid[curIndex].knownCost; //CHANGE
                    ushort status = local_calcGrid[num14].status; //CHANGE
                    if (status == local_statusClosedValue || status == local_statusOpenValue) //CHANGE
                    {
                        int num19 = 0;
                        if (status == local_statusClosedValue) //CHANGE
                        {
                            num19 = num8;
                        }

                        if (local_calcGrid[num14].knownCost <= num18 + num19) //CHANGE
                        {
                            continue;
                        }
                    }

                    if (flag8)
                    {
                        local_calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)regionCostCalculatorField(__instance).GetPathCostFromDestToRegion(num14) * RegionHeuristicWeightByNodesOpened.Evaluate(num3)); //CHANGE
                        if (local_calcGrid[num14].heuristicCost < 0) //CHANGE
                        {
                            Log.ErrorOnce(string.Concat("Heuristic cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0xB8DC389);
                            local_calcGrid[num14].heuristicCost = 0; //CHANGE
                        }
                    }
                    else if (status != local_statusClosedValue && status != local_statusOpenValue) //CHANGE
                    {
                        int dx = Math.Abs(num12 - x);
                        int dz = Math.Abs(num13 - z);
                        int num20 = GenMath.OctileDistance(dx, dz, num8, num9);
                        local_calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)num20 * num7); //CHANGE
                    }

                    int num21 = num18 + local_calcGrid[num14].heuristicCost; //CHANGE
                    if (num21 < 0)
                    {
                        Log.ErrorOnce(string.Concat("Node cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0x53CB9DE);
                        num21 = 0;
                    }

                    local_calcGrid[num14].parentIndex = curIndex; //CHANGE
                    local_calcGrid[num14].knownCost = num18; //CHANGE
                    local_calcGrid[num14].status = local_statusOpenValue; //CHANGE
                    local_calcGrid[num14].costNodeCost = num21; //CHANGE
                    num3++;
                    //object newCostNode = constructorCostNode.Invoke(new object[] { num14, num21 });
                    local_openList.Push(new CostNode2(num14, num21));
                }

                PfProfilerEndSample();
                num2++;
                local_calcGrid[curIndex].status = local_statusClosedValue; //CHANGE
                if (num3 >= num4 && flag6 && !flag8)
                {
                    flag8 = true;
                    regionCostCalculatorField(__instance).Init(cellRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, disallowedCornerIndicesField(__instance));
                    //InitStatusesAndPushStartNode
                    InitStatusesAndPushStartNode2(ref curIndex, start, cellIndicesField(__instance), local_calcGrid, local_openList, ref local_statusOpenValue, ref local_statusClosedValue); //CHANGE
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

    }
}
