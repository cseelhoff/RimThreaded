using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections;

namespace RimThreaded
{

    public class GenClosest_Patch
    {

        private static bool EarlyOutSearch(
          IntVec3 start,
          Map map,
          ThingRequest thingReq,
          IEnumerable<Thing> customGlobalSearchSet,
          Predicate<Thing> validator)
        {
            if (thingReq.group == ThingRequestGroup.Everything)
            {
                Log.Error("Cannot do ClosestThingReachable searching everything without restriction.", false);
                return true;
            }
            if (!start.InBounds(map))
            {
                Log.Error("Did FindClosestThing with start out of bounds (" + start + "), thingReq=" + thingReq, false);
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
                Log.Error("RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThings. Use ClosestThingGlobal.", false);
                __result = null;
                return false;
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater)
            {
                Log.Error("RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThingsNotWater. Use ClosestThingGlobal.", false);
                __result = null;
                return false;
            }
            if (!req.IsUndefined && !req.CanBeFoundInRegion)
            {
                Log.ErrorOnce("RegionwiseBFSWorker with thing request group " + req.group + ". This group is never stored in regions. Most likely a global search should have been used.", 385766189, false);
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
                    ++regionsSeenScan;
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
                            float num = priorityGetter != null ? priorityGetter(thing) : 0.0f;
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
        public static bool ClosestThing_Global(ref Thing __result,
          IntVec3 center,
          IEnumerable searchSet,
          float maxDistance = 99999f,
          Predicate<Thing> validator = null,
          Func<Thing, float> priorityGetter = null)
        {
            if (searchSet == null)
            {
                __result = null;
                return false;
            }
            float closestDistSquared = int.MaxValue;
            Thing chosen = null;
            float bestPrio = float.MinValue;
            float maxDistanceSquared = maxDistance * maxDistance;

            RimThreaded.lastClosestThingGlobal = DateTime.Now;
            if (searchSet is IList<Thing> thingList)
            {
                for (int index = 0; index < thingList.Count; ++index)
                {
                    Process2(thingList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
                }
            }
            else if (searchSet is IList<Pawn> pawnList)
            {
                for (int index = 0; index < pawnList.Count; ++index)
                {
                    Process2(pawnList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
                }
            }
            else if (searchSet is IList<Building> buildingList)
            {
                for (int index = 0; index < buildingList.Count; ++index)
                {
                    Process2(buildingList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
                }
            }
            else if (searchSet is IList<IAttackTarget> attackTargetList)
            {
                for (int index = 0; index < attackTargetList.Count; ++index)
                {
                    Process2((Thing)attackTargetList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
                }
            }
            else
            {
                List<Thing> listThings = new List<Thing>((IEnumerable<Thing>)searchSet);
                for (int index = 0; index < listThings.Count; ++index)
                {
                    Process2(listThings[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
                }
            }
            __result = chosen;
            return false;

        }

        public static void Process2(Thing t, IntVec3 center, float maxDistanceSquared,
            Func<Thing, float> priorityGetter, ref float bestPrio, ref float closestDistSquared,
            ref Thing chosen, Predicate<Thing> validator)
        {
            DateTime now = DateTime.Now;
            if (now.Subtract(RimThreaded.lastClosestThingGlobal).TotalMilliseconds > 50)
            {
                Log.Error("ClosestThing_Global was called over 50ms ago. Resetting last check. Processing: " + t.ToString());
                RimThreaded.lastClosestThingGlobal = DateTime.Now;
            }            

            if (null != t)
            {
                if (!t.Spawned)
                    return;
                float horizontalSquared = (center - t.Position).LengthHorizontalSquared;
                if (horizontalSquared > maxDistanceSquared || priorityGetter == null && horizontalSquared >= (double)closestDistSquared || validator != null && !validator(t))
                    return;
                float num = 0.0f;
                if (priorityGetter != null)
                {
                    num = priorityGetter(t);
                    if (num < (double)bestPrio || num == (double)bestPrio && horizontalSquared >= (double)closestDistSquared)
                        return;
                }
                chosen = t;
                closestDistSquared = horizontalSquared;
                bestPrio = num;
            }
        }
    }
}