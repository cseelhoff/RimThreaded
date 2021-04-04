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
using UnityEngine;
using System.Reflection;

namespace RimThreaded
{

    public class RegionCostCalculator_Patch
    {
        [ThreadStatic]
        private static List<int> tmpPathableNeighborIndices;
        [ThreadStatic]
        private static Dictionary<int, float> tmpDistances;
        [ThreadStatic]
        private static List<int> tmpCellIndices;

        public static FieldRef<RegionCostCalculator, IntVec3> destinationCell =
            FieldRefAccess<RegionCostCalculator, IntVec3>("destinationCell");
        public static FieldRef<RegionCostCalculator, Dictionary<int, RegionLink>> regionMinLink =
            FieldRefAccess<RegionCostCalculator, Dictionary<int, RegionLink>>("regionMinLink");
        public static FieldRef<RegionCostCalculator, TraverseParms> traverseParms =
            FieldRefAccess<RegionCostCalculator, TraverseParms>("traverseParms");
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
        
        public class DistanceComparer2 : IComparer<RegionLinkQueueEntry2>
        {
            public int Compare(RegionLinkQueueEntry2 a, RegionLinkQueueEntry2 b)
            {
                return a.EstimatedPathCost.CompareTo(b.EstimatedPathCost);
            }
        }

        public class FastPriorityQueueRegionLinkQueueEntry2
        {
            protected List<RegionLinkQueueEntry2> innerList = new List<RegionLinkQueueEntry2>();
            protected IComparer<RegionLinkQueueEntry2> comparer;
        }

        public static Dictionary<RegionCostCalculator, FastPriorityQueueRegionLinkQueueEntry2> queueDict =
            new Dictionary<RegionCostCalculator, FastPriorityQueueRegionLinkQueueEntry2>();

        static MethodInfo methodPreciseRegionLinkDistancesNeighborsGetter =
            Method(typeof(RegionCostCalculator), "PreciseRegionLinkDistancesNeighborsGetter", new Type[] { typeof(int), typeof(Region) });
        static Func<RegionCostCalculator, int, Region, IEnumerable<int>> funcPreciseRegionLinkDistancesNeighborsGetter =
            (Func<RegionCostCalculator, int, Region, IEnumerable<int>>)Delegate.CreateDelegate(typeof(Func<RegionCostCalculator, int, Region, IEnumerable<int>>), methodPreciseRegionLinkDistancesNeighborsGetter);


        public static bool GetPreciseRegionLinkDistances(RegionCostCalculator __instance, Region region, CellRect destination, List<Pair<RegionLink, int>> outDistances)
        {
            outDistances.Clear();            
            if(tmpCellIndices == null)
            {
                tmpCellIndices = new List<int>();
            }
            else
            {
                tmpCellIndices.Clear();
            }
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
            if(tmpDistances == null)
            {
                tmpDistances = new Dictionary<int, float>();
            } else
            {
                tmpDistances.Clear();
            }

            DijkstraInt.Run(tmpCellIndices, (int x) => funcPreciseRegionLinkDistancesNeighborsGetter(__instance, x, region),
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
        public static bool PathableNeighborIndices(RegionCostCalculator __instance, ref List<int> __result, int index)
        {
            if (tmpPathableNeighborIndices == null)
            {
                tmpPathableNeighborIndices = new List<int>();
            }
            else
            {
                tmpPathableNeighborIndices.Clear();
            }
            PathGrid pathGrid = map(__instance).pathGrid;
            int x = map(__instance).Size.x;
            bool num = index % x > 0;
            bool flag = index % x < x - 1;
            bool flag2 = index >= x;
            bool flag3 = index / x < map(__instance).Size.z - 1;
            if (flag2 && pathGrid.WalkableFast(index - x))
            {
                tmpPathableNeighborIndices.Add(index - x);
            }

            if (flag && pathGrid.WalkableFast(index + 1))
            {
                tmpPathableNeighborIndices.Add(index + 1);
            }

            if (num && pathGrid.WalkableFast(index - 1))
            {
                tmpPathableNeighborIndices.Add(index - 1);
            }

            if (flag3 && pathGrid.WalkableFast(index + x))
            {
                tmpPathableNeighborIndices.Add(index + x);
            }

            bool flag4 = !num || PathFinder.BlocksDiagonalMovement(index - 1, map(__instance));
            bool flag5 = !flag || PathFinder.BlocksDiagonalMovement(index + 1, map(__instance));
            if (flag2 && !PathFinder.BlocksDiagonalMovement(index - x, map(__instance)))
            {
                if (!flag5 && pathGrid.WalkableFast(index - x + 1))
                {
                    tmpPathableNeighborIndices.Add(index - x + 1);
                }

                if (!flag4 && pathGrid.WalkableFast(index - x - 1))
                {
                    tmpPathableNeighborIndices.Add(index - x - 1);
                }
            }

            if (flag3 && !PathFinder.BlocksDiagonalMovement(index + x, map(__instance)))
            {
                if (!flag5 && pathGrid.WalkableFast(index + x + 1))
                {
                    tmpPathableNeighborIndices.Add(index + x + 1);
                }

                if (!flag4 && pathGrid.WalkableFast(index + x - 1))
                {
                    tmpPathableNeighborIndices.Add(index + x - 1);
                }
            }

            __result = tmpPathableNeighborIndices;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(RegionCostCalculator);
            Type patched = typeof(RegionCostCalculator_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetPreciseRegionLinkDistances");
            RimThreadedHarmony.Prefix(original, patched, "PathableNeighborIndices");
            //RimThreadedHarmony.Prefix(original, patched, "GetRegionDistance");
            //RimThreadedHarmony.Prefix(original, patched, "Init");
        }
    }
}
