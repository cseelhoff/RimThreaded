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

namespace RimThreaded
{

    public class PathFinder_Patch
    {
        public static AccessTools.FieldRef<PathFinder, Map> map =
            AccessTools.FieldRefAccess<PathFinder, Map>("map");
        public static AccessTools.FieldRef<PathFinder, RegionCostCalculatorWrapper> regionCostCalculator =
            AccessTools.FieldRefAccess<PathFinder, RegionCostCalculatorWrapper>("regionCostCalculator");

        public static Dictionary<int, PathFinderNodeFast[]> calcGridDict2 =
            new Dictionary<int, PathFinderNodeFast[]>();
        public static Dictionary<int, FastPriorityQueue<CostNode>> openListDict2 =
            new Dictionary<int, FastPriorityQueue<CostNode>>();

        public static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve =
            AccessTools.StaticFieldRefAccess<SimpleCurve>(typeof(PathFinder), "NonRegionBasedHeuristicStrengthHuman_DistanceCurve");
        public static readonly int[] Directions =
            AccessTools.StaticFieldRefAccess<int[]>(typeof(PathFinder), "Directions");

        public static object pLock = new object();

        public struct CostNode
        {
            public int index;

            public int cost;

            public CostNode(int index, int cost)
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
        private static List<int> CalculateAndAddDisallowedCorners2(Map map2, PathEndMode peMode, CellRect destinationRect)
        {
            List<int> disallowedCornerIndices2 = new List<int>(4);
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
            return disallowedCornerIndices2;
        }
        private static bool IsCornerTouchAllowed2(Map map2, int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, map2);
        }
        private static void InitStatusesAndPushStartNode2(CellIndices cellIndices, ref int curIndex, IntVec3 start, PathFinderNodeFast[] pathFinderNodeFast, FastPriorityQueue<CostNode> fastPriorityQueue, ref ushort statusOpenValue2, ref ushort statusClosedValue2)
        {
            statusOpenValue2 += 2;
            statusClosedValue2 += 2;
            if (statusClosedValue2 >= 65435)
            {
                int num = pathFinderNodeFast.Length;
                for (int i = 0; i < num; i++)
                {
                    pathFinderNodeFast[i].status = 0;
                }

                statusOpenValue2 = 1;
                statusClosedValue2 = 2;
            }
            curIndex = cellIndices.CellToIndex(start);
            pathFinderNodeFast[curIndex].knownCost = 0;
            pathFinderNodeFast[curIndex].heuristicCost = 0;
            pathFinderNodeFast[curIndex].costNodeCost = 0;
            pathFinderNodeFast[curIndex].parentIndex = curIndex;
            pathFinderNodeFast[curIndex].status = statusOpenValue2;
            fastPriorityQueue.Clear();
            fastPriorityQueue.Push(new CostNode(curIndex, 0));
        }

        private static void DebugDrawRichData()
        {
        }
        [Conditional("PFPROFILE")]
        private static void PfProfilerEndSample()
        {
        }
        private static PawnPath FinalizedPath2(CellIndices cellIndices, int finalIndex, bool usedRegionHeuristics, PathFinderNodeFast[] pathFinderNodeFast)
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
        public class CostNodeComparer : IComparer<CostNode>
        {
            public int Compare(CostNode a, CostNode b)
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
            PathFinderNodeFast[] pathFinderNodeFast;
            FastPriorityQueue<CostNode> fastPriorityQueue;
            int tID = Thread.CurrentThread.ManagedThreadId;
            Map map2 = map(__instance);
            int mapSizeX2 = map2.Size.x; // mapSizeX(__instance);
            int mapSizeZ2 = map2.Size.z; // mapSizeZ(__instance);
            int mapCellCount = mapSizeX2 * mapSizeZ2;
            Pawn pawn = traverseParms.pawn;
            CellIndices cellIndices2 = map2.cellIndices;
            PathGrid pathGrid2 = map2.pathGrid;
            Building[] this_edificeGrid2 = map2.edificeGrid.InnerArray;
            List<Blueprint>[] blueprintGrid2 = map2.blueprintGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int curIndex = cellIndices2.CellToIndex(start);
            int num = cellIndices2.CellToIndex(dest.Cell);
            ByteGrid byteGrid = pawn?.GetAvoidGrid();
            bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag3 = !flag;
            CellRect cellRect = CalculateDestinationRect(dest, peMode);
            bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
            int[] array = map2.pathGrid.pathGrid;
            TerrainDef[] topGrid = map2.terrainGrid.topGrid;
            EdificeGrid edificeGrid = map2.edificeGrid;
            int num2 = 0;
            int num3 = 0;
            Area allowedArea = GetAllowedArea(pawn);
            bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = (!flag && start.GetRegion(map2) != null) & flag2;
            bool flag7 = !flag || !flag3;
            bool flag8 = false;
            bool flag9 = pawn?.Drafted ?? false;
            int num4 = (pawn?.IsColonist ?? false) ? 100000 : 2000;
            int num5 = 0;
            int num6 = 0;
            float num7 = DetermineHeuristicStrength(pawn, start, dest);
            int num8;
            int num9;
            ushort statusOpenValue2 = 1;
            ushort statusClosedValue2 = 2;
            List<int> disallowedCornerIndices2;
            //if (!calcGridDict2.TryGetValue(tID, out pathFinderNodeFast))
            //{
                pathFinderNodeFast = new PathFinderNodeFast[mapCellCount];
                //calcGridDict2.Add(tID, pathFinderNodeFast);
            //}
            //if (!openListDict2.TryGetValue(tID, out fastPriorityQueue))
            //{
                fastPriorityQueue = new FastPriorityQueue<CostNode>(new CostNodeComparer());
                //openListDict2.Add(tID, fastPriorityQueue);
            //}
                
            if (DebugSettings.pathThroughWalls)
            {
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            }

            if (pawn != null && pawn.Map != map2)
            {
                Log.Error("Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=" + pawn + " pawn.Map=" + pawn.Map + " map=" + map);
                __result = PawnPath.NotFound;
                return false;
            }

            if (!start.IsValid)
            {
                Log.Error("Tried to FindPath with invalid start " + start + ", pawn= " + pawn);
                __result = PawnPath.NotFound;
                return false;
            }

            if (!dest.IsValid)
            {
                Log.Error("Tried to FindPath with invalid dest " + dest + ", pawn= " + pawn);
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
            else if (!map2.reachability.CanReach(start, dest, peMode, traverseParms))
            {
                __result = PawnPath.NotFound;
                return false;
            }

            PfProfilerBeginSample("FindPath for " + pawn + " from " + start + " to " + dest + (dest.HasThing ? (" at " + dest.Cell) : ""));
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

            disallowedCornerIndices2 = CalculateAndAddDisallowedCorners2(map2, peMode, cellRect);

            lock (pLock)
            {
                InitStatusesAndPushStartNode2(cellIndices2, ref curIndex, start, pathFinderNodeFast, fastPriorityQueue, ref statusOpenValue2, ref statusClosedValue2);

                while (true)
                {
                    PfProfilerBeginSample("Open cell");
                    if (fastPriorityQueue.Count <= 0)
                    {
                        string text = (pawn != null && pawn.CurJob != null) ? pawn.CurJob.ToString() : "null";
                        string text2 = (pawn != null && pawn.Faction != null) ? pawn.Faction.ToString() : "null";
                        Log.Warning(pawn + " pathing from " + start + " to " + dest + " ran out of cells to process.\nJob:" + text + "\nFaction: " + text2);
                        DebugDrawRichData();
                        PfProfilerEndSample();
                        PfProfilerEndSample();
                        __result = PawnPath.NotFound;
                        return false;
                    }

                    num5 += fastPriorityQueue.Count;
                    num6++;
                    CostNode costNode = fastPriorityQueue.Pop();
                    curIndex = costNode.index;
                    if (costNode.cost != pathFinderNodeFast[curIndex].costNodeCost)
                    {
                        PfProfilerEndSample();
                        continue;
                    }

                    if (pathFinderNodeFast[curIndex].status == statusClosedValue2)
                    {
                        PfProfilerEndSample();
                        continue;
                    }

                    IntVec3 c = cellIndices2.IndexToCell(curIndex);
                    int x2 = c.x;
                    int z2 = c.z;
                    if (flag4)
                    {
                        if (curIndex == num)
                        {
                            PfProfilerEndSample();
                            PawnPath result = FinalizedPath2(cellIndices2, curIndex, flag8, pathFinderNodeFast);
                            PfProfilerEndSample();
                            __result = result;
                            return false;
                        }
                    }
                    else if (cellRect.Contains(c) && !disallowedCornerIndices2.Contains(curIndex))
                    {
                        PfProfilerEndSample();
                        PawnPath result2 = FinalizedPath2(cellIndices2, curIndex, flag8, pathFinderNodeFast);
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
                        if (num10 >= mapSizeX2 || num11 >= mapSizeZ2)
                        {
                            continue;
                        }

                        int num12 = (int)num10;
                        int num13 = (int)num11;
                        int num14 = cellIndices2.CellToIndex(num12, num13);
                        if (pathFinderNodeFast[num14].status == statusClosedValue2 && !flag8)
                        {
                            continue;
                        }

                        int num15 = 0;
                        bool flag10 = false;
                        if (!flag2 && new IntVec3(num12, 0, num13).GetTerrain(map2).HasTag("Water"))
                        {
                            continue;
                        }

                        if (!pathGrid2.WalkableFast(num14))
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
                                if (PathFinder.BlocksDiagonalMovement(curIndex - mapSizeX2, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                if (PathFinder.BlocksDiagonalMovement(curIndex + 1, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                break;
                            case 5:
                                if (PathFinder.BlocksDiagonalMovement(curIndex + mapSizeX2, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                if (PathFinder.BlocksDiagonalMovement(curIndex + 1, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                break;
                            case 6:
                                if (PathFinder.BlocksDiagonalMovement(curIndex + mapSizeX2, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                if (PathFinder.BlocksDiagonalMovement(curIndex - 1, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                break;
                            case 7:
                                if (PathFinder.BlocksDiagonalMovement(curIndex - mapSizeX2, map2))
                                {
                                    if (flag7)
                                    {
                                        continue;
                                    }

                                    num15 += 70;
                                }

                                if (PathFinder.BlocksDiagonalMovement(curIndex - 1, map2))
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

                        Building building2 = this_edificeGrid2[num14];
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

                        List<Blueprint> list = blueprintGrid2[num14];
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

                        int num18 = num16 + pathFinderNodeFast[curIndex].knownCost;
                        ushort status = pathFinderNodeFast[num14].status;
                        if (status == statusClosedValue2 || status == statusOpenValue2)
                        {
                            int num19 = 0;
                            if (status == statusClosedValue2)
                            {
                                num19 = num8;
                            }

                            if (pathFinderNodeFast[num14].knownCost <= num18 + num19)
                            {
                                continue;
                            }
                        }

                        if (flag8)
                        {
                            pathFinderNodeFast[num14].heuristicCost = Mathf.RoundToInt((float)regionCostCalculator(__instance).GetPathCostFromDestToRegion(num14) * RegionHeuristicWeightByNodesOpened.Evaluate(num3));
                            if (pathFinderNodeFast[num14].heuristicCost < 0)
                            {
                                Log.ErrorOnce("Heuristic cost overflow for " + pawn.ToStringSafe() + " pathing from " + start + " to " + dest + ".", pawn.GetHashCode() ^ 0xB8DC389);
                                pathFinderNodeFast[num14].heuristicCost = 0;
                            }
                        }
                        else if (status != statusClosedValue2 && status != statusOpenValue2)
                        {
                            int dx = Math.Abs(num12 - x);
                            int dz = Math.Abs(num13 - z);
                            int num20 = GenMath.OctileDistance(dx, dz, num8, num9);
                            pathFinderNodeFast[num14].heuristicCost = Mathf.RoundToInt((float)num20 * num7);
                        }

                        int num21 = num18 + pathFinderNodeFast[num14].heuristicCost;
                        if (num21 < 0)
                        {
                            Log.ErrorOnce("Node cost overflow for " + pawn.ToStringSafe() + " pathing from " + start + " to " + dest + ".", pawn.GetHashCode() ^ 0x53CB9DE);
                            num21 = 0;
                        }

                        pathFinderNodeFast[num14].parentIndex = curIndex;
                        pathFinderNodeFast[num14].knownCost = num18;
                        pathFinderNodeFast[num14].status = statusOpenValue2;
                        pathFinderNodeFast[num14].costNodeCost = num21;
                        num3++;
                        fastPriorityQueue.Push(new CostNode(num14, num21));
                    }

                    PfProfilerEndSample();
                    num2++;
                    pathFinderNodeFast[curIndex].status = statusClosedValue2;
                    if (num3 >= num4 && flag6 && !flag8)
                    {
                        flag8 = true;
                        regionCostCalculator(__instance).Init(cellRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, disallowedCornerIndices2);
                        InitStatusesAndPushStartNode2(cellIndices2, ref curIndex, start, pathFinderNodeFast, fastPriorityQueue, ref statusOpenValue2, ref statusClosedValue2);
                        num3 = 0;
                        num2 = 0;
                    }
                }
            }
            Log.Warning(pawn + " pathing from " + start + " to " + dest + " hit search limit of " + 160000 + " cells.");
            DebugDrawRichData();
            PfProfilerEndSample();
            PfProfilerEndSample();

            __result = PawnPath.NotFound;
            return false;
        }

    }
}
