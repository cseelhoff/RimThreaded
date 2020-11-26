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

    public class Reachability_Patch
    {
        public static AccessTools.FieldRef<Reachability, Map> map =
            AccessTools.FieldRefAccess<Reachability, Map>("map");
        public static uint offsetReachedIndex = 1;
        private static readonly object reachedIndexLock = new object();
        public static bool CanReach(Reachability __instance, ref bool __result,
          IntVec3 start,
          LocalTargetInfo dest,
          PathEndMode peMode,
          TraverseParms traverseParams)
        {
            /*
            if (working)
            {
                Log.ErrorOnce("Called CanReach() while working. This should never happen. Suppressing further errors.", 7312233);
                return false;
            }
            */

            Map this_map = map(__instance);
            if (traverseParams.pawn != null)
            {
                if (!traverseParams.pawn.Spawned)
                {
                    __result = false;
                    return false;
                }
                if (traverseParams.pawn.Map != this_map)
                {
                    Log.Error("Called CanReach() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=" +
                        (object)traverseParams.pawn + " pawn.Map=" + (object)traverseParams.pawn.Map + " map=" + (object)map(__instance), false);
                    __result = false;
                    return false;
                }
            }
            if (ReachabilityImmediate.CanReachImmediate(start, dest, this_map, peMode, traverseParams.pawn))
            {
                __result = true;
                return false;
            }
            if (!dest.IsValid || dest.HasThing && dest.Thing.Map != this_map || (!start.InBounds(this_map) || !dest.Cell.InBounds(this_map)))
            {
                __result = false;
                return false;
            }
            if ((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) && (traverseParams.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater))
            {
                Room room = RegionAndRoomQuery.RoomAtFast(start, this_map, RegionType.Set_Passable);
                if (room != null && room == RegionAndRoomQuery.RoomAtFast(dest.Cell, this_map, RegionType.Set_Passable))
                {
                    __result = true;
                    return false;
                }
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings)
            {
                TraverseParms traverseParams1 = traverseParams;
                traverseParams1.mode = TraverseMode.PassDoors;
                if (__instance.CanReach(start, dest, peMode, traverseParams1))
                {
                    __result = true;
                    return false;
                }
            }
            dest = (LocalTargetInfo)GenPath.ResolvePathMode(traverseParams.pawn, dest.ToTargetInfo(this_map), ref peMode);
            //working = true;
            try
            {
                uint this_reachedIndex; //Replaced reachedIndex
                lock (reachedIndexLock) //Added
                {
                    this_reachedIndex = offsetReachedIndex; //Added
                    offsetReachedIndex += 100000; //Added
                }
                HashSet<Region> regionsReached = new HashSet<Region>(); //Added
                PathGrid pathGrid = this_map.pathGrid; //Replaced pathGrid
                RegionGrid regionGrid = this_map.regionGrid; //Replaced regionGrid
                ++this_reachedIndex; //Replaced reachedIndex

                //this_destRegions.Clear();
                List<Region> this_destRegions = new List<Region>(); //Replaced destRegions
                switch (peMode)
                {
                    case PathEndMode.OnCell:
                        Region region = dest.Cell.GetRegion(this_map, RegionType.Set_Passable);
                        if (region != null && region.Allows(traverseParams, true))
                        {
                            this_destRegions.Add(region);
                            break;
                        }
                        break;
                    case PathEndMode.Touch:
                        TouchPathEndModeUtility.AddAllowedAdjacentRegions(dest, traverseParams, this_map, this_destRegions);
                        break;
                }
                if (this_destRegions.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    //this.FinalizeCheck();
                    __result = false;
                    return false;
                }
                this_destRegions.RemoveDuplicates<Region>();
                //this_openQueue.Clear();
                Queue<Region> this_openQueue = new Queue<Region>(); //Replaced openQueue
                int this_numRegionsOpened = 0; //Replaced numRegionsOpened
                List<Region> this_startingRegions = new List<Region>();
                DetermineStartRegions2(__instance, start, this_startingRegions, pathGrid, regionGrid, this_reachedIndex, this_openQueue, ref this_numRegionsOpened, regionsReached);
                if (this_openQueue.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    //this.FinalizeCheck();
                    __result = false;
                    return false;
                }
                ReachabilityCache this_cache = new ReachabilityCache();
                if (this_startingRegions.Any<Region>() && this_destRegions.Any<Region>() && CanUseCache(traverseParams.mode))
                {

                    switch (GetCachedResult2(traverseParams, this_startingRegions, this_destRegions, this_cache))
                    {
                        case BoolUnknown.True:
                            //this.FinalizeCheck();
                            __result = true;
                            return false;
                        case BoolUnknown.False:
                            //this.FinalizeCheck();
                            __result = false;
                            return false;
                    }
                }
                if (traverseParams.mode == TraverseMode.PassAllDestroyableThings || traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater || traverseParams.mode == TraverseMode.NoPassClosedDoorsOrWater)
                {
                    int num = CheckCellBasedReachability(start, dest, peMode, traverseParams,
                          regionGrid, __instance, this_startingRegions, this_cache, this_destRegions
                        ) ? 1 : 0;
                    //this.FinalizeCheck();
                    __result = num != 0;
                    return false;
                }
                int num1 = CheckRegionBasedReachability(traverseParams,
                     this_openQueue, this_reachedIndex,
             this_destRegions, this_startingRegions, this_cache, ref this_numRegionsOpened, regionsReached
                    ) ? 1 : 0;
                //this.FinalizeCheck();
                __result = num1 != 0;
                return false;
            }
            finally
            {
            }
        }
        public static bool CanReach2(Reachability __instance,
                  IntVec3 start,
                  LocalTargetInfo dest,
                  PathEndMode peMode,
                  TraverseParms traverseParams)
        {
            bool __result;
            Map this_map = map(__instance);
            if (traverseParams.pawn != null)
            {
                if (!traverseParams.pawn.Spawned)
                {
                    __result = false;
                    return __result;
                }
                if (traverseParams.pawn.Map != this_map)
                {
                    Log.Error("Called CanReach() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=" +
                        (object)traverseParams.pawn + " pawn.Map=" + (object)traverseParams.pawn.Map + " map=" + (object)map(__instance), false);
                    __result = false;
                    return __result;
                }
            }
            if (ReachabilityImmediate.CanReachImmediate(start, dest, this_map, peMode, traverseParams.pawn))
            {
                __result = true;
                return __result;
            }
            if (!dest.IsValid || dest.HasThing && dest.Thing.Map != this_map || (!start.InBounds(this_map) || !dest.Cell.InBounds(this_map)))
            {
                __result = false;
                return __result;
            }
            if ((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) && (traverseParams.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater))
            {
                Room room = RegionAndRoomQuery.RoomAtFast(start, this_map, RegionType.Set_Passable);
                if (room != null && room == RegionAndRoomQuery.RoomAtFast(dest.Cell, this_map, RegionType.Set_Passable))
                {
                    __result = true;
                    return __result;
                }
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings)
            {
                TraverseParms traverseParams1 = traverseParams;
                traverseParams1.mode = TraverseMode.PassDoors;
                if (__instance.CanReach(start, dest, peMode, traverseParams1))
                {
                    __result = true;
                    return __result;
                }
            }
            dest = (LocalTargetInfo)GenPath.ResolvePathMode(traverseParams.pawn, dest.ToTargetInfo(this_map), ref peMode);

            try
            {
                HashSet<Region> regionsReached = new HashSet<Region>();
                uint this_reachedIndex;
                lock (reachedIndexLock)
                {
                    this_reachedIndex = offsetReachedIndex;
                    offsetReachedIndex += 100000;
                }
                List<Region> this_destRegions = new List<Region>();
                PathGrid pathGrid = this_map.pathGrid;
                RegionGrid regionGrid = this_map.regionGrid;
                ++this_reachedIndex;
                //this_destRegions.Clear();
                switch (peMode)
                {
                    case PathEndMode.OnCell:
                        Region region = dest.Cell.GetRegion(this_map, RegionType.Set_Passable);
                        if (region != null && region.Allows(traverseParams, true))
                        {
                            this_destRegions.Add(region);
                            break;
                        }
                        break;
                    case PathEndMode.Touch:
                        TouchPathEndModeUtility.AddAllowedAdjacentRegions(dest, traverseParams, this_map, this_destRegions);
                        break;
                }
                if (this_destRegions.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    //this.FinalizeCheck();
                    __result = false;
                    return __result;
                }
                this_destRegions.RemoveDuplicates<Region>();
                Queue<Region> this_openQueue = new Queue<Region>();
                //this_openQueue.Clear();
                int this_numRegionsOpened = 0;
                List<Region> this_startingRegions = new List<Region>();
                DetermineStartRegions2(__instance, start, this_startingRegions, pathGrid, regionGrid, this_reachedIndex, this_openQueue, ref this_numRegionsOpened, regionsReached);
                if (this_openQueue.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings && traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    //this.FinalizeCheck();
                    __result = false;
                    return __result;
                }
                ReachabilityCache this_cache = new ReachabilityCache();
                if (this_startingRegions.Any<Region>() && this_destRegions.Any<Region>() && CanUseCache(traverseParams.mode))
                {

                    switch (GetCachedResult2(traverseParams, this_startingRegions, this_destRegions, this_cache))
                    {
                        case BoolUnknown.True:
                            //this.FinalizeCheck();
                            __result = true;
                            return __result;
                        case BoolUnknown.False:
                            //this.FinalizeCheck();
                            __result = false;
                            return __result;
                    }
                }
                if (traverseParams.mode == TraverseMode.PassAllDestroyableThings || traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater || traverseParams.mode == TraverseMode.NoPassClosedDoorsOrWater)
                {
                    int num = CheckCellBasedReachability(start, dest, peMode, traverseParams,
                          regionGrid, __instance, this_startingRegions, this_cache, this_destRegions
                        ) ? 1 : 0;
                    //this.FinalizeCheck();
                    __result = num != 0;
                    return __result;
                }
                int num1 = CheckRegionBasedReachability(traverseParams,
                     this_openQueue, this_reachedIndex,
             this_destRegions, this_startingRegions, this_cache, ref this_numRegionsOpened, regionsReached
                    ) ? 1 : 0;
                //this.FinalizeCheck();
                __result = num1 != 0;
                return __result;
            }
            finally
            {
            }
        }

        private static void QueueNewOpenRegion(Region region, uint this_reachedIndex, Queue<Region> this_openQueue, ref int this_numRegionsOpened, HashSet<Region> regionsReached)
        {
            if (region == null)
                Log.ErrorOnce("Tried to queue null region.", 881121, false);
            else if ((int)region.reachedIndex == (int)this_reachedIndex)
            {
                Log.ErrorOnce("Region is already reached; you can't open it. Region: " + region.ToString(), 719991, false);
            }
            else
            {
                this_openQueue.Enqueue(region);
                region.reachedIndex = this_reachedIndex;
                regionsReached.Add(region);
                ++this_numRegionsOpened;
            }
        }
        private static bool CheckRegionBasedReachability(TraverseParms traverseParams, Queue<Region> this_openQueue, uint this_reachedIndex, 
            List<Region> this_destRegions, List<Region> this_startingRegions, ReachabilityCache this_cache, ref int this_numRegionsOpened, HashSet<Region> regionsReached)
        {
            while (this_openQueue.Count > 0)
            {
                Region region1 = this_openQueue.Dequeue();
                for (int index1 = 0; index1 < region1.links.Count; ++index1)
                {
                    RegionLink link = region1.links[index1];
                    for (int index2 = 0; index2 < 2; ++index2)
                    {
                        Region region2 = link.regions[index2];
                        if (region2 != null && !regionsReached.Contains(region2) && (region2.type.Passable() && region2.Allows(traverseParams, false)))
                        {
                            if (this_destRegions.Contains(region2))
                            {
                                for (int index3 = 0; index3 < this_startingRegions.Count; ++index3)
                                {
                                    Region regionA = this_startingRegions[index3];
                                    Room roomA = null;
                                    if(regionA != null)
                                    {
                                        roomA = regionA.Room;
                                        this_cache.AddCachedResult(roomA, region2.Room, traverseParams, true);
                                    }                                    
                                }
                                return true;
                            }
                            QueueNewOpenRegion(region2,
                                 this_reachedIndex, this_openQueue, ref this_numRegionsOpened, regionsReached
                                );
                        }
                    }
                }
            }
            for (int index1 = 0; index1 < this_startingRegions.Count; ++index1)
            {
                for (int index2 = 0; index2 < this_destRegions.Count; ++index2)
                    this_cache.AddCachedResult(this_startingRegions[index1].Room, this_destRegions[index2].Room, traverseParams, false);
            }
            return false;
        }

        private static bool CheckCellBasedReachability(
          IntVec3 start,
          LocalTargetInfo dest,
          PathEndMode peMode,
          TraverseParms traverseParams, RegionGrid regionGrid, Reachability __instance, List<Region> this_startingRegions, 
          ReachabilityCache this_cache, List<Region> this_destRegions)
        {
            IntVec3 foundCell = IntVec3.Invalid;
            Region[] directRegionGrid = regionGrid.DirectGrid;
            PathGrid pathGrid = map(__instance).pathGrid;
            CellIndices cellIndices = map(__instance).cellIndices;
            map(__instance).floodFiller.FloodFill(start, (Predicate<IntVec3>)(c =>
            {
                int index = cellIndices.CellToIndex(c);
                if ((traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater || traverseParams.mode == TraverseMode.NoPassClosedDoorsOrWater) && 
                c.GetTerrain(map(__instance)).IsWater)
                    return false;
                if (traverseParams.mode == TraverseMode.PassAllDestroyableThings || traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    if (!pathGrid.WalkableFast(index))
                    {
                        Building edifice = c.GetEdifice(map(__instance));
                        if (edifice == null || !PathFinder.IsDestroyable((Thing)edifice))
                            return false;
                    }
                }
                else if (traverseParams.mode != TraverseMode.NoPassClosedDoorsOrWater)
                {
                    Log.ErrorOnce("Do not use this method for non-cell based modes!", 938476762, false);
                    if (!pathGrid.WalkableFast(index))
                        return false;
                }
                Region region = directRegionGrid[index];
                return region == null || region.Allows(traverseParams, false);
            }), (Func<IntVec3, bool>)(c =>
            {
                if (!ReachabilityImmediate.CanReachImmediate(c, dest, map(__instance), peMode, traverseParams.pawn))
                    return false;
                foundCell = c;
                return true;
            }), int.MaxValue, false, (IEnumerable<IntVec3>)null);
            if (foundCell.IsValid)
            {
                if (CanUseCache(traverseParams.mode))
                {
                    Region validRegionAt = regionGrid.GetValidRegionAt(foundCell);
                    if (validRegionAt != null)
                    {
                        for (int index = 0; index < this_startingRegions.Count; ++index)
                            this_cache.AddCachedResult(this_startingRegions[index].Room, validRegionAt.Room, traverseParams, true);
                    }
                }
                return true;
            }
            if (CanUseCache(traverseParams.mode))
            {
                for (int index1 = 0; index1 < this_startingRegions.Count; ++index1)
                {
                    for (int index2 = 0; index2 < this_destRegions.Count; ++index2)
                        this_cache.AddCachedResult(this_startingRegions[index1].Room, this_destRegions[index2].Room, traverseParams, false);
                }
            }
            return false;
        }

        private static BoolUnknown GetCachedResult2(TraverseParms traverseParams, List<Region> this_startingRegions, List<Region> this_destRegions, ReachabilityCache this_cache)
        {
            bool flag = false;
            for (int index1 = 0; index1 < this_startingRegions.Count; ++index1)
            {
                for (int index2 = 0; index2 < this_destRegions.Count; ++index2)
                {
                    Region tsr1 = this_startingRegions[index1];
                    Region tdr2 = this_destRegions[index2];
                    if (tdr2 == tsr1)
                        return BoolUnknown.True;
                    Room tsr1r = tsr1.Room;
                    Room tdr2r = tdr2.Room;
                    if (null != tsr1r && null != tdr2r)
                    {
                        switch (this_cache.CachedResultFor(tsr1r, tdr2r, traverseParams))
                        {
                            case BoolUnknown.True:
                                return BoolUnknown.True;
                            case BoolUnknown.Unknown:
                                flag = true;
                                break;
                        }                        
                    }
                }
            }
            return !flag ? BoolUnknown.False : BoolUnknown.Unknown;
        }

        private static bool CanUseCache(TraverseMode mode)
        {
            return mode != TraverseMode.PassAllDestroyableThingsNotWater && mode != TraverseMode.NoPassClosedDoorsOrWater;
        }

        private static void DetermineStartRegions2(Reachability __instance, IntVec3 start, List<Region> this_startingRegions, PathGrid pathGrid, 
            RegionGrid regionGrid, uint this_reachedIndex, Queue<Region> this_openQueue, ref int this_numRegionsOpened, HashSet<Region> regionsReached)
        {
            //this_startingRegions.Clear();
            if (pathGrid.WalkableFast(start))
            {
                Region validRegionAt = regionGrid.GetValidRegionAt(start);
                if (validRegionAt != null)
                {
                    QueueNewOpenRegion(validRegionAt, this_reachedIndex, this_openQueue, ref this_numRegionsOpened, regionsReached);
                    this_startingRegions.Add(validRegionAt);
                } else
                {
                    Log.Warning("regionGrid.GetValidRegionAt returned null for start at: " + start.ToString());
                }
            }
            else
            {
                for (int index = 0; index < 8; ++index)
                {
                    IntVec3 intVec3 = start + GenAdj.AdjacentCells[index];
                    if (intVec3.InBounds(map(__instance)) && pathGrid.WalkableFast(intVec3))
                    {
                        Region validRegionAt = regionGrid.GetValidRegionAt(intVec3);
                        if (validRegionAt != null && (int)validRegionAt.reachedIndex != (int)this_reachedIndex)
                        {
                            QueueNewOpenRegion(validRegionAt, this_reachedIndex, this_openQueue, ref this_numRegionsOpened, regionsReached);
                            this_startingRegions.Add(validRegionAt);
                        }
                    }
                }
            }
        }


        
    }
}
