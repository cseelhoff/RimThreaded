using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class TemperatureCache_Patch
	{
		public static AccessTools.FieldRef<TemperatureCache, Map> map =
			AccessTools.FieldRefAccess<TemperatureCache, Map>("map");

        private static void SetCachedCellInfo2(TemperatureCache __instance, IntVec3 c, CachedTempInfo info)
        {
            __instance.tempCache[map(__instance).cellIndices.CellToIndex(c)] = info;
        }


        public static bool TryCacheRegionTempInfo(TemperatureCache __instance, IntVec3 c, Region reg)
        {
            Room room = reg.Room;
            if (room != null)
            {
                RoomGroup group = room.Group;
                if (group != null)
                {
                    SetCachedCellInfo2(__instance, c, new CachedTempInfo(group.ID, group.CellCount, group.Temperature));
                }                
            }
            return false;
        }


    }
}
