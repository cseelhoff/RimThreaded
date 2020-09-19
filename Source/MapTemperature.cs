using HarmonyLib;
using System.Collections.Generic;
using Verse;

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
            for (int index = 0; index < allRooms.Count; ++index)
            {
                RoomGroup group = allRooms[index].Group;
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
