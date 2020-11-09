using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Reflection;
using System.Threading;

namespace RimThreaded
{

    public class RegionAndRoomUpdater_Patch
    {

        public static AccessTools.FieldRef<RegionDirtyer, Map> maprd =
            AccessTools.FieldRefAccess<RegionDirtyer, Map>("map");
        public static void SetAllClean2(RegionDirtyer __instance)
        {
            List<IntVec3> dirtyCells = __instance.DirtyCells;
            for (int i = 0; i < dirtyCells.Count; i++)
            {
                IntVec3 dirtyCell;
                try
                {
                    dirtyCell = dirtyCells[i];
                }
                catch(ArgumentOutOfRangeException) { break;  }
                    
                maprd(__instance).temperatureCache.ResetCachedCellInfo(dirtyCell);
            }

            dirtyCells.Clear();
        }

        public static AccessTools.FieldRef<RegionAndRoomUpdater, Map> map =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, Map>("map");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<Region>> newRegions =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<Region>>("newRegions");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<Room>> newRooms =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<Room>>("newRooms");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, HashSet<Room>> reusedOldRooms =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, HashSet<Room>>("reusedOldRooms");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<RoomGroup>> newRoomGroups =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<RoomGroup>>("newRoomGroups");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, HashSet<RoomGroup>> reusedOldRoomGroups =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, HashSet<RoomGroup>>("reusedOldRoomGroups");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<Region>> currentRegionGroup =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<Region>>("currentRegionGroup");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, List<Room>> currentRoomGroup =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, List<Room>>("currentRoomGroup");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, bool> initialized =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, bool>("initialized");
        public static AccessTools.FieldRef<RegionAndRoomUpdater, bool> working =
            AccessTools.FieldRefAccess<RegionAndRoomUpdater, bool>("working");

        public static MethodInfo RegenerateNewRegionsFromDirtyCells =
            typeof(RegionAndRoomUpdater).GetMethod("RegenerateNewRegionsFromDirtyCells", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo CreateOrUpdateRooms =
            typeof(RegionAndRoomUpdater).GetMethod("CreateOrUpdateRooms", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo SetAllClean =
            typeof(RegionDirtyer).GetMethod("SetAllClean", BindingFlags.NonPublic | BindingFlags.Instance);


        public static object workingLock = new object();
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
            Stack<Room> tmpRoomStack = new Stack<Room>();
            foreach (Room room in reusedOldRooms(__instance).Concat(newRooms(__instance)))
            {
                if (room.newOrReusedRoomGroupIndex < 0)
                {
                    tmpRoomStack.Clear();
                    tmpRoomStack.Push(room);
                    room.newOrReusedRoomGroupIndex = num;
                    while (tmpRoomStack.Count != 0)
                    {
                        Room a = tmpRoomStack.Pop();
                        foreach (Room neighbor in a.Neighbors)
                        {
                            if (neighbor != null)
                            {
                                if (neighbor.newOrReusedRoomGroupIndex < 0 && ShouldBeInTheSameRoomGroup(a, neighbor))
                                {
                                    neighbor.newOrReusedRoomGroupIndex = num;
                                    tmpRoomStack.Push(neighbor);
                                }
                            }
                        }
                    }
                    //tmpRoomStack.Clear();
                    ++num;
                }
            }
            __result = num;
            return false;
        }
        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            //todo: optimize lock speedup fix

            //lock (workingLock)
            //{
                if (working(__instance) || !__instance.Enabled)
                {
                    return false;
                }
                working(__instance) = true;
                if (!initialized(__instance))
                {
                    __instance.RebuildAllRegionsAndRooms();
                }

                if (!map(__instance).regionDirtyer.AnyDirty)
                {
                    working(__instance) = false;
                    return false;
                }

                try
                {
                    RegenerateNewRegionsFromDirtyCells2(__instance);
                    //RegenerateNewRegionsFromDirtyCells.Invoke(__instance, new object[] { });
                    CreateOrUpdateRooms2(__instance);
                    //CreateOrUpdateRooms.Invoke(__instance, new object[] { });
                }
                catch (Exception arg)
                {
                    Log.Error("Exception while rebuilding dirty regions: " + arg);
                }

                newRegions(__instance).Clear();
                SetAllClean2(map(__instance).regionDirtyer);
                //SetAllClean.Invoke(map(__instance).regionDirtyer, new object[] { });
                initialized(__instance) = true;
                working(__instance) = false;

            //}
            if (DebugSettings.detectRegionListersBugs)
            {
                Autotests_RegionListers.CheckBugs(map(__instance));
            }
            return false;
        }






        private static void RegenerateNewRegionsFromDirtyCells2(RegionAndRoomUpdater __instance)
        {
            newRegions(__instance).Clear();
            List<IntVec3> dirtyCells = map(__instance).regionDirtyer.DirtyCells;
            for (int i = 0; i < dirtyCells.Count; i++)
            {
                IntVec3 intVec = dirtyCells[i];
                if (intVec.GetRegion(map(__instance), RegionType.Set_All) == null)
                {
                    Region region = map(__instance).regionMaker.TryGenerateRegionFrom(intVec);
                    if (region != null)
                    {
                        newRegions(__instance).Add(region);
                    }
                }
            }
        }

        private static int CombineNewAndReusedRoomsIntoContiguousGroups2(RegionAndRoomUpdater __instance)
        {
            int num = 0;
            foreach (Room reusedOldRoom in reusedOldRooms(__instance))
            {
                reusedOldRoom.newOrReusedRoomGroupIndex = -1;
            }
            Stack<Room> tmpRoomStack = new Stack<Room>();
            foreach (Room item in reusedOldRooms(__instance).Concat(newRooms(__instance)))
            {
                if (item.newOrReusedRoomGroupIndex < 0)
                {
                    tmpRoomStack.Clear();
                    tmpRoomStack.Push(item);
                    item.newOrReusedRoomGroupIndex = num;
                    while (tmpRoomStack.Count != 0)
                    {
                        Room room = tmpRoomStack.Pop();
                        foreach (Room neighbor in room.Neighbors)
                        {
                            if (neighbor.newOrReusedRoomGroupIndex < 0 && ShouldBeInTheSameRoomGroup(room, neighbor))
                            {
                                neighbor.newOrReusedRoomGroupIndex = num;
                                tmpRoomStack.Push(neighbor);
                            }
                        }
                    }

                    tmpRoomStack.Clear();
                    num++;
                }
            }

            return num;
        }
        private static int CombineNewRegionsIntoContiguousGroups2(RegionAndRoomUpdater __instance)
        {
            int num = 0;
            for (int i = 0; i < newRegions(__instance).Count; i++)
            {
                if (newRegions(__instance)[i].newRegionGroupIndex < 0)
                {
                    RegionTraverser.FloodAndSetNewRegionIndex(newRegions(__instance)[i], num);
                    num++;
                }
            }

            return num;
        }

        private static void CreateOrAttachToExistingRooms2(RegionAndRoomUpdater __instance, int numRegionGroups)
        {
            for (int i = 0; i < numRegionGroups; i++)
            {
                currentRegionGroup(__instance).Clear();
                for (int j = 0; j < newRegions(__instance).Count; j++)
                {
                    if (newRegions(__instance)[j].newRegionGroupIndex == i)
                    {
                        currentRegionGroup(__instance).Add(newRegions(__instance)[j]);
                    }
                }

                if (!currentRegionGroup(__instance)[0].type.AllowsMultipleRegionsPerRoom())
                {
                    if (currentRegionGroup(__instance).Count != 1)
                    {
                        Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
                    }

                    Room room = Room.MakeNew(map(__instance));
                    currentRegionGroup(__instance)[0].Room = room;
                    newRooms(__instance).Add(room);
                    continue;
                }

                bool multipleOldNeighborRooms;
                Room room2 = FindCurrentRegionGroupNeighborWithMostRegions2(__instance, out multipleOldNeighborRooms);
                if (room2 == null)
                {
                    Room item = RegionTraverser.FloodAndSetRooms(currentRegionGroup(__instance)[0], map(__instance), null);
                    newRooms(__instance).Add(item);
                }
                else if (!multipleOldNeighborRooms)
                {
                    for (int k = 0; k < currentRegionGroup(__instance).Count; k++)
                    {
                        currentRegionGroup(__instance)[k].Room = room2;
                    }

                    reusedOldRooms(__instance).Add(room2);
                }
                else
                {
                    RegionTraverser.FloodAndSetRooms(currentRegionGroup(__instance)[0], map(__instance), room2);
                    reusedOldRooms(__instance).Add(room2);
                }
            }
        }

        private static void CreateOrAttachToExistingRoomGroups2(RegionAndRoomUpdater __instance, int numRoomGroups)
        {
            for (int i = 0; i < numRoomGroups; i++)
            {
                currentRoomGroup(__instance).Clear();
                foreach (Room reusedOldRoom in reusedOldRooms(__instance))
                {
                    if (reusedOldRoom.newOrReusedRoomGroupIndex == i)
                    {
                        currentRoomGroup(__instance).Add(reusedOldRoom);
                    }
                }

                for (int j = 0; j < newRooms(__instance).Count; j++)
                {
                    if (newRooms(__instance)[j].newOrReusedRoomGroupIndex == i)
                    {
                        currentRoomGroup(__instance).Add(newRooms(__instance)[j]);
                    }
                }

                bool multipleOldNeighborRoomGroups;
                RoomGroup roomGroup = FindCurrentRoomGroupNeighborWithMostRegions2(__instance, out multipleOldNeighborRoomGroups);
                if (roomGroup == null)
                {
                    RoomGroup roomGroup2 = RoomGroup.MakeNew(map(__instance));
                    FloodAndSetRoomGroups2(__instance, currentRoomGroup(__instance)[0], roomGroup2);
                    newRoomGroups(__instance).Add(roomGroup2);
                }
                else if (!multipleOldNeighborRoomGroups)
                {
                    for (int k = 0; k < currentRoomGroup(__instance).Count; k++)
                    {
                        currentRoomGroup(__instance)[k].Group = roomGroup;
                    }

                    reusedOldRoomGroups(__instance).Add(roomGroup);
                }
                else
                {
                    FloodAndSetRoomGroups2(__instance, currentRoomGroup(__instance)[0], roomGroup);
                    reusedOldRoomGroups(__instance).Add(roomGroup);
                }
            }
        }
        private static void FloodAndSetRoomGroups2(RegionAndRoomUpdater __instance, Room start, RoomGroup roomGroup)
        {
            Stack<Room> tmpRoomStack = new Stack<Room>();
            //tmpRoomStack.Clear();
            tmpRoomStack.Push(start);
            HashSet<Room> tmpVisitedRooms = new HashSet<Room>();
            //tmpVisitedRooms.Clear();
            tmpVisitedRooms.Add(start);
            while (tmpRoomStack.Count != 0)
            {
                Room room = tmpRoomStack.Pop();
                room.Group = roomGroup;
                foreach (Room neighbor in room.Neighbors)
                {
                    if (!tmpVisitedRooms.Contains(neighbor) && ShouldBeInTheSameRoomGroup(room, neighbor))
                    {
                        tmpRoomStack.Push(neighbor);
                        tmpVisitedRooms.Add(neighbor);
                    }
                }
            }

            tmpVisitedRooms.Clear();
            tmpRoomStack.Clear();
        }
        private static Room FindCurrentRegionGroupNeighborWithMostRegions2(RegionAndRoomUpdater __instance, out bool multipleOldNeighborRooms)
        {
            multipleOldNeighborRooms = false;
            Room room = null;
            for (int i = 0; i < currentRegionGroup(__instance).Count; i++)
            {
                foreach (Region item in currentRegionGroup(__instance)[i].NeighborsOfSameType)
                {
                    if (item.Room != null && !reusedOldRooms(__instance).Contains(item.Room))
                    {
                        if (room == null)
                        {
                            room = item.Room;
                        }
                        else if (item.Room != room)
                        {
                            multipleOldNeighborRooms = true;
                            if (item.Room.RegionCount > room.RegionCount)
                            {
                                room = item.Room;
                            }
                        }
                    }
                }
            }

            return room;
        }

        private static RoomGroup FindCurrentRoomGroupNeighborWithMostRegions2(RegionAndRoomUpdater __instance, out bool multipleOldNeighborRoomGroups)
        {
            multipleOldNeighborRoomGroups = false;
            RoomGroup roomGroup = null;
            for (int i = 0; i < currentRoomGroup(__instance).Count; i++)
            {
                foreach (Room neighbor in currentRoomGroup(__instance)[i].Neighbors)
                {
                    if (neighbor.Group != null && ShouldBeInTheSameRoomGroup(currentRoomGroup(__instance)[i], neighbor) && !reusedOldRoomGroups(__instance).Contains(neighbor.Group))
                    {
                        if (roomGroup == null)
                        {
                            roomGroup = neighbor.Group;
                        }
                        else if (neighbor.Group != roomGroup)
                        {
                            multipleOldNeighborRoomGroups = true;
                            if (neighbor.Group.RegionCount > roomGroup.RegionCount)
                            {
                                roomGroup = neighbor.Group;
                            }
                        }
                    }
                }
            }

            return roomGroup;
        }



        private static void NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature2(RegionAndRoomUpdater __instance)
        {
            foreach (Room reusedOldRoom in reusedOldRooms(__instance))
            {
                reusedOldRoom.Notify_RoomShapeOrContainedBedsChanged();
            }

            for (int i = 0; i < newRooms(__instance).Count; i++)
            {
                newRooms(__instance)[i].Notify_RoomShapeOrContainedBedsChanged();
            }

            foreach (RoomGroup reusedOldRoomGroup in reusedOldRoomGroups(__instance))
            {
                reusedOldRoomGroup.Notify_RoomGroupShapeChanged();
            }

            for (int j = 0; j < newRoomGroups(__instance).Count; j++)
            {
                RoomGroup roomGroup = newRoomGroups(__instance)[j];
                roomGroup.Notify_RoomGroupShapeChanged();
                if (map(__instance).temperatureCache.TryGetAverageCachedRoomGroupTemp(roomGroup, out float result))
                {
                    roomGroup.Temperature = result;
                }
            }
        }



        private static void CreateOrUpdateRooms2(RegionAndRoomUpdater __instance)
        {
            newRooms(__instance).Clear();
            reusedOldRooms(__instance).Clear();
            newRoomGroups(__instance).Clear();
            reusedOldRoomGroups(__instance).Clear();
            int numRegionGroups = CombineNewRegionsIntoContiguousGroups2(__instance);
            CreateOrAttachToExistingRooms2(__instance, numRegionGroups);
            int numRoomGroups = CombineNewAndReusedRoomsIntoContiguousGroups2(__instance);
            CreateOrAttachToExistingRoomGroups2(__instance, numRoomGroups);
            NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature2(__instance);
            newRooms(__instance).Clear();
            reusedOldRooms(__instance).Clear();
            newRoomGroups(__instance).Clear();
            reusedOldRoomGroups(__instance).Clear();
        }



    }
}
