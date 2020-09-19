using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System;

namespace RimThreaded
{

    public class MapTemperature_Patch
    {
        public static AccessTools.FieldRef<MapTemperature, Map> map =
            AccessTools.FieldRefAccess<MapTemperature, Map>("map");
        
        public static bool MapTemperatureTick(MapTemperature __instance)
        {
            if (Find.TickManager.TicksGame % 120 != 7 && !DebugSettings.fastEcology)
                return false;

            HashSet<RoomGroup> fastProcessedRoomGroups = new HashSet<RoomGroup>();
            List<Room> allRooms = map(__instance).regionGrid.allRooms;
            RoomGroup group;
            for (int index = 0; index < allRooms.Count; ++index)
            {
                try {
                    group = allRooms[index].Group;
                } catch(ArgumentOutOfRangeException) { break; }
                if (!fastProcessedRoomGroups.Contains(group))
                {
                    group.TempTracker.EqualizeTemperature();
                    fastProcessedRoomGroups.Add(group);
                }
            }

            return false;
        }
    }
}
