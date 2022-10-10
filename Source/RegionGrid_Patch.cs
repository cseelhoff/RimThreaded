using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class RegionGrid_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(RegionGrid);
            Type patched = typeof(RegionGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetValidRegionAt));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetRegionAt));
            RimThreadedHarmony.Prefix(original, patched, nameof(UpdateClean));
        }

        public static bool SetRegionAt(RegionGrid __instance, IntVec3 c, Region reg)
        {
            int index = __instance.map.cellIndices.CellToIndex(c);

            if (false && Prefs.LogVerbose)
            {
                if (Log.messageCount > 900)
                    Log.Clear();
                Region old = __instance.regionGrid[index];
                string oldString = "null";
                if (old != null)
                    oldString = old.ToString();
                Log.Message("c:" + c.ToString() + " old:" + oldString + " new:" + reg.ToString());
            }

            Region oldRegion = __instance.regionGrid[index];
            if (oldRegion != null) { 
                HashSet<IntVec3> oldRegionCells = Region_Patch.GetRegionCells(oldRegion);
                oldRegionCells.Remove(c);
            }
            __instance.regionGrid[index] = reg;
            Region_Patch.GetRegionCells(reg).Add(c);

            return false;
        }

        public static bool GetValidRegionAt(RegionGrid __instance, ref Region __result, IntVec3 c)
        {
            Map map = __instance.map;
            RegionAndRoomUpdater regionAndRoomUpdater = map.regionAndRoomUpdater;
            if (!c.InBounds(map))
            {
                Log.Error("Tried to get valid region out of bounds at " + c);
                __result = null;
                return false;
            }
            if (!regionAndRoomUpdater.Enabled && regionAndRoomUpdater.AnythingToRebuild)
            {
                Log.Warning(string.Concat("Trying to get valid region at ", c, " but RegionAndRoomUpdater is disabled. The result may be incorrect."));
            }
            Region region = __instance.regionGrid[map.cellIndices.CellToIndex(c)];
            if ((region == null || !region.valid) && Find.TickManager.TicksGame - map.generationTick == 0) //credit to BlackJack
            {
                //not locking this breaks the generation of raid maps, since structure, pawn and loot generation depend on the canReachMapBorder or something in order to generate
                //Log.Message("Intitially generating region for a map, namedly: " + map.uniqueID + " Dict :"+Regen);

                lock (map.regionAndRoomUpdater)
                {
                    regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                    region = __instance.regionGrid[map.cellIndices.CellToIndex(c)];
                }

            }
            if (region != null && region.valid)
            {
                __result = region;
                return false;
            }
            __result = null;
            return false;
        }

        public static bool UpdateClean(RegionGrid __instance)
        {
            /*
            for (int i = 0; i < 16; i++)
            {
                if (__instance.curCleanIndex >= __instance.regionGrid.Length)
                {
                    __instance.curCleanIndex = 0;
                }
                Region region = __instance.regionGrid[__instance.curCleanIndex];
                if (region != null && !region.valid)
                {
                    lock (__instance.map.regionAndRoomUpdater)
                    {
                        __instance.regionGrid[__instance.curCleanIndex] = null;
                        __instance.map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                    }
                }
                __instance.curCleanIndex++;
            }
            */
            return false;
        }
    }
}
