using System;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace RimThreaded
{

    public class RegionAndRoomUpdater_Patch
    {
        [ThreadStatic] public static Stack<Room> tmpRoomStack;
        [ThreadStatic] public static bool working;

        static readonly Type original = typeof(RegionAndRoomUpdater);
        static readonly Type patched = typeof(RegionAndRoomUpdater_Patch);

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FloodAndSetRoomGroups");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CombineNewAndReusedRoomsIntoContiguousGroups");
        }

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "TryRebuildDirtyRegionsAndRooms");
        }

        public static HashSet<IntVec3> cellsWithNewRegions = new HashSet<IntVec3>();
        public static List<Region> regionsToReDirty = new List<Region>();
        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            if (!__instance.Enabled || working) return false;
            //regionCleaning.WaitOne();
            lock (RegionDirtyer_Patch.regionDirtyerLock)
            {
                working = true;
                if (!__instance.initialized) __instance.RebuildAllRegionsAndRooms();
                List<IntVec3> dirtyCells = RegionDirtyer_Patch.get_DirtyCells(__instance.map.regionDirtyer);
                {
                    if (dirtyCells.Count == 0)
                    {
                        working = false;
                        //regionCleaning.Set();
                        return false;
                    }
                    try
                    {
                        RegenerateNewRegionsFromDirtyCells2(__instance, dirtyCells);
                        CreateOrUpdateRooms2(__instance);
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
                }
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

        private static void CreateOrUpdateRooms2(RegionAndRoomUpdater __instance)
        {
            __instance.newRooms = new List<Room>();
            __instance.reusedOldRooms = new HashSet<Room>();
            __instance.newRoomGroups = new List<RoomGroup>();
            __instance.reusedOldRoomGroups = new HashSet<RoomGroup>();
            int numRegionGroups = __instance.CombineNewRegionsIntoContiguousGroups(); //CombineNewRegionsIntoContiguousGroups2(__instance);
            //actionCreateOrAttachToExistingRooms(__instance, numRegionGroups); //CreateOrAttachToExistingRooms2(__instance, numRegionGroups);
            CreateOrAttachToExistingRooms2(__instance, numRegionGroups);
            int numRoomGroups = CombineNewAndReusedRoomsIntoContiguousGroups2(__instance);
            __instance.CreateOrAttachToExistingRoomGroups(numRoomGroups); //CreateOrAttachToExistingRoomGroups2(__instance, numRoomGroups);
            __instance.NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature(); //NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature2(__instance);
        }
        
        private static void CreateOrAttachToExistingRooms2(RegionAndRoomUpdater __instance, int numRegionGroups)
        {
            Region currentRegionGroup3;
            List<Region> newRegions = __instance.newRegions;
            for (int i = 0; i < numRegionGroups; i++)
            {
                List<Region> currentRegionGroup2 = __instance.currentRegionGroup;
                currentRegionGroup2.Clear();
                for (int j = 0; j < newRegions.Count; j++)
                {
                    if (newRegions[j].newRegionGroupIndex == i)
                    {
                        currentRegionGroup2.Add(newRegions[j]);
                    }
                }
                currentRegionGroup3 = currentRegionGroup2[0];
                if (!currentRegionGroup3.type.AllowsMultipleRegionsPerRoom())
                {
                    if (currentRegionGroup2.Count != 1)
                    {
                        Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
                    }
                    Room room = Room.MakeNew(__instance.map);
                    currentRegionGroup3.Room = room;
                    __instance.newRooms.Add(room);
                    continue;
                }
                Room room2 = FindCurrentRegionGroupNeighborWithMostRegions2(__instance, out bool multipleOldNeighborRooms);
                if (room2 == null)
                {
                    Room item = RegionTraverser.FloodAndSetRooms(currentRegionGroup3, __instance.map, null);
                    __instance.newRooms.Add(item);
                }
                else if (!multipleOldNeighborRooms)
                {
                    for (int k = 0; k < currentRegionGroup2.Count; k++)
                    {
                        currentRegionGroup2[k].Room = room2;
                    }
                    __instance.reusedOldRooms.Add(room2);
                }
                else
                {
                    RegionTraverser.FloodAndSetRooms(currentRegionGroup3, __instance.map, room2);
                    __instance.reusedOldRooms.Add(room2);
                }
            }
        }
        private static int CombineNewAndReusedRoomsIntoContiguousGroups2(RegionAndRoomUpdater __instance)
        {
            int num = 0;
            foreach (Room reusedOldRoom in __instance.reusedOldRooms)
            {
                reusedOldRoom.newOrReusedRoomGroupIndex = -1;
            }
            if (tmpRoomStack == null)
            {
                tmpRoomStack = new Stack<Room>();
            }
            else
            {
                tmpRoomStack.Clear();
            }
            
            foreach (Room item in __instance.reusedOldRooms.Concat(__instance.newRooms))
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
                            if (neighbor.newOrReusedRoomGroupIndex < 0 && __instance.ShouldBeInTheSameRoomGroup(room, neighbor))
                            {
                                neighbor.newOrReusedRoomGroupIndex = num;
                                tmpRoomStack.Push(neighbor);
                            }
                        }
                    }
                    num++;
                }
            }
            return num;
        }

        private static Room FindCurrentRegionGroupNeighborWithMostRegions2(RegionAndRoomUpdater __instance, out bool multipleOldNeighborRooms)
        {
            multipleOldNeighborRooms = false;
            Room room = null;
            for (int i = 0; i < __instance.currentRegionGroup.Count; i++)
            {
                foreach (Region item in __instance.currentRegionGroup[i].NeighborsOfSameType)
                {
                    Region currentRegionGroup3 = item;
                    Map map2 = currentRegionGroup3.Map;
                    CellIndices cellIndices = map2.cellIndices;
                    Region[] directGrid = map2.regionGrid.DirectGrid;
                    bool cellExists = false;
                    foreach (IntVec3 intVec3 in currentRegionGroup3.extentsClose)
                    {
                        if (directGrid[cellIndices.CellToIndex(intVec3)] == currentRegionGroup3)
                        {
                            cellExists = true;
                            break;
                        }
                    }
                    if (!cellExists)
                    {
                        Log.Error("still bad regions");
                    }
                    if (item.Room != null && !__instance.reusedOldRooms.Contains(item.Room))
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



    }
}