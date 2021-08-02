﻿using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class GenClosest_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(GenClosest);
            Type patched = typeof(GenClosest_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegionwiseBFSWorker");
        }

        private static bool EarlyOutSearch(
          IntVec3 start,
          Map map,
          ThingRequest thingReq,
          IEnumerable<Thing> customGlobalSearchSet,
          Predicate<Thing> validator)
        {
            if (thingReq.group == ThingRequestGroup.Everything)
            {
                Log.Error("Cannot do ClosestThingReachable searching everything without restriction.");
                return true;
            }
            if (!start.InBounds(map))
            {
                Log.Error("Did FindClosestThing with start out of bounds (" + start + "), thingReq=" + thingReq);
                return true;
            }
            return thingReq.group == ThingRequestGroup.Nothing || (thingReq.IsUndefined || map.listerThings.ThingsMatching(thingReq).Count == 0) && customGlobalSearchSet.EnumerableNullOrEmpty();
        }

        public static Thing ClosestThingReachable2(
          IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
        {
            bool flag = searchRegionsMax < 0 || forceAllowGlobalSearch;
            if (!flag && customGlobalSearchSet != null)
            {
                Log.ErrorOnce("searchRegionsMax >= 0 && customGlobalSearchSet != null && !forceAllowGlobalSearch. customGlobalSearchSet will never be used.", 634984);
            }

            if (!flag && !thingReq.IsUndefined && !thingReq.CanBeFoundInRegion)
            {
                Log.ErrorOnce(string.Concat("ClosestThingReachable with thing request group ", thingReq.group, " and global search not allowed. This will never find anything because this group is never stored in regions. Either allow global search or don't call this method at all."), 518498981);
            }

            if (EarlyOutSearch(root, map, thingReq, customGlobalSearchSet, validator))
            {
                return null;
            }

            Thing thing = null;
            bool flag2 = false;
            if (!thingReq.IsUndefined && thingReq.CanBeFoundInRegion)
            {
                int num = (searchRegionsMax > 0) ? searchRegionsMax : 30;
                thing = GenClosest.RegionwiseBFSWorker(root, map, thingReq, peMode, traverseParams, validator, null, searchRegionsMin, num, maxDistance, out int regionsSeen, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
                flag2 = (thing == null && regionsSeen < num);
            }

            if (thing == null && flag && !flag2)
            {
                if (traversableRegionTypes != RegionType.Set_Passable)
                {
                    Log.ErrorOnce("ClosestThingReachable had to do a global search, but traversableRegionTypes is not set to passable only. It's not supported, because Reachability is based on passable regions only.", 14384767);
                }

                bool validator2(Thing t)
                {
                    if (validator != null && !validator(t))
                    {
                        return false;
                    }
                    if (!map.reachability.CanReach(root, t, peMode, traverseParams))
                    {
                        return false;
                    }
                    return true;
                }
                IEnumerable<Thing> searchSet = customGlobalSearchSet ?? map.listerThings.ThingsMatching(thingReq);
                thing = GenClosest.ClosestThing_Global(root, searchSet, maxDistance, validator2);                
            }

            return thing;
        }

        public static bool RegionwiseBFSWorker(ref Thing __result,
          IntVec3 root,
          Map map,
          ThingRequest req,
          PathEndMode peMode,
          TraverseParms traverseParams,
          Predicate<Thing> validator,
          Func<Thing, float> priorityGetter,
          int minRegions,
          int maxRegions,
          float maxDistance,
          out int regionsSeen,
          RegionType traversableRegionTypes = RegionType.Set_Passable,
          bool ignoreEntirelyForbiddenRegions = false)
        {
            regionsSeen = 0;
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings)
            {
                Log.Error("RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThings. Use ClosestThingGlobal.");
                __result = null;
                return false;
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater)
            {
                Log.Error("RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThingsNotWater. Use ClosestThingGlobal.");
                __result = null;
                return false;
            }
            if (!req.IsUndefined && !req.CanBeFoundInRegion)
            {
                Log.ErrorOnce("RegionwiseBFSWorker with thing request group " + req.group + ". This group is never stored in regions. Most likely a global search should have been used.", 385766189);
                __result = null;
                return false;
            }
            Region region = root.GetRegion(map, traversableRegionTypes);
            if (region == null)
            {
                __result = null;
                return false;
            }
            float maxDistSquared = maxDistance * maxDistance;
            RegionEntryPredicate entryCondition = (from, to) =>
            {
                if (!to.Allows(traverseParams, false))
                    return false;
                return maxDistance > 5000.0 || to.extentsClose.ClosestDistSquaredTo(root) < (double)maxDistSquared;
            };
            Thing closestThing = null;
            float closestDistSquared = 9999999f;
            float bestPrio = float.MinValue;
            int regionsSeenScan = 0;
            RegionProcessor regionProcessor = r =>
            {
                if (RegionTraverser.ShouldCountRegion(r))
                    regionsSeenScan++;
                if (!r.IsDoorway && !r.Allows(traverseParams, true))
                    return false;
                if (!ignoreEntirelyForbiddenRegions || !r.IsForbiddenEntirely(traverseParams.pawn))
                {
                    List<Thing> thingList = r.ListerThings.ThingsMatching(req);
                    for (int index = 0; index < thingList.Count; ++index)
                    {
                        Thing thing = thingList[index];
                        if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, peMode, traverseParams.pawn))
                        {
                            float num = priorityGetter?.Invoke(thing) ?? 0.0f;
                            if (num >= (double)bestPrio)
                            {
                                float horizontalSquared = (thing.Position - root).LengthHorizontalSquared;
                                if ((num > (double)bestPrio || horizontalSquared < (double)closestDistSquared) && horizontalSquared < (double)maxDistSquared && (validator == null || validator(thing)))
                                {
                                    closestThing = thing;
                                    closestDistSquared = horizontalSquared;
                                    bestPrio = num;
                                }
                            }
                        }
                    }
                }
                return regionsSeenScan >= minRegions && closestThing != null;
            };
            RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
            regionsSeen = regionsSeenScan;
            __result = closestThing;
            return false;
        }
    }
}