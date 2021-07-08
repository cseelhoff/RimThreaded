using System;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace RimThreaded
{

    public class RegionAndRoomUpdater_Patch
    {
        [ThreadStatic] public static bool working;
        [ThreadStatic] public static HashSet<Room> tmpVisitedRooms;

        static readonly Type original = typeof(RegionAndRoomUpdater);
        static readonly Type patched = typeof(RegionAndRoomUpdater_Patch);


        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, nameof(TryRebuildDirtyRegionsAndRooms));
        }

        public static HashSet<IntVec3> cellsWithNewRegions = new HashSet<IntVec3>();
        public static List<Region> regionsToReDirty = new List<Region>();
        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            if (!__instance.Enabled || working) return false;
            lock (RegionDirtyer_Patch.regionDirtyerLock)
            {
                working = true;
                if (!__instance.initialized) __instance.RebuildAllRegionsAndRooms();
                List<IntVec3> dirtyCells = RegionDirtyer_Patch.get_DirtyCells(__instance.map.regionDirtyer);
                
                if (dirtyCells.Count == 0)
                {
                    working = false;
                    return false;
                }
                try
                {
                    RegenerateNewRegionsFromDirtyCells2(__instance, dirtyCells);
                    __instance.CreateOrUpdateRooms();
                }
                catch (Exception arg) { Log.Error("Exception while rebuilding dirty regions: " + arg); }
                foreach (IntVec3 dirtyCell in dirtyCells)
                {
                    __instance.map.temperatureCache.ResetCachedCellInfo(dirtyCell);
                }
                dirtyCells.Clear();
                foreach (Region region in regionsToReDirty)
                {
                    RegionDirtyer_Patch.SetRegionDirty(region.Map.regionDirtyer, region);
                }
                regionsToReDirty.Clear();
                __instance.initialized = true;
                working = false;
                //regionCleaning.Set();
                
                if (DebugSettings.detectRegionListersBugs) Autotests_RegionListers.CheckBugs(__instance.map);
            }
            return false;
        }

        private static void RegenerateNewRegionsFromDirtyCells2(RegionAndRoomUpdater __instance, List<IntVec3> dirtyCells)
        {
            cellsWithNewRegions.Clear();
            List<Region> newRegions = __instance.newRegions;
            newRegions.Clear();
            Map localMap = __instance.map;
            //while (dirtyCells.TryDequeue(out IntVec3 dirtyCell))
            for (int index = 0; index < dirtyCells.Count; index++)
            {
                IntVec3 dirtyCell = dirtyCells[index];
                Region oldRegion = dirtyCell.GetRegion(localMap, RegionType.Set_All);
                if (oldRegion == null)
                {
                    Region region = localMap.regionMaker.TryGenerateRegionFrom(dirtyCell);
                    if (region != null)
                    {
                        newRegions.Add(region);
                    }
                }
            }
        }



    }
}