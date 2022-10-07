using System;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class RegionAndRoomQuery_Patch
    {

        static readonly Type original = typeof(RegionAndRoomQuery);
        static readonly Type patched = typeof(RegionAndRoomQuery_Patch);

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(original, patched, nameof(RoomAt));
        }

        public static void RoomAt(ref Room __result, IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_All)
        {
            if (__result == null)
                lock (map.regionAndRoomUpdater)
                {
                    __result = RegionAndRoomQuery.DistrictAt(c, map, allowedRegionTypes)?.Room;
                }
        }


    }
}