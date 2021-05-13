using System;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RegionGrid_Patch
    {
        private static readonly FieldRef<RegionGrid, Map> mapFieldRef = FieldRefAccess<RegionGrid, Map>("map");
        private static readonly FieldRef<RegionGrid, Region[]> regionGridFieldRef = FieldRefAccess<RegionGrid, Region[]>("regionGrid");

        public static void RunDestructivePatches()
        {
            Type original = typeof(RegionGrid);
            Type patched = typeof(RegionGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetValidRegionAt");
        }

        public static bool GetValidRegionAt(RegionGrid __instance, ref Region __result, IntVec3 c)
        {
            Map map = mapFieldRef(__instance);
            if (!c.InBounds(map))
            {
                Log.Error("Tried to get valid region out of bounds at " + c);
                __result = null;
                return false;
            }
            if (!map.regionAndRoomUpdater.Enabled && map.regionAndRoomUpdater.AnythingToRebuild)
            {
                Log.Warning(string.Concat("Trying to get valid region at ", c, " but RegionAndRoomUpdater is disabled. The result may be incorrect."));
            }
            Region region = regionGridFieldRef(__instance)[map.cellIndices.CellToIndex(c)];
            if (region == null || !region.valid)
            {
                map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                region = regionGridFieldRef(__instance)[map.cellIndices.CellToIndex(c)];
            }
            if (region != null && region.valid)
            {
                __result = region;
                return false;
            }
            __result = null;
            return false;
        }
	}
}
