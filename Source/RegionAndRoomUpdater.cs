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

    public class RegionAndRoomUpdater_Patch
    {
        public static AccessTools.FieldRef<RegionAndRoomUpdater, HashSet<Room>> reusedOldRooms =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, HashSet<Room>>("reusedOldRooms");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<Room>> newRooms =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<Room>>("newRooms");
        private static bool ShouldBeInTheSameRoomGroup(Room a, Room b)
        {
            RegionType regionType1 = a.RegionType;
            RegionType regionType2 = b.RegionType;
            if (regionType1 != RegionType.Normal && regionType1 != RegionType.ImpassableFreeAirExchange)
                return false;
            return regionType2 == RegionType.Normal || regionType2 == RegionType.ImpassableFreeAirExchange;
        }
        public static bool FloodAndSetRoomGroups(RegionAndRoomUpdater __instance, Room start, RoomGroup roomGroup)
        {
            //this.tmpRoomStack.Clear();
            Stack<Room> tmpRoomStack = new Stack<Room>();
            tmpRoomStack.Push(start);
            //this.tmpVisitedRooms.Clear();
            HashSet<Room> tmpVisitedRooms = new HashSet<Room>();
            tmpVisitedRooms.Add(start);
            while (tmpRoomStack.Count != 0)
            {
                Room a = tmpRoomStack.Pop();
                a.Group = roomGroup;
                foreach (Room neighbor in a.Neighbors)
                {
                    if (!tmpVisitedRooms.Contains(neighbor) && ShouldBeInTheSameRoomGroup(a, neighbor))
                    {
                        tmpRoomStack.Push(neighbor);
                        tmpVisitedRooms.Add(neighbor);
                    }
                }
            }
            //this.tmpVisitedRooms.Clear();
            //this.tmpRoomStack.Clear();
            return false;
        }
        public static bool CombineNewAndReusedRoomsIntoContiguousGroups(RegionAndRoomUpdater __instance, ref int __result)
        {
            int num = 0;
            foreach (Room reusedOldRoom in reusedOldRooms(__instance))
                reusedOldRoom.newOrReusedRoomGroupIndex = -1;
            foreach (Room room in reusedOldRooms(__instance).Concat(newRooms(__instance)))
            {
                if (room.newOrReusedRoomGroupIndex < 0)
                {
                    //this.tmpRoomStack.Clear();
                    Stack<Room> tmpRoomStack = new Stack<Room>();
                    tmpRoomStack.Push(room);
                    room.newOrReusedRoomGroupIndex = num;
                    while (tmpRoomStack.Count != 0)
                    {
                        Room a = tmpRoomStack.Pop();
                        foreach (Room neighbor in a.Neighbors)
                        {
                            if (neighbor.newOrReusedRoomGroupIndex < 0 && ShouldBeInTheSameRoomGroup(a, neighbor))
                            {
                                neighbor.newOrReusedRoomGroupIndex = num;
                                tmpRoomStack.Push(neighbor);
                            }
                        }
                    }
                    //this.tmpRoomStack.Clear();
                    ++num;
                }
            }
            __result = num;
            return false;
        }

    }
}
