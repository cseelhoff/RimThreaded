using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class RegionAndRoomUpdater_Patch
    {
        [ThreadStatic] public static Stack<Room> tmpRoomStack;
        [ThreadStatic] public static HashSet<Room> tmpVisitedRooms;
        [ThreadStatic] public static bool working;

        public static FieldRef<RegionAndRoomUpdater, Map> map =
            FieldRefAccess<RegionAndRoomUpdater, Map>("map");
        public static FieldRef<RegionAndRoomUpdater, List<Region>> newRegions =
            FieldRefAccess<RegionAndRoomUpdater, List<Region>>("newRegions");
        public static FieldRef<RegionAndRoomUpdater, List<Room>> newRooms =
            FieldRefAccess<RegionAndRoomUpdater, List<Room>>("newRooms");
        public static FieldRef<RegionAndRoomUpdater, HashSet<Room>> reusedOldRooms =
            FieldRefAccess<RegionAndRoomUpdater, HashSet<Room>>("reusedOldRooms");
        public static FieldRef<RegionAndRoomUpdater, List<RoomGroup>> newRoomGroups =
            FieldRefAccess<RegionAndRoomUpdater, List<RoomGroup>>("newRoomGroups");
        public static FieldRef<RegionAndRoomUpdater, HashSet<RoomGroup>> reusedOldRoomGroups =
            FieldRefAccess<RegionAndRoomUpdater, HashSet<RoomGroup>>("reusedOldRoomGroups");
        public static FieldRef<RegionAndRoomUpdater, List<Region>> currentRegionGroup =
            FieldRefAccess<RegionAndRoomUpdater, List<Region>>("currentRegionGroup");
        public static FieldRef<RegionAndRoomUpdater, List<Room>> currentRoomGroup =
            FieldRefAccess<RegionAndRoomUpdater, List<Room>>("currentRoomGroup");
        public static FieldRef<RegionAndRoomUpdater, bool> initialized =
            FieldRefAccess<RegionAndRoomUpdater, bool>("initialized");
        public static Dictionary<int, bool> threadRebuilding = new Dictionary<int, bool>();
        public static EventWaitHandle regionCleaning = new AutoResetEvent(true);

        static readonly MethodInfo methodShouldBeInTheSameRoomGroup =
            Method(typeof(RegionAndRoomUpdater), "ShouldBeInTheSameRoomGroup", new Type[] { typeof(Room), typeof(Room) });
        static readonly Func<RegionAndRoomUpdater, Room, Room, bool> funcShouldBeInTheSameRoomGroup =
            (Func<RegionAndRoomUpdater, Room, Room, bool>)Delegate.CreateDelegate(typeof(Func<RegionAndRoomUpdater, Room, Room, bool>), methodShouldBeInTheSameRoomGroup);

        static readonly MethodInfo methodCombineNewRegionsIntoContiguousGroups =
            Method(typeof(RegionAndRoomUpdater), "CombineNewRegionsIntoContiguousGroups", new Type[] { });
        static readonly Func<RegionAndRoomUpdater, int> funcCombineNewRegionsIntoContiguousGroups =
            (Func<RegionAndRoomUpdater, int>)Delegate.CreateDelegate(typeof(Func<RegionAndRoomUpdater, int>), methodCombineNewRegionsIntoContiguousGroups);

        static readonly MethodInfo methodCreateOrAttachToExistingRooms =
            Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingRooms", new Type[] { typeof(int) });
        static readonly Action<RegionAndRoomUpdater, int> actionCreateOrAttachToExistingRooms =
            (Action<RegionAndRoomUpdater, int>)Delegate.CreateDelegate(typeof(Action<RegionAndRoomUpdater, int>), methodCreateOrAttachToExistingRooms);

        static readonly MethodInfo methodCreateOrAttachToExistingRoomGroups =
            Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingRoomGroups", new Type[] { typeof(int) });
        static readonly Action<RegionAndRoomUpdater, int> actionCreateOrAttachToExistingRoomGroups =
            (Action<RegionAndRoomUpdater, int>)Delegate.CreateDelegate(typeof(Action<RegionAndRoomUpdater, int>), methodCreateOrAttachToExistingRoomGroups);

        static readonly MethodInfo methodNotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature =
            Method(typeof(RegionAndRoomUpdater), "NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature", new Type[] { });
        static readonly Action<RegionAndRoomUpdater> actionNotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature =
            (Action<RegionAndRoomUpdater>)Delegate.CreateDelegate(typeof(Action<RegionAndRoomUpdater>), methodNotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature);

        static readonly Type original = typeof(RegionAndRoomUpdater);
        static readonly Type patched = typeof(RegionAndRoomUpdater_Patch);

        internal static void InitializeThreadStatics()
        {
            tmpRoomStack = new Stack<Room>();
            tmpVisitedRooms = new HashSet<Room>();
        }

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

        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            if (!__instance.Enabled || working) return false;
            //regionCleaning.WaitOne();
            lock (RegionDirtyer_Patch.regionDirtyerLock)
            {
                working = true;
                if (!initialized(__instance)) __instance.RebuildAllRegionsAndRooms();
                List<IntVec3> dirtyCells = RegionDirtyer_Patch.get_DirtyCells(map(__instance).regionDirtyer);
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
                        map(__instance).temperatureCache.ResetCachedCellInfo(dirtyCell);
                    }
                    dirtyCells.Clear();
                    initialized(__instance) = true;
                    working = false;
                    //regionCleaning.Set();
                }
                if (DebugSettings.detectRegionListersBugs) Autotests_RegionListers.CheckBugs(map(__instance));
            }
            return false;
        }

        private static void RegenerateNewRegionsFromDirtyCells2(RegionAndRoomUpdater __instance, List<IntVec3> dirtyCells)
        {
            newRegions(__instance).Clear();
            Map localMap = map(__instance);
            //while (dirtyCells.TryDequeue(out IntVec3 dirtyCell))
            foreach(IntVec3 dirtyCell in dirtyCells)
            {
                if (dirtyCell.GetRegion(localMap, RegionType.Set_All) == null)
                {
                    Region region = localMap.regionMaker.TryGenerateRegionFrom(dirtyCell);
                    if (region != null)
                    {
                        newRegions(__instance).Add(region);
                    }
                }
            }
        }

        private static void CreateOrUpdateRooms2(RegionAndRoomUpdater __instance)
        {
            newRooms(__instance) = new List<Room>();
            reusedOldRooms(__instance) = new HashSet<Room>();
            newRoomGroups(__instance) = new List<RoomGroup>();
            reusedOldRoomGroups(__instance) = new HashSet<RoomGroup>();
            int numRegionGroups = funcCombineNewRegionsIntoContiguousGroups(__instance); //CombineNewRegionsIntoContiguousGroups2(__instance);
            //actionCreateOrAttachToExistingRooms(__instance, numRegionGroups); //CreateOrAttachToExistingRooms2(__instance, numRegionGroups);
            CreateOrAttachToExistingRooms2(__instance, numRegionGroups);
            int numRoomGroups = CombineNewAndReusedRoomsIntoContiguousGroups2(__instance);
            actionCreateOrAttachToExistingRoomGroups(__instance, numRoomGroups); //CreateOrAttachToExistingRoomGroups2(__instance, numRoomGroups);
            actionNotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature(__instance); //NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature2(__instance);
        }
        
        private static void CreateOrAttachToExistingRooms2(RegionAndRoomUpdater __instance, int numRegionGroups)
        {
            Region currentRegionGroup3;
            for (int i = 0; i < numRegionGroups; i++)
            {
                List<Region> currentRegionGroup2 = currentRegionGroup(__instance);
                currentRegionGroup2.Clear();
                for (int j = 0; j < newRegions(__instance).Count; j++)
                {
                    if (newRegions(__instance)[j].newRegionGroupIndex == i)
                    {
                        currentRegionGroup2.Add(newRegions(__instance)[j]);
                    }
                }
                currentRegionGroup3 = currentRegionGroup2[0];
                if (!currentRegionGroup3.type.AllowsMultipleRegionsPerRoom())
                {
                    if (currentRegionGroup2.Count != 1)
                    {
                        Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
                    }
                    Room room = Room.MakeNew(map(__instance));
                    currentRegionGroup3.Room = room;
                    newRooms(__instance).Add(room);
                    continue;
                }
                Room room2 = FindCurrentRegionGroupNeighborWithMostRegions2(__instance, out bool multipleOldNeighborRooms);
                if (room2 == null)
                {
                    Room item = RegionTraverser.FloodAndSetRooms(currentRegionGroup3, map(__instance), null);
                    newRooms(__instance).Add(item);
                }
                else if (!multipleOldNeighborRooms)
                {
                    for (int k = 0; k < currentRegionGroup2.Count; k++)
                    {
                        currentRegionGroup2[k].Room = room2;
                    }
                    reusedOldRooms(__instance).Add(room2);
                }
                else
                {
                    RegionTraverser.FloodAndSetRooms(currentRegionGroup3, map(__instance), room2);
                    reusedOldRooms(__instance).Add(room2);
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
            if (tmpRoomStack == null)
            {
                tmpRoomStack = new Stack<Room>();
            }
            else
            {
                tmpRoomStack.Clear();
            }
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
                            if (neighbor.newOrReusedRoomGroupIndex < 0 && funcShouldBeInTheSameRoomGroup(__instance, room, neighbor))
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
            for (int i = 0; i < currentRegionGroup(__instance).Count; i++)
            {
                foreach (Region item in currentRegionGroup(__instance)[i].NeighborsOfSameType)
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



    }
}