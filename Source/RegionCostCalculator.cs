using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class RegionCostCalculator_Patch
    {
        public struct RegionLinkQueueEntry2
        {
            private Region from;

            private RegionLink link;

            private int cost;

            private int estimatedPathCost;

            public Region From => from;

            public RegionLink Link => link;

            public int Cost => cost;

            public int EstimatedPathCost => estimatedPathCost;

            public RegionLinkQueueEntry2(Region from, RegionLink link, int cost, int estimatedPathCost)
            {
                this.from = from;
                this.link = link;
                this.cost = cost;
                this.estimatedPathCost = estimatedPathCost;
            }
        }

        public static FieldRef<RegionCostCalculator, Dictionary<int, RegionLink>> regionMinLink =
            FieldRefAccess<RegionCostCalculator, Dictionary<int, RegionLink>>("regionMinLink");
        public static FieldRef<RegionCostCalculator, Dictionary<RegionLink, int>> distances =
            FieldRefAccess<RegionCostCalculator, Dictionary<RegionLink, int>>("distances");
        public static FieldRef<RegionCostCalculator, Dictionary<Region, int>> minPathCosts =
            FieldRefAccess<RegionCostCalculator, Dictionary<Region, int>>("minPathCosts");
        public static FieldRef<RegionCostCalculator, List<Pair<RegionLink, int>>> preciseRegionLinkDistances =
            FieldRefAccess<RegionCostCalculator, List<Pair<RegionLink, int>>>("preciseRegionLinkDistances");
        public static FieldRef<RegionCostCalculator, bool> draftedField =
            FieldRefAccess<RegionCostCalculator, bool>("drafted");
        public static FieldRef<RegionCostCalculator, Area> allowedAreaField =
            FieldRefAccess<RegionCostCalculator, Area>("allowedArea");
        public static FieldRef<RegionCostCalculator, ByteGrid> avoidGridField =
            FieldRefAccess<RegionCostCalculator, ByteGrid>("avoidGrid");
        public static FieldRef<RegionCostCalculator, int> moveTicksDiagonalField =
            FieldRefAccess<RegionCostCalculator, int>("moveTicksDiagonal");
        public static FieldRef<RegionCostCalculator, int> moveTicksCardinalField =
            FieldRefAccess<RegionCostCalculator, int>("moveTicksCardinal");
        public static FieldRef<RegionCostCalculator, IntVec3> destinationCellField =
            FieldRefAccess<RegionCostCalculator, IntVec3>("destinationCell");
        public static FieldRef<RegionCostCalculator, TraverseParms> traverseParmsField =
            FieldRefAccess<RegionCostCalculator, TraverseParms>("traverseParms");
        public static FieldRef<RegionCostCalculator, Region[]> regionGridField =
            FieldRefAccess<RegionCostCalculator, Region[]>("regionGrid");
        public static FieldRef<RegionCostCalculator, Dictionary<RegionLink, IntVec3>> linkTargetCells =
            FieldRefAccess<RegionCostCalculator, Dictionary<RegionLink, IntVec3>>("linkTargetCells");
        public static FieldRef<RegionCostCalculator, Map> map =
            FieldRefAccess<RegionCostCalculator, Map>("map");
        public static FieldRef<RegionCostCalculator, Func<int, int, float>> preciseRegionLinkDistancesDistanceGetter =
            FieldRefAccess<RegionCostCalculator, Func<int, int, float>>("preciseRegionLinkDistancesDistanceGetter");
        /*
        public static bool Init(RegionCostCalculator __instance, CellRect destination, HashSet<Region> destRegions, TraverseParms parms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted)
        {
            regionGridField(__instance) = map(__instance).regionGrid.DirectGrid;
            traverseParmsField(__instance) = parms;
            destinationCellField(__instance) = destination.CenterCell;
            moveTicksCardinalField(__instance) = moveTicksCardinal;
            moveTicksDiagonalField(__instance) = moveTicksDiagonal;
            avoidGridField(__instance) = avoidGrid;
            allowedAreaField(__instance) = allowedArea;
            draftedField(__instance) = drafted;

            //temps?
            regionMinLink(__instance).Clear();
            distances(__instance).Clear();
            linkTargetCells(__instance).Clear();
            queue(__instance).Clear();
            minPathCosts(__instance).Clear();

            foreach (Region destRegion in destRegions)
            {
                int minPathCost = RegionMedianPathCost(destRegion);
                for (int i = 0; i < destRegion.links.Count; i++)
                {
                    RegionLink regionLink = destRegion.links[i];
                    if (!regionLink.GetOtherRegion(destRegion).Allows(traverseParmsField(__instance), isDestination: false))
                    {
                        continue;
                    }

                    int num = RegionLinkDistance(destinationCellField(__instance), regionLink, minPathCost);
                    if (distances(__instance).TryGetValue(regionLink, out int value))
                    {
                        if (num < value)
                        {
                            linkTargetCells(__instance)[regionLink] = GetLinkTargetCell(destinationCellField(__instance), regionLink);
                        }

                        num = Math.Min(value, num);
                    }
                    else
                    {
                        linkTargetCells(__instance)[regionLink] = GetLinkTargetCell(destinationCellField(__instance), regionLink);
                    }

                    distances(__instance)[regionLink] = num;
                }

                GetPreciseRegionLinkDistances(destRegion, destination, preciseRegionLinkDistances);
                for (int j = 0; j < preciseRegionLinkDistances(__instance).Count; j++)
                {
                    Pair<RegionLink, int> pair = preciseRegionLinkDistances(__instance)[j];
                    RegionLink first = pair.First;
                    int num2 = distances(__instance)[first];
                    int num3;
                    if (pair.Second > num2)
                    {
                        distances(__instance)[first] = pair.Second;
                        num3 = pair.Second;
                    }
                    else
                    {
                        num3 = num2;
                    }

                    queue(__instance).Push(new RegionLinkQueueEntry(destRegion, first, num3, num3));
                }
            }
            return false;
        }
        */
        public static bool GetPreciseRegionLinkDistances(RegionCostCalculator __instance, Region region, CellRect destination, List<Pair<RegionLink, int>> outDistances)
        {
            outDistances.Clear();            
            List<int> tmpCellIndices = new List<int>();// Replaces tmpCellIndices.Clear();
            if (destination.Width == 1 && destination.Height == 1)
            {
                tmpCellIndices.Add(map(__instance).cellIndices.CellToIndex(destination.CenterCell));// Replaces tmpCellIndices
            }
            else
            {
                foreach (IntVec3 item in destination)
                {
                    if (item.InBounds(map(__instance)))
                    {
                        tmpCellIndices.Add(map(__instance).cellIndices.CellToIndex(item));// Replaces tmpCellIndices
                    }
                }
            }
            Dictionary<int, float> tmpDistances = new Dictionary<int, float>(); //Replaces tmpDistances
            Dijkstra<int>.Run(tmpCellIndices, (int x) => PreciseRegionLinkDistancesNeighborsGetter2(__instance, x, region), 
                preciseRegionLinkDistancesDistanceGetter(__instance), tmpDistances); // Replaces tmpCellIndices

            for (int i = 0; i < region.links.Count; i++)
            {
                RegionLink regionLink = region.links[i]; //Needs catch ArgumentOutOfRange - or fix region links
                if (regionLink.GetOtherRegion(region).Allows(traverseParmsField(__instance), isDestination: false))
                {
                    if (!tmpDistances.TryGetValue(map(__instance).cellIndices.CellToIndex(
                        linkTargetCells(__instance)[regionLink]), out float value))//Replaces tmpDistances
                    {
                        Log.ErrorOnce("Dijkstra couldn't reach one of the cells even though they are in the same region. There is most likely something wrong with the neighbor nodes getter.", 1938471531);
                        value = 100f;
                    }
                    outDistances.Add(new Pair<RegionLink, int>(regionLink, (int)value));
                }
            }

            return false;
        }
        private static IEnumerable<int> PreciseRegionLinkDistancesNeighborsGetter2(RegionCostCalculator __instance, 
            int node, Region region) //Not needed if GetPreciseRegionLinkDistances is transpiled
        {
            if (regionGridField(__instance)[node] == null || regionGridField(__instance)[node] != region)
            {
                return null;
            }

            return PathableNeighborIndices2(__instance, node);
        }

        private static List<int> PathableNeighborIndices2(RegionCostCalculator __instance, int index)
        {
            List<int> tmpPathableNeighborIndices = new List<int>(); //Replaces tmpPathableNeighborIndices.Clear();
            //tmpPathableNeighborIndices.Clear();
            PathGrid pathGrid = map(__instance).pathGrid;
            int x = map(__instance).Size.x;
            bool num = index % x > 0;
            bool flag = index % x < x - 1;
            bool flag2 = index >= x;
            bool flag3 = index / x < map(__instance).Size.z - 1;
            if (flag2 && pathGrid.WalkableFast(index - x))
            {
                tmpPathableNeighborIndices.Add(index - x);//Replaces tmpPathableNeighborIndices
            }

            if (flag && pathGrid.WalkableFast(index + 1))
            {
                tmpPathableNeighborIndices.Add(index + 1);//Replaces tmpPathableNeighborIndices
            }

            if (num && pathGrid.WalkableFast(index - 1))
            {
                tmpPathableNeighborIndices.Add(index - 1);//Replaces tmpPathableNeighborIndices
            }

            if (flag3 && pathGrid.WalkableFast(index + x))
            {
                tmpPathableNeighborIndices.Add(index + x);//Replaces tmpPathableNeighborIndices
            }

            bool flag4 = !num || PathFinder.BlocksDiagonalMovement(index - 1, map(__instance));
            bool flag5 = !flag || PathFinder.BlocksDiagonalMovement(index + 1, map(__instance));
            if (flag2 && !PathFinder.BlocksDiagonalMovement(index - x, map(__instance)))
            {
                if (!flag5 && pathGrid.WalkableFast(index - x + 1))
                {
                    tmpPathableNeighborIndices.Add(index - x + 1);//Replaces tmpPathableNeighborIndices
                }

                if (!flag4 && pathGrid.WalkableFast(index - x - 1))
                {
                    tmpPathableNeighborIndices.Add(index - x - 1);//Replaces tmpPathableNeighborIndices
                }
            }

            if (flag3 && !PathFinder.BlocksDiagonalMovement(index + x, map(__instance)))
            {
                if (!flag5 && pathGrid.WalkableFast(index + x + 1))
                {
                    tmpPathableNeighborIndices.Add(index + x + 1);//Replaces tmpPathableNeighborIndices
                }

                if (!flag4 && pathGrid.WalkableFast(index + x - 1))
                {
                    tmpPathableNeighborIndices.Add(index + x - 1);//Replaces tmpPathableNeighborIndices
                }
            }

            return tmpPathableNeighborIndices;//Replaces tmpPathableNeighborIndices
        }

    }
}
