using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class RegionDirtyer_Patch
    {
        public static Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>> dirtyCellsDict = new Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>>();

        public static FieldRef<RegionDirtyer, Map> map = FieldRefAccess<RegionDirtyer, Map>("map");
        public static bool SetAllClean(RegionDirtyer __instance)
        {
            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);            
            while (dirtyCells.TryDequeue(out IntVec3 dirtyCell))
            {
                map(__instance).temperatureCache.ResetCachedCellInfo(dirtyCell);
            }

            //dirtyCells.Clear();
            return false;
        }

        public static ConcurrentQueue<IntVec3> get_DirtyCells(RegionDirtyer __instance)
        {
            if(!dirtyCellsDict.TryGetValue(__instance, out ConcurrentQueue<IntVec3> dirtyCells))
            {
                dirtyCells = new ConcurrentQueue<IntVec3>();
                lock(dirtyCellsDict)
                {
                    dirtyCellsDict.SetOrAdd(__instance, dirtyCells);
                }
            }
            return dirtyCells;
        }

        public static bool Notify_WalkabilityChanged(RegionDirtyer __instance, IntVec3 c)
        {
            List<Region> regionsToDirty = new List<Region>();
            //regionsToDirty.Clear();
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c2 = c + GenAdj.AdjacentCellsAndInside[i];
                if (c2.InBounds(map(__instance)))
                {
                    Region regionAt_NoRebuild_InvalidAllowed = map(__instance).regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2);
                    if (regionAt_NoRebuild_InvalidAllowed != null && regionAt_NoRebuild_InvalidAllowed.valid)
                    {
                        map(__instance).temperatureCache.TryCacheRegionTempInfo(c, regionAt_NoRebuild_InvalidAllowed);
                        regionsToDirty.Add(regionAt_NoRebuild_InvalidAllowed);
                    }
                }
            }

            for (int j = 0; j < regionsToDirty.Count; j++)
            {
                SetRegionDirty(__instance, regionsToDirty[j]);
            }

            //regionsToDirty.Clear();
            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
            if (c.Walkable(map(__instance)) && !dirtyCells.Contains(c))
            {
                dirtyCells.Enqueue(c);
            }
            return false;
        }

        public static bool Notify_ThingAffectingRegionsSpawned(RegionDirtyer __instance, Thing b)
        {
            //regionsToDirty.Clear();
            List<Region> regionsToDirty = new List<Region>();
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

            //regionsToDirty.Clear();
            return false;
        }


        public static bool Notify_ThingAffectingRegionsDespawned(RegionDirtyer __instance, Thing b)
        {
            //regionsToDirty.Clear();
            List<Region> regionsToDirty = new List<Region>();
            Region validRegionAt_NoRebuild = map(__instance).regionGrid.GetValidRegionAt_NoRebuild(b.Position);
            if (validRegionAt_NoRebuild != null)
            {
                map(__instance).temperatureCache.TryCacheRegionTempInfo(b.Position, validRegionAt_NoRebuild);
                regionsToDirty.Add(validRegionAt_NoRebuild);
            }

            foreach (IntVec3 item2 in GenAdj.CellsAdjacent8Way(b))
            {
                if (item2.InBounds(map(__instance)))
                {
                    Region validRegionAt_NoRebuild2 = map(__instance).regionGrid.GetValidRegionAt_NoRebuild(item2);
                    if (validRegionAt_NoRebuild2 != null)
                    {
                        map(__instance).temperatureCache.TryCacheRegionTempInfo(item2, validRegionAt_NoRebuild2);
                        regionsToDirty.Add(validRegionAt_NoRebuild2);
                    }
                }
            }

            for (int i = 0; i < regionsToDirty.Count; i++)
            {
                SetRegionDirty(__instance, regionsToDirty[i]);
            }

            regionsToDirty.Clear();
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
            foreach (IntVec3 item in map(__instance))
            {
                dirtyCells.Enqueue(item);
            }
            dirtyCellsDict.SetOrAdd(__instance, dirtyCells);
            foreach (Region item2 in map(__instance).regionGrid.AllRegions_NoRebuild_InvalidAllowed)
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
            reg.Room = null;
            for (int i = 0; i < reg.links.Count; i++)
            {
                reg.links[i].Deregister(reg);
            }

            reg.links.Clear();
            if (!addCellsToDirtyCells)
            {
                return false;
            }
            ConcurrentQueue<IntVec3> dirtyCells = get_DirtyCells(__instance);
            foreach (IntVec3 cell in reg.Cells)
            {                
                dirtyCells.Enqueue(cell);
                if (DebugViewSettings.drawRegionDirties)
                {
                    map(__instance).debugDrawer.FlashCell(cell);
                }
            }
            return false;
        }

    }
}
