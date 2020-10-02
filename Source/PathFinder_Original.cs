#region Assembly Assembly-CSharp, Version=1.2.7558.21380, Culture=neutral, PublicKeyToken=null
// C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Verse.AI
{
    public class PathFinder_Original
    {
        internal struct CostNode
        {
            public int index;

            public int cost;

            public CostNode(int index, int cost)
            {
                this.index = index;
                this.cost = cost;
            }
        }

        private struct PathFinderNodeFast
        {
            public int knownCost;

            public int heuristicCost;

            public int parentIndex;

            public int costNodeCost;

            public ushort status;
        }

        internal class CostNodeComparer : IComparer<CostNode>
        {
            public int Compare(CostNode a, CostNode b)
            {
                return a.cost.CompareTo(b.cost);
            }
        }

        private Map map;

        private FastPriorityQueue<CostNode> openList;

        private static PathFinderNodeFast[] calcGrid;

        private static ushort statusOpenValue = 1;

        private static ushort statusClosedValue = 2;

        private RegionCostCalculatorWrapper regionCostCalculator;

        private int mapSizeX;

        private int mapSizeZ;

        private PathGrid pathGrid;

        private Building[] edificeGrid;

        private List<Blueprint>[] blueprintGrid;

        private CellIndices cellIndices;

        private List<int> disallowedCornerIndices = new List<int>(4);

        public const int DefaultMoveTicksCardinal = 13;

        private const int DefaultMoveTicksDiagonal = 18;

        private const int SearchLimit = 160000;

        private static readonly int[] Directions = new int[16]
        {
            0,
            1,
            0,
            -1,
            1,
            1,
            -1,
            -1,
            -1,
            0,
            1,
            0,
            -1,
            1,
            1,
            -1
        };

        private const int Cost_DoorToBash = 300;

        private const int Cost_BlockedWallBase = 70;

        private const float Cost_BlockedWallExtraPerHitPoint = 0.2f;

        private const int Cost_BlockedDoor = 50;

        private const float Cost_BlockedDoorPerHitPoint = 0.2f;

        public const int Cost_OutsideAllowedArea = 600;

        private const int Cost_PawnCollision = 175;

        private const int NodesToOpenBeforeRegionBasedPathing_NonColonist = 2000;

        private const int NodesToOpenBeforeRegionBasedPathing_Colonist = 100000;

        private const float NonRegionBasedHeuristicStrengthAnimal = 1.75f;

        private static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve = new SimpleCurve
        {
            new CurvePoint(40f, 1f),
            new CurvePoint(120f, 2.8f)
        };

        private static readonly SimpleCurve RegionHeuristicWeightByNodesOpened = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(3500f, 1f),
            new CurvePoint(4500f, 5f),
            new CurvePoint(30000f, 50f),
            new CurvePoint(100000f, 500f)
        };

        public PathFinder_Original(Map map)
        {
            this.map = map;
            mapSizeX = map.Size.x;
            mapSizeZ = map.Size.z;
            int num = mapSizeX * mapSizeZ;
            if (calcGrid == null || calcGrid.Length < num)
            {
                calcGrid = new PathFinderNodeFast[num];
            }

            openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
            regionCostCalculator = new RegionCostCalculatorWrapper(map);
        }

        public PawnPath FindPath(IntVec3 start, LocalTargetInfo dest, Pawn pawn, PathEndMode peMode = PathEndMode.OnCell)
        {
            bool canBash = false;
            if (pawn != null && pawn.CurJob != null && pawn.CurJob.canBash)
            {
                canBash = true;
            }

            return FindPath(start, dest, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, canBash), peMode);
        }

        public PawnPath FindPath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            if (DebugSettings.pathThroughWalls)
            {
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            }

            Pawn pawn = traverseParms.pawn;
            if (pawn != null && pawn.Map != map)
            {
                Log.Error("Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=" + pawn + " pawn.Map=" + pawn.Map + " map=" + map);
                return PawnPath.NotFound;
            }

            if (!start.IsValid)
            {
                Log.Error("Tried to FindPath with invalid start " + start + ", pawn= " + pawn);
                return PawnPath.NotFound;
            }

            if (!dest.IsValid)
            {
                Log.Error("Tried to FindPath with invalid dest " + dest + ", pawn= " + pawn);
                return PawnPath.NotFound;
            }

            if (traverseParms.mode == TraverseMode.ByPawn)
            {
                if (!pawn.CanReach(dest, peMode, Danger.Deadly, traverseParms.canBash, traverseParms.mode))
                {
                    return PawnPath.NotFound;
                }
            }
            else if (!map.reachability.CanReach(start, dest, peMode, traverseParms))
            {
                return PawnPath.NotFound;
            }

            PfProfilerBeginSample("FindPath for " + pawn + " from " + start + " to " + dest + (dest.HasThing ? (" at " + dest.Cell) : ""));
            cellIndices = map.cellIndices;
            pathGrid = map.pathGrid;
            this.edificeGrid = map.edificeGrid.InnerArray;
            blueprintGrid = map.blueprintGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int curIndex = cellIndices.CellToIndex(start);
            int num = cellIndices.CellToIndex(dest.Cell);
            ByteGrid byteGrid = pawn?.GetAvoidGrid();
            bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag3 = !flag;
            CellRect cellRect = CalculateDestinationRect(dest, peMode);
            bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
            int[] array = map.pathGrid.pathGrid;
            TerrainDef[] topGrid = map.terrainGrid.topGrid;
            EdificeGrid edificeGrid = map.edificeGrid;
            int num2 = 0;
            int num3 = 0;
            Area allowedArea = GetAllowedArea(pawn);
            bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = (!flag && start.GetRegion(map) != null) & flag2;
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

            CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
            InitStatusesAndPushStartNode(ref curIndex, start);
            while (true)
            {
                PfProfilerBeginSample("Open cell");
                if (openList.Count <= 0)
                {
                    string text = (pawn != null && pawn.CurJob != null) ? pawn.CurJob.ToString() : "null";
                    string text2 = (pawn != null && pawn.Faction != null) ? pawn.Faction.ToString() : "null";
                    Log.Warning(pawn + " pathing from " + start + " to " + dest + " ran out of cells to process.\nJob:" + text + "\nFaction: " + text2);
                    DebugDrawRichData();
                    PfProfilerEndSample();
                    PfProfilerEndSample();
                    return PawnPath.NotFound;
                }

                num5 += openList.Count;
                num6++;
                CostNode costNode = openList.Pop();
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

                IntVec3 c = cellIndices.IndexToCell(curIndex);
                int x2 = c.x;
                int z2 = c.z;
                if (flag4)
                {
                    if (curIndex == num)
                    {
                        PfProfilerEndSample();
                        PawnPath result = FinalizedPath(curIndex, flag8);
                        PfProfilerEndSample();
                        return result;
                    }
                }
                else if (cellRect.Contains(c) && !disallowedCornerIndices.Contains(curIndex))
                {
                    PfProfilerEndSample();
                    PawnPath result2 = FinalizedPath(curIndex, flag8);
                    PfProfilerEndSample();
                    return result2;
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
                    if (num10 >= mapSizeX || num11 >= mapSizeZ)
                    {
                        continue;
                    }

                    int num12 = (int)num10;
                    int num13 = (int)num11;
                    int num14 = cellIndices.CellToIndex(num12, num13);
                    if (calcGrid[num14].status == statusClosedValue && !flag8)
                    {
                        continue;
                    }

                    int num15 = 0;
                    bool flag10 = false;
                    if (!flag2 && new IntVec3(num12, 0, num13).GetTerrain(map).HasTag("Water"))
                    {
                        continue;
                    }

                    if (!pathGrid.WalkableFast(num14))
                    {
                        if (!flag)
                        {
                            continue;
                        }

                        flag10 = true;
                        num15 += 70;
                        Building building = edificeGrid[num14];
                        if (building == null || !IsDestroyable(building))
                        {
                            continue;
                        }

                        num15 += (int)((float)building.HitPoints * 0.2f);
                    }

                    switch (i)
                    {
                        case 4:
                            if (BlocksDiagonalMovement(curIndex - mapSizeX))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (BlocksDiagonalMovement(curIndex + 1))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 5:
                            if (BlocksDiagonalMovement(curIndex + mapSizeX))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (BlocksDiagonalMovement(curIndex + 1))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 6:
                            if (BlocksDiagonalMovement(curIndex + mapSizeX))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (BlocksDiagonalMovement(curIndex - 1))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            break;
                        case 7:
                            if (BlocksDiagonalMovement(curIndex - mapSizeX))
                            {
                                if (flag7)
                                {
                                    continue;
                                }

                                num15 += 70;
                            }

                            if (BlocksDiagonalMovement(curIndex - 1))
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

                    Building building2 = this.edificeGrid[num14];
                    if (building2 != null)
                    {
                        PfProfilerBeginSample("Edifices");
                        int buildingCost = GetBuildingCost(building2, traverseParms, pawn);
                        if (buildingCost == int.MaxValue)
                        {
                            PfProfilerEndSample();
                            continue;
                        }

                        num16 += buildingCost;
                        PfProfilerEndSample();
                    }

                    List<Blueprint> list = blueprintGrid[num14];
                    if (list != null)
                    {
                        PfProfilerBeginSample("Blueprints");
                        int num17 = 0;
                        for (int j = 0; j < list.Count; j++)
                        {
                            num17 = Mathf.Max(num17, GetBlueprintCost(list[j], pawn));
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
                            Log.ErrorOnce("Heuristic cost overflow for " + pawn.ToStringSafe() + " pathing from " + start + " to " + dest + ".", pawn.GetHashCode() ^ 0xB8DC389);
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
                        Log.ErrorOnce("Node cost overflow for " + pawn.ToStringSafe() + " pathing from " + start + " to " + dest + ".", pawn.GetHashCode() ^ 0x53CB9DE);
                        num21 = 0;
                    }

                    calcGrid[num14].parentIndex = curIndex;
                    calcGrid[num14].knownCost = num18;
                    calcGrid[num14].status = statusOpenValue;
                    calcGrid[num14].costNodeCost = num21;
                    num3++;
                    openList.Push(new CostNode(num14, num21));
                }

                PfProfilerEndSample();
                num2++;
                calcGrid[curIndex].status = statusClosedValue;
                if (num3 >= num4 && flag6 && !flag8)
                {
                    flag8 = true;
                    regionCostCalculator.Init(cellRect, traverseParms, num8, num9, byteGrid, allowedArea, flag9, disallowedCornerIndices);
                    InitStatusesAndPushStartNode(ref curIndex, start);
                    num3 = 0;
                    num2 = 0;
                }
            }

            Log.Warning(pawn + " pathing from " + start + " to " + dest + " hit search limit of " + 160000 + " cells.");
            DebugDrawRichData();
            PfProfilerEndSample();
            PfProfilerEndSample();
            return PawnPath.NotFound;
        }

        public static int GetBuildingCost(Building b, TraverseParms traverseParms, Pawn pawn)
        {
            Building_Door building_Door = b as Building_Door;
            if (building_Door != null)
            {
                switch (traverseParms.mode)
                {
                    case TraverseMode.NoPassClosedDoors:
                    case TraverseMode.NoPassClosedDoorsOrWater:
                        if (building_Door.FreePassage)
                        {
                            return 0;
                        }

                        return int.MaxValue;
                    case TraverseMode.PassAllDestroyableThings:
                    case TraverseMode.PassAllDestroyableThingsNotWater:
                        if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }

                        if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
                        {
                            return 0;
                        }

                        return 50 + (int)((float)building_Door.HitPoints * 0.2f);
                    case TraverseMode.PassDoors:
                        if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }

                        if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
                        {
                            return 0;
                        }

                        return 150;
                    case TraverseMode.ByPawn:
                        if (!traverseParms.canBash && building_Door.IsForbiddenToPass(pawn))
                        {
                            return int.MaxValue;
                        }

                        if (building_Door.PawnCanOpen(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }

                        if (building_Door.CanPhysicallyPass(pawn))
                        {
                            return 0;
                        }

                        if (traverseParms.canBash)
                        {
                            return 300;
                        }

                        return int.MaxValue;
                }
            }
            else if (pawn != null)
            {
                return b.PathFindCostFor(pawn);
            }

            return 0;
        }

        public static int GetBlueprintCost(Blueprint b, Pawn pawn)
        {
            if (pawn != null)
            {
                return b.PathFindCostFor(pawn);
            }

            return 0;
        }

        public static bool IsDestroyable(Thing th)
        {
            if (th.def.useHitPoints)
            {
                return th.def.destroyable;
            }

            return false;
        }

        private bool BlocksDiagonalMovement(int x, int z)
        {
            return BlocksDiagonalMovement(x, z, map);
        }

        private bool BlocksDiagonalMovement(int index)
        {
            return BlocksDiagonalMovement(index, map);
        }

        public static bool BlocksDiagonalMovement(int x, int z, Map map)
        {
            return BlocksDiagonalMovement(map.cellIndices.CellToIndex(x, z), map);
        }

        public static bool BlocksDiagonalMovement(int index, Map map)
        {
            if (!map.pathGrid.WalkableFast(index))
            {
                return true;
            }

            if (map.edificeGrid[index] is Building_Door)
            {
                return true;
            }

            return false;
        }

        private void DebugFlash(IntVec3 c, float colorPct, string str)
        {
            DebugFlash(c, map, colorPct, str);
        }

        private static void DebugFlash(IntVec3 c, Map map, float colorPct, string str)
        {
            map.debugDrawer.FlashCell(c, colorPct, str);
        }

        private PawnPath FinalizedPath(int finalIndex, bool usedRegionHeuristics)
        {
            PawnPath emptyPawnPath = map.pawnPathPool.GetEmptyPawnPath();
            int num = finalIndex;
            while (true)
            {
                int parentIndex = calcGrid[num].parentIndex;
                emptyPawnPath.AddNode(map.cellIndices.IndexToCell(num));
                if (num == parentIndex)
                {
                    break;
                }

                num = parentIndex;
            }

            emptyPawnPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
            return emptyPawnPath;
        }

        private void InitStatusesAndPushStartNode(ref int curIndex, IntVec3 start)
        {
            statusOpenValue += 2;
            statusClosedValue += 2;
            if (statusClosedValue >= 65435)
            {
                ResetStatuses();
            }

            curIndex = cellIndices.CellToIndex(start);
            calcGrid[curIndex].knownCost = 0;
            calcGrid[curIndex].heuristicCost = 0;
            calcGrid[curIndex].costNodeCost = 0;
            calcGrid[curIndex].parentIndex = curIndex;
            calcGrid[curIndex].status = statusOpenValue;
            openList.Clear();
            openList.Push(new CostNode(curIndex, 0));
        }

        private void ResetStatuses()
        {
            int num = calcGrid.Length;
            for (int i = 0; i < num; i++)
            {
                calcGrid[i].status = 0;
            }

            statusOpenValue = 1;
            statusClosedValue = 2;
        }

        [Conditional("PFPROFILE")]
        private void PfProfilerBeginSample(string s)
        {
        }

        [Conditional("PFPROFILE")]
        private void PfProfilerEndSample()
        {
        }

        private void DebugDrawRichData()
        {
        }

        private float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
        {
            if (pawn != null && pawn.RaceProps.Animal)
            {
                return 1.75f;
            }

            float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
            return Mathf.RoundToInt(NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
        }

        private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
        {
            CellRect result = (dest.HasThing && peMode != PathEndMode.OnCell) ? dest.Thing.OccupiedRect() : CellRect.SingleCell(dest.Cell);
            if (peMode == PathEndMode.Touch)
            {
                result = result.ExpandedBy(1);
            }

            return result;
        }

        private Area GetAllowedArea(Pawn pawn)
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

        private void CalculateAndAddDisallowedCorners(TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
        {
            disallowedCornerIndices.Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, minZ));
                }

                if (!IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, maxZ));
                }

                if (!IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, maxZ));
                }

                if (!IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                {
                    disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, minZ));
                }
            }
        }

        private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, map);
        }
    }
}
#if false // Decompilation log
'18' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll'
------------------
Resolve: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'
------------------
Resolve: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AudioModule.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Core.dll'
------------------
Resolve: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'
------------------
Resolve: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.dll'
------------------
Resolve: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.Linq.dll'
------------------
Resolve: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AssetBundleModule.dll'
------------------
Resolve: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'
------------------
Resolve: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
#endif
