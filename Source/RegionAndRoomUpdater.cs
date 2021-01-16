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
using System.Collections.Concurrent;

namespace RimThreaded
{

    public class RegionAndRoomUpdater_Patch
    {

        public static AccessTools.FieldRef<RegionDirtyer, Map> maprd =
            AccessTools.FieldRefAccess<RegionDirtyer, Map>("map");
        public static void SetAllClean2(RegionDirtyer __instance)
        {
            ConcurrentQueue<IntVec3> dirtyCells = RegionDirtyer_Patch.get_DirtyCells(__instance);
            while (dirtyCells.TryDequeue(out IntVec3 dirtyCell))
            {
                //IntVec3 dirtyCell;
                //try
                //{
                    //dirtyCell = dirtyCells[i];
                //}
                //catch(ArgumentOutOfRangeException) { break;  }
                    
                maprd(__instance).temperatureCache.ResetCachedCellInfo(dirtyCell);
            }

            //dirtyCells.Clear();
            dirtyCells = new ConcurrentQueue<IntVec3>();
            lock (RegionDirtyer_Patch.dirtyCellsDict)
            {
                RegionDirtyer_Patch.dirtyCellsDict.SetOrAdd(__instance, dirtyCells);
            }
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

        public static Dictionary<int, bool> threadRebuilding = new Dictionary<int, bool>();
        public static Dictionary<RegionAndRoomUpdater, EventWaitHandle> gate1WaitHandles = new Dictionary<RegionAndRoomUpdater, EventWaitHandle>();
        public static Dictionary<RegionAndRoomUpdater, EventWaitHandle> gate2WaitHandles = new Dictionary<RegionAndRoomUpdater, EventWaitHandle>();
        public static Dictionary<RegionAndRoomUpdater, HashSet<IntVec3>> oldDirtyCellsDict = new Dictionary<RegionAndRoomUpdater, HashSet<IntVec3>>();
        public static Dictionary<RegionAndRoomUpdater, HashSet<int>> gate1ThreadSets = new Dictionary<RegionAndRoomUpdater, HashSet<int>>();
        public static Dictionary<RegionAndRoomUpdater, HashSet<int>> gate2ThreadSets = new Dictionary<RegionAndRoomUpdater, HashSet<int>>();
        public static Dictionary<RegionAndRoomUpdater, Integer> gate1Counts = new Dictionary<RegionAndRoomUpdater, Integer>();
        public static Dictionary<RegionAndRoomUpdater, Integer> gate2Counts = new Dictionary<RegionAndRoomUpdater, Integer>();

        public static object workingLock = new object();
        public static int workingInt = 0;
        public static object initializingLock = new object();
        public static object regionMakerLock = new object();


        public struct Integer
        {
            public int integer;
        }
        //public static Dictionary<RegionDirtyer, int> dirtyCellsCompleted = new Dictionary<RegionDirtyer, int>();

        public static EventWaitHandle regionCleaning = new ManualResetEvent(false);

        public static int dirtyCellsStartIndex = 0;
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

        public static AccessTools.FieldRef<RegionDirtyer, List<IntVec3>> dirtyCells =
            AccessTools.FieldRefAccess<RegionDirtyer, List<IntVec3>>("dirtyCells");

        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            //todo: optimize lock speedup fix

            //lock (workingLock)
            //{
            //if (working(__instance) || !__instance.Enabled)
            if(!__instance.Enabled)
            {
                return false;
            }
            if(getThreadRebuilding())
            {
                return false;
            }
            int workerId = Interlocked.Increment(ref workingInt);
            if (workerId > 1)
            {
                regionCleaning.WaitOne();
                //Interlocked.Decrement(ref workingInt);
                return false;
            }
            regionCleaning.Reset();
            setThreadRebuilding(true);
            //working(__instance) = true;
            if (!initialized(__instance))
            {
                __instance.RebuildAllRegionsAndRooms();
            }

            //if (!map(__instance).regionDirtyer.AnyDirty)
            if (RegionDirtyer_Patch.get_DirtyCells(map(__instance).regionDirtyer).IsEmpty)
            {
                //working(__instance) = false;
                resumeThreads();
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
            //working(__instance) = false;
            resumeThreads();
            //}
            if (DebugSettings.detectRegionListersBugs)
            {
                Autotests_RegionListers.CheckBugs(map(__instance));
            }
            return false;
        }

        public static bool TryRebuildDirtyRegionsAndRooms2(RegionAndRoomUpdater __instance)
        {
            //if (working || !Enabled)
            //  return;
            if (!__instance.Enabled)
            {
                return false;
            }
            if (getThreadRebuilding())
            {
                return false;
            }
            //working = true;
            setThreadRebuilding(true);
            if (!initialized(__instance))
            {
                lock (initializingLock)
                {
                    if (!initialized(__instance))
                    {
                        __instance.RebuildAllRegionsAndRooms();
                    }
                    initialized(__instance) = true;
                }
            }

            if (RegionDirtyer_Patch.get_DirtyCells(map(__instance).regionDirtyer).IsEmpty)
            {
                //working = false;
                setThreadRebuilding(false);
                return false;
            }

            try
            {
                int tid = Thread.CurrentThread.ManagedThreadId;
                //HashSet<int> gate1ThreadSet = getGate1ThreadSet(__instance);
                //gate1ThreadSet.Add(tid);
                Integer gate1Count = getGate1Count(__instance);
                int gate1Ticket = Interlocked.Increment(ref gate1Count.integer);
                EventWaitHandle gate1WaitHandle = getGate1WaitHandle(__instance);
                gate1WaitHandle.WaitOne();
                //EventWaitHandle gate2WaitHandle = getGate2WaitHandle(__instance);
                //gate2WaitHandle.Reset();
                if (gate1Ticket == 1)
                {
                    RegenerateNewRegionsFromDirtyCells2(__instance);

                    CreateOrUpdateRooms2(__instance);
                    if (DebugSettings.detectRegionListersBugs)
                    {
                        Autotests_RegionListers.CheckBugs(map(__instance));
                    }
                    newRegions(__instance).Clear();

                    gate1WaitHandle.Reset();
                }
                //HashSet<int> gate2ThreadSet = getGate2ThreadSet(__instance);
                //gate2ThreadSet.Add(tid);
                Integer gate2Count = getGate2Count(__instance);
                Interlocked.Increment(ref gate2Count.integer);
                int gate1Remaining = Interlocked.Decrement(ref gate1Count.integer);
                EventWaitHandle gate2WaitHandle = getGate2WaitHandle(__instance);
                if (gate1Remaining == 0)
                {
                    //CreateOrUpdateRooms2(__instance);
                    /*
                    HashSet<IntVec3> oldDirtyCells = getOldDirtyCells(__instance);
                    foreach(IntVec3 oldDirtyCell in oldDirtyCells)
                    {
                        map(__instance).temperatureCache.ResetCachedCellInfo(oldDirtyCell);
                    }
                    */
                    //if (DebugSettings.detectRegionListersBugs)
                    //{
                        //Autotests_RegionListers.CheckBugs(map(__instance));
                    //}
                    //newRegions(__instance).Clear();
                    gate2WaitHandle.Set();
                }
                gate2WaitHandle.WaitOne();
                int gate2Remaining = Interlocked.Decrement(ref gate2Count.integer);
                if (gate2Remaining == 0)
                {
                    gate2WaitHandle.Reset();
                    gate1WaitHandle.Set();
                }
            }
            catch (Exception arg)
            {
                Log.Error("Exception while rebuilding dirty regions: " + arg);
            }

            //newRegions.Clear();
            //map.regionDirtyer.SetAllClean();
            //initialized = true; //Moved to earlier code above
            
            //working = false;
            setThreadRebuilding(false);

            return false;
        }

        private static Integer getGate1Count(RegionAndRoomUpdater __instance)
        {
            if (!gate1Counts.TryGetValue(__instance, out Integer getGate1Count))
            {
                getGate1Count = new Integer
                {
                    integer = 0
                };
                lock (gate1Counts)
                {
                    if (!gate1Counts.TryGetValue(__instance, out Integer getGate1Count2))
                    {
                        gate1Counts.Add(__instance, getGate1Count);
                    } else
                    {
                        return getGate1Count2;
                    }
                }
            }
            return getGate1Count;
        }

        private static Integer getGate2Count(RegionAndRoomUpdater __instance)
        {
            if (!gate2Counts.TryGetValue(__instance, out Integer getGate2Count))
            {
                getGate2Count = new Integer
                {
                    integer = 0
                };
                lock (gate2Counts)
                {
                    if (!gate2Counts.TryGetValue(__instance, out Integer getGate2Count2))
                    {
                        gate2Counts.Add(__instance, getGate2Count);
                    }
                    else
                    {
                        return getGate2Count2;
                    }
                }
            }
            return getGate2Count;
        }

        private static EventWaitHandle getGate1WaitHandle(RegionAndRoomUpdater __instance)
        {
            if (!gate1WaitHandles.TryGetValue(__instance, out EventWaitHandle gate1WaitHandle))
            {
                gate1WaitHandle = new ManualResetEvent(true);
                lock (gate1WaitHandles)
                {
                    if (!gate1WaitHandles.TryGetValue(__instance, out EventWaitHandle gate1WaitHandle2))
                    {
                        gate1WaitHandles.SetOrAdd(__instance, gate1WaitHandle);
                    } else
                    {
                        return gate1WaitHandle2;
                    }
                }
            }
            return gate1WaitHandle;
        }

        private static EventWaitHandle getGate2WaitHandle(RegionAndRoomUpdater __instance)
        {
            if (!gate2WaitHandles.TryGetValue(__instance, out EventWaitHandle gate2WaitHandle))
            {
                gate2WaitHandle = new ManualResetEvent(false);
                lock (gate2WaitHandles)
                {
                    if (!gate2WaitHandles.TryGetValue(__instance, out EventWaitHandle gate2WaitHandle2))
                    {
                        gate2WaitHandles.SetOrAdd(__instance, gate2WaitHandle);
                    }
                    else
                    {
                        return gate2WaitHandle2;
                    }
                }
            }
            return gate2WaitHandle;
        }

        private static HashSet<IntVec3> getOldDirtyCells(RegionAndRoomUpdater __instance)
        {
            if (!oldDirtyCellsDict.TryGetValue(__instance, out HashSet<IntVec3> oldDirtyCells))
            {
                oldDirtyCells = new HashSet<IntVec3>();
                lock (oldDirtyCellsDict)
                {
                    if (!oldDirtyCellsDict.TryGetValue(__instance, out oldDirtyCells))
                    {
                        oldDirtyCellsDict.Add(__instance, oldDirtyCells);
                    }
                }
            }
            return oldDirtyCells;
        }

        private static HashSet<int> getGate1ThreadSet(RegionAndRoomUpdater __instance)
        {
            if (!gate1ThreadSets.TryGetValue(__instance, out HashSet<int> gate1ThreadSet))
            {
                gate1ThreadSet = new HashSet<int>();
                lock (gate1ThreadSets)
                {
                    if (!gate1ThreadSets.TryGetValue(__instance, out gate1ThreadSet))
                    {
                        gate1ThreadSets.Add(__instance, gate1ThreadSet);
                    }
                }
            }
            return gate1ThreadSet;
        }

        private static HashSet<int> getGate2ThreadSet(RegionAndRoomUpdater __instance)
        {
            if (!gate2ThreadSets.TryGetValue(__instance, out HashSet<int> gate2ThreadSet))
            {
                gate2ThreadSet = new HashSet<int>();
                lock (gate2ThreadSets)
                {
                    if (!gate2ThreadSets.TryGetValue(__instance, out gate2ThreadSet))
                    {
                        gate2ThreadSets.Add(__instance, gate2ThreadSet);
                    }
                }
            }
            return gate2ThreadSet;
        }

        private static void resumeThreads()
        {
            regionCleaning.Set();
            workingInt = 0;
            setThreadRebuilding(false);
        }

        private static void setThreadRebuilding(bool v)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            lock (threadRebuilding)
            {
                threadRebuilding[tID] = v;
            }            
        }

        private static bool getThreadRebuilding()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!threadRebuilding.TryGetValue(tID, out bool rebuilding))
            {
                rebuilding = false;
                lock (threadRebuilding)
                {
                    threadRebuilding[tID] = rebuilding;
                }
            }
            return rebuilding;
        }

        private static void RegenerateNewRegionsFromDirtyCells2(RegionAndRoomUpdater __instance)
        {
            newRegions(__instance).Clear(); //already cleared at end of method TryRebuildDirtyRegionsAndRooms()
            //List<IntVec3> dirtyCells = map(__instance).regionDirtyer.DirtyCells;
            Map localMap = map(__instance);
            RegionDirtyer regionDirtyer = localMap.regionDirtyer;
            ConcurrentQueue<IntVec3> dirtyCells = RegionDirtyer_Patch.get_DirtyCells(regionDirtyer);
            //HashSet<IntVec3> oldDirtyCells = getOldDirtyCells(__instance);
            while (dirtyCells.TryDequeue(out IntVec3 dirtyCell))
            {
                if (dirtyCell.GetRegion(localMap, RegionType.Set_All) == null)
                {
                    Region region;
                    lock (regionMakerLock) //TODO OPTIMIZE for multithreading
                    {
                        region = localMap.regionMaker.TryGenerateRegionFrom(dirtyCell);
                    }
                    //Region region = regionTryGenerateRegionFrom2(map(__instance).regionMaker, intVec);
                    if (region != null)
                    {
                        //lock (newRegions(__instance))
                        //{
                            newRegions(__instance).Add(region);
                        //}
                    }
                }
                //oldDirtyCells.Add(dirtyCell);
                localMap.temperatureCache.ResetCachedCellInfo(dirtyCell);
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
