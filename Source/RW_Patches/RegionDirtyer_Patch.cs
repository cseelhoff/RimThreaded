using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class RegionDirtyer_Patch
    {
        [ThreadStatic] public static List<Region> regionsToDirty;
        public static Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>> dirtyCellsDict = new Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>>();
        public static object regionDirtyerLock = new object();

        internal static void InitializeThreadStatics()
        {
            regionsToDirty = new List<Region>();
        }

        public static void RunDestructivePatches()
        {
            Type original = typeof(RegionDirtyer);
            Type patched = typeof(RegionDirtyer_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAllClean));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_WalkabilityChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_ThingAffectingRegionsSpawned));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_ThingAffectingRegionsDespawned));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAllDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetRegionDirty));
        }

        public static bool SetAllClean(RegionDirtyer __instance)
        {
            lock (dirtyCellsDict)
            {
                ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
                foreach (IntVec3 dirtyCell in dirtyCells)
                {
                    __instance.map.temperatureCache.ResetCachedCellInfo(dirtyCell);
                }
                dirtyCellsDict.SetOrAdd(__instance, new ConcurrentQueue<IntVec3>());
            }
            return false;
        }

        public static ConcurrentQueue<IntVec3> get_DirtyCells(RegionDirtyer __instance)
        {
            if (!dirtyCellsDict.TryGetValue(__instance, out ConcurrentQueue<IntVec3> dirtyCells))
            {
                lock (dirtyCellsDict)
                {
                    if (!dirtyCellsDict.TryGetValue(__instance, out ConcurrentQueue<IntVec3> dirtyCells2))
                    {
                        dirtyCells2 = new ConcurrentQueue<IntVec3>();
                        dirtyCellsDict.SetOrAdd(__instance, dirtyCells2);
                    }
                    dirtyCells = dirtyCells2;
                }
            }
            return dirtyCells;
        }

        public static bool Notify_WalkabilityChanged(RegionDirtyer __instance, IntVec3 c)
        {
            regionsToDirty.Clear();
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c2 = c + GenAdj.AdjacentCellsAndInside[i];
                if (c2.InBounds(__instance.map))
                {
                    Region regionAt_NoRebuild_InvalidAllowed = __instance.map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2);
                    if (regionAt_NoRebuild_InvalidAllowed != null && regionAt_NoRebuild_InvalidAllowed.valid)
                    {
                        __instance.map.temperatureCache.TryCacheRegionTempInfo(c, regionAt_NoRebuild_InvalidAllowed);
                        regionsToDirty.Add(regionAt_NoRebuild_InvalidAllowed);
                    }
                }
            }

            for (int j = 0; j < regionsToDirty.Count; j++)
            {
                SetRegionDirty(__instance, regionsToDirty[j]);
            }

            //regionsToDirty.Clear();
            // why twice? (AdjacentCellsAndInside should include "c") 
            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
            if (c.Walkable(__instance.map))
            {
                dirtyCells.Enqueue(c);
                /*
                lock (dirtyCells)
                {
                    if (!dirtyCells.Contains(c))
                    {
                        dirtyCells.Add(c);
                    }
                }
                */
            }

            return false;
        }

        public static bool Notify_ThingAffectingRegionsSpawned(RegionDirtyer __instance, Thing b)
        {
            regionsToDirty.Clear();
            foreach (IntVec3 item in b.OccupiedRect().ExpandedBy(1).ClipInsideMap(b.Map))
            {
                Region validRegionAt_NoRebuild = b.Map.regionGrid.GetValidRegionAt_NoRebuild(item);
                if (validRegionAt_NoRebuild != null)
                {
                    b.Map.temperatureCache.TryCacheRegionTempInfo(item, validRegionAt_NoRebuild);
                    regionsToDirty.Add(validRegionAt_NoRebuild);
                }
            }

            for (int i = 0; i < regionsToDirty.Count; i++)
            {
                SetRegionDirty(__instance, regionsToDirty[i]);
            }
            return false;
        }


        public static bool Notify_ThingAffectingRegionsDespawned(RegionDirtyer __instance, Thing b)
        {
            regionsToDirty.Clear();
            Region validRegionAt_NoRebuild = __instance.map.regionGrid.GetValidRegionAt_NoRebuild(b.Position);
            if (validRegionAt_NoRebuild != null)
            {
                __instance.map.temperatureCache.TryCacheRegionTempInfo(b.Position, validRegionAt_NoRebuild);
                regionsToDirty.Add(validRegionAt_NoRebuild);
            }

            foreach (IntVec3 item2 in GenAdj.CellsAdjacent8Way(b))
            {
                if (item2.InBounds(__instance.map))
                {
                    Region validRegionAt_NoRebuild2 = __instance.map.regionGrid.GetValidRegionAt_NoRebuild(item2);
                    if (validRegionAt_NoRebuild2 != null)
                    {
                        __instance.map.temperatureCache.TryCacheRegionTempInfo(item2, validRegionAt_NoRebuild2);
                        regionsToDirty.Add(validRegionAt_NoRebuild2);
                    }
                }
            }

            for (int i = 0; i < regionsToDirty.Count; i++)
            {
                SetRegionDirty(__instance, regionsToDirty[i]);
            }

            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
            if (b.def.size.x == 1 && b.def.size.z == 1)
            {
                dirtyCells.Enqueue(b.Position);
                return false;
            }

            CellRect cellRect = b.OccupiedRect();
            for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
            {
                for (int k = cellRect.minX; k <= cellRect.maxX; k++)
                {
                    IntVec3 item = new IntVec3(k, 0, j);
                    dirtyCells.Enqueue(item);
                }
            }
            return false;
        }

        public static bool SetAllDirty(RegionDirtyer __instance)
        {
            ConcurrentQueue<IntVec3> dirtyCells = new ConcurrentQueue<IntVec3>();
            foreach (IntVec3 item in __instance.map)
            {
                dirtyCells.Enqueue(item);
            }
            lock (dirtyCellsDict)
            {
                dirtyCellsDict.SetOrAdd(__instance, dirtyCells);
            }
            foreach (Region item2 in __instance.map.regionGrid.AllRegions_NoRebuild_InvalidAllowed)
            {
                SetRegionDirty(__instance, item2, addCellsToDirtyCells: false);
            }
            return false;
        }

        public static bool SetRegionDirty(RegionDirtyer __instance, Region reg, bool addCellsToDirtyCells = true)
        {
            if (!reg.valid)
            {
                return false;
            }
            reg.valid = false;
            reg.District = null;
            List<RegionLink> links = reg.links;
            for (int i = 0; i < links.Count; i++)
            {
                links[i].Deregister(reg);
            }

            reg.links = new List<RegionLink>();
            if (!addCellsToDirtyCells)
            {
                return false;
            }
            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
            foreach (IntVec3 cell in reg.Cells)
            {
                //RegionAndRoomUpdater_Patch.cellsWithNewRegions.Remove(cell);
                dirtyCells.Enqueue(cell);
                if (DebugViewSettings.drawRegionDirties)
                {
                    __instance.map.debugDrawer.FlashCell(cell);
                }
            }
            return false;
        }
    }
}
