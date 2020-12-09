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

    public class RegionCostCalculatorWrapper_Patch
    {
        public static FieldRef<RegionCostCalculatorWrapper, Map> map =
            FieldRefAccess<RegionCostCalculatorWrapper, Map>("map");
        public static FieldRef<RegionCostCalculatorWrapper, IntVec3> endCell =
            FieldRefAccess<RegionCostCalculatorWrapper, IntVec3>("endCell");
        public static FieldRef<RegionCostCalculatorWrapper, HashSet<Region>> destRegions =
            FieldRefAccess<RegionCostCalculatorWrapper, HashSet<Region>>("destRegions");
        public static FieldRef<RegionCostCalculatorWrapper, RegionCostCalculator> regionCostCalculator =
            FieldRefAccess<RegionCostCalculatorWrapper, RegionCostCalculator>("regionCostCalculator");
        public static FieldRef<RegionCostCalculatorWrapper, int> moveTicksCardinalField =
            FieldRefAccess<RegionCostCalculatorWrapper, int>("moveTicksCardinal");
        public static FieldRef<RegionCostCalculatorWrapper, int> moveTicksDiagonalField =
            FieldRefAccess<RegionCostCalculatorWrapper, int>("moveTicksDiagonal");
        public static FieldRef<RegionCostCalculatorWrapper, Region> cachedRegion =
            FieldRefAccess<RegionCostCalculatorWrapper, Region>("cachedRegion");
        public static FieldRef<RegionCostCalculatorWrapper, RegionLink> cachedBestLink =
            FieldRefAccess<RegionCostCalculatorWrapper, RegionLink>("cachedBestLink");
        public static FieldRef<RegionCostCalculatorWrapper, RegionLink> cachedSecondBestLink =
            FieldRefAccess<RegionCostCalculatorWrapper, RegionLink>("cachedSecondBestLink");
        public static FieldRef<RegionCostCalculatorWrapper, int> cachedBestLinkCost =
            FieldRefAccess<RegionCostCalculatorWrapper, int>("cachedBestLinkCost");
        public static FieldRef<RegionCostCalculatorWrapper, int> cachedSecondBestLinkCost =
            FieldRefAccess<RegionCostCalculatorWrapper, int>("cachedSecondBestLinkCost");
        public static FieldRef<RegionCostCalculatorWrapper, bool> cachedRegionIsDestination =
            FieldRefAccess<RegionCostCalculatorWrapper, bool>("cachedRegionIsDestination");
        public static FieldRef<RegionCostCalculatorWrapper, Region[]> regionGrid =
            FieldRefAccess<RegionCostCalculatorWrapper, Region[]>("regionGrid");
        public static bool Init(RegionCostCalculatorWrapper __instance, CellRect end, TraverseParms traverseParms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted, List<int> disallowedCorners)
        {
            moveTicksCardinalField(__instance) = moveTicksCardinal;
            moveTicksDiagonalField(__instance) = moveTicksDiagonal;
            endCell(__instance) = end.CenterCell;
            cachedRegion(__instance) = null;
            cachedBestLink(__instance) = null;
            cachedSecondBestLink(__instance) = null;
            cachedBestLinkCost(__instance) = 0;
            cachedSecondBestLinkCost(__instance) = 0;
            cachedRegionIsDestination(__instance) = false;
            Map map1 = map(__instance);
            RegionGrid regionGrid1 = map1.regionGrid;
            if (regionGrid1 == null)
            {
                Log.Error("regionGrid is null for map: " + map1.ToString() );
                return false;
            }
            regionGrid(__instance) = regionGrid1.DirectGrid;
            destRegions(__instance).Clear();
            if (end.Width == 1 && end.Height == 1)
            {
                Region region = endCell(__instance).GetRegion(map(__instance));
                if (region != null)
                {
                    destRegions(__instance).Add(region);
                }
            }
            else
            {
                foreach (IntVec3 item in end)
                {
                    if (item.InBounds(map(__instance)) && !disallowedCorners.Contains(map(__instance).cellIndices.CellToIndex(item)))
                    {
                        Region region2 = item.GetRegion(map(__instance));
                        if (region2 != null && region2.Allows(traverseParms, isDestination: true))
                        {
                            destRegions(__instance).Add(region2);
                        }
                    }
                }
            }

            if (destRegions(__instance).Count == 0)
            {
                Log.Error("Couldn't find any destination regions. This shouldn't ever happen because we've checked reachability.");
            }

            regionCostCalculator(__instance).Init(end, destRegions(__instance), traverseParms, moveTicksCardinal, moveTicksDiagonal, avoidGrid, allowedArea, drafted);
            
            return false;
        }
        public static bool Init2(RegionCostCalculatorWrapper __instance, CellRect end, TraverseParms traverseParms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted, List<int> disallowedCorners)
    {
            moveTicksCardinalField(__instance) = moveTicksCardinal;
            moveTicksDiagonalField(__instance) = moveTicksDiagonal;
            endCell(__instance) = end.CenterCell;
            cachedRegion(__instance) = null;
            cachedBestLink(__instance) = null;
            cachedSecondBestLink(__instance) = null;
            cachedBestLinkCost(__instance) = 0;
            cachedSecondBestLinkCost(__instance) = 0;
            cachedRegionIsDestination(__instance) = false;
            regionGrid(__instance) = map(__instance).regionGrid.DirectGrid;
            destRegions(__instance).Clear();
            if (end.Width == 1 && end.Height == 1)
            {
                Region region = endCell(__instance).GetRegion(map(__instance));
                if (region != null)
                {
                    destRegions(__instance).Add(region);
                }
            }
            else
            {
                foreach (IntVec3 item in end)
                {
                    if (item.InBounds(map(__instance)) && !disallowedCorners.Contains(map(__instance).cellIndices.CellToIndex(item)))
                    {
                        Region region2 = item.GetRegion(map(__instance));
                        if (region2 != null && region2.Allows(traverseParms, isDestination: true))
                        {
                            destRegions(__instance).Add(region2);
                        }
                    }
                }
            }

            if (destRegions(__instance).Count == 0)
            {
                Log.Error("Couldn't find any destination regions. This shouldn't ever happen because we've checked reachability.");
            }

            regionCostCalculator(__instance).Init(end, destRegions(__instance), traverseParms, moveTicksCardinal, 
                moveTicksDiagonal, avoidGrid, allowedArea, drafted);
            
            return false;
        }



    }
}
