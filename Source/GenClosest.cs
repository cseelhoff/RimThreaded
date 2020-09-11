using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections;

namespace RimThreaded
{

    public class GenClosest_Patch
    {
        public static AccessTools.FieldRef<Building_SteamGeyser, IntermittentSteamSprayer> steamSprayer =
            AccessTools.FieldRefAccess<Building_SteamGeyser, IntermittentSteamSprayer>("steamSprayer");
        public static AccessTools.FieldRef<Building_SteamGeyser, Sustainer> spraySustainer =
            AccessTools.FieldRefAccess<Building_SteamGeyser, Sustainer>("spraySustainer");
        public static AccessTools.FieldRef<Building_SteamGeyser, int> spraySustainerStartTick =
            AccessTools.FieldRefAccess<Building_SteamGeyser, int>("spraySustainerStartTick");

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
                Log.Error("Did FindClosestThing with start out of bounds (" + (object)start + "), thingReq=" + (object)thingReq, false);
                return true;
            }
            return thingReq.group == ThingRequestGroup.Nothing || (thingReq.IsUndefined || map.listerThings.ThingsMatching(thingReq).Count == 0) && customGlobalSearchSet.EnumerableNullOrEmpty<Thing>();
        }

        public static bool ClosestThingReachable(ref Thing __result,
          IntVec3 root,
          Map map,
          ThingRequest thingReq,
          PathEndMode peMode,
          TraverseParms traverseParams,
          float maxDistance = 9999f,
          Predicate<Thing> validator = null,
          IEnumerable<Thing> customGlobalSearchSet = null,
          int searchRegionsMin = 0,
          int searchRegionsMax = -1,
          bool forceAllowGlobalSearch = false,
          RegionType traversableRegionTypes = RegionType.Set_Passable,
          bool ignoreEntirelyForbiddenRegions = false)
        {
            bool flag1 = searchRegionsMax < 0 | forceAllowGlobalSearch;
            if (!flag1 && customGlobalSearchSet != null)
                Log.ErrorOnce("searchRegionsMax >= 0 && customGlobalSearchSet != null && !forceAllowGlobalSearch. customGlobalSearchSet will never be used.", 634984, false);
            if (!flag1 && !thingReq.IsUndefined && !thingReq.CanBeFoundInRegion)
            {
                Log.ErrorOnce("ClosestThingReachable with thing request group " + thingReq.group + " and global search not allowed. This will never find anything because this group is never stored in regions. Either allow global search or don't call this method at all.", 518498981, false);
                __result = null;
                return false;
            }
            if (EarlyOutSearch(root, map, thingReq, customGlobalSearchSet, validator))
            {
                __result = null;
                return false;
            }
            Thing thing = null;
            bool flag2 = false;
            if (!thingReq.IsUndefined && thingReq.CanBeFoundInRegion)
            {
                int maxRegions = searchRegionsMax > 0 ? searchRegionsMax : 30;
                int regionsSeen;
                thing = GenClosest.RegionwiseBFSWorker(root, map, thingReq, peMode, traverseParams, validator, null, searchRegionsMin, maxRegions, maxDistance, out regionsSeen, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
                flag2 = thing == null && regionsSeen < maxRegions;
            }
            if (thing == null & flag1 && !flag2)
            {
                if (traversableRegionTypes != RegionType.Set_Passable)
                    Log.ErrorOnce("ClosestThingReachable had to do a global search, but traversableRegionTypes is not set to passable only. It's not supported, because Reachability is based on passable regions only.", 14384767, false);
                // Predicate<Thing> validator1 = t => map.reachability.CanReach(root, t, peMode, traverseParams) && (validator == null || validator(t));
                Predicate<Thing> validator1 = t => Reachability_Patch.CanReach2(map.reachability, root, t, peMode, traverseParams) && (validator == null || validator(t));
                IEnumerable<Thing> things = customGlobalSearchSet ?? map.listerThings.ThingsMatching(thingReq);

                //null check needed - bug #55
                thing = GenClosest.ClosestThing_Global(root, things, maxDistance, validator1, null);
            }
            __result = thing;
            return false;
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
                __result = (Thing)null;
                return false;
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater)
            {
                Log.Error("RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThingsNotWater. Use ClosestThingGlobal.", false);
                __result = (Thing)null;
                return false;
            }
            if (!req.IsUndefined && !req.CanBeFoundInRegion)
            {
                Log.ErrorOnce("RegionwiseBFSWorker with thing request group " + (object)req.group + ". This group is never stored in regions. Most likely a global search should have been used.", 385766189, false);
                __result = (Thing)null;
                return false;
            }
            Region region = root.GetRegion(map, traversableRegionTypes);
            if (region == null)
            {
                __result = (Thing)null;
                return false;
            }
            float maxDistSquared = maxDistance * maxDistance;
            RegionEntryPredicate entryCondition = (RegionEntryPredicate)((from, to) =>
            {
                if (!to.Allows(traverseParams, false))                
                    return false;                
                return (double)maxDistance > 5000.0 || (double)to.extentsClose.ClosestDistSquaredTo(root) < (double)maxDistSquared;
            });
            Thing closestThing = (Thing)null;
            float closestDistSquared = 9999999f;
            float bestPrio = float.MinValue;
            int regionsSeenScan = 0;
            RegionProcessor regionProcessor = (RegionProcessor)(r =>
            {
                if (RegionTraverser.ShouldCountRegion(r))
                    ++regionsSeenScan;
                if (!r.IsDoorway && !r.Allows(traverseParams, true))
                    return false;
                if (!ignoreEntirelyForbiddenRegions || !r.IsForbiddenEntirely(traverseParams.pawn))
                {
                    Thing[] arrayThingList;
                    List<Thing> thingList = r.ListerThings.ThingsMatching(req);
                    lock (thingList)
                    {
                        arrayThingList = thingList.ToArray();
                    }
                    for (int index = 0; index < arrayThingList.Length; ++index)
                    {
                        Thing thing = arrayThingList[index];
                        if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, peMode, traverseParams.pawn))
                        {
                            float num = priorityGetter != null ? priorityGetter(thing) : 0.0f;
                            if ((double)num >= (double)bestPrio)
                            {
                                float horizontalSquared = (float)(thing.Position - root).LengthHorizontalSquared;
                                if (((double)num > (double)bestPrio || (double)horizontalSquared < (double)closestDistSquared) && (double)horizontalSquared < (double)maxDistSquared && (validator == null || validator(thing)))
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
            });
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
            float closestDistSquared = (float)int.MaxValue;
            Thing chosen = (Thing)null;
            float bestPrio = float.MinValue;
            float maxDistanceSquared = maxDistance * maxDistance;
            if (searchSet is IList<Thing> thingList)
            {
                for (int index = 0; index < thingList.Count; ++index)
                    Process2(thingList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
            }
            else if (searchSet is IList<Pawn> pawnList)
            {
                for (int index = 0; index < pawnList.Count; ++index)
                    Process2((Thing)pawnList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
            }
            else if (searchSet is IList<Building> buildingList)
            {
                for (int index = 0; index < buildingList.Count; ++index)
                    Process2((Thing)buildingList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
            }
            else if (searchSet is IList<IAttackTarget> attackTargetList)
            {
                for (int index = 0; index < attackTargetList.Count; ++index)
                    Process2((Thing)attackTargetList[index], center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
            }
            else
            {
                foreach (Thing search in searchSet)
                    Process2(search, center, maxDistanceSquared, priorityGetter, ref bestPrio, ref closestDistSquared, ref chosen, validator);
            }
            __result = chosen;
            return false;

        }

        public static void Process2(Thing t, IntVec3 center, float maxDistanceSquared, 
            Func<Thing, float> priorityGetter, ref float bestPrio, ref float closestDistSquared, 
            ref Thing chosen, Predicate<Thing> validator)
        {
            if (!t.Spawned)
                return;
            float horizontalSquared = (float)(center - t.Position).LengthHorizontalSquared;
            if ((double)horizontalSquared > (double)maxDistanceSquared || priorityGetter == null && (double)horizontalSquared >= (double)closestDistSquared || validator != null && !validator(t))
                return;
            float num = 0.0f;
            if (priorityGetter != null)
            {
                num = priorityGetter(t);
                if ((double)num < (double)bestPrio || (double)num == (double)bestPrio && (double)horizontalSquared >= (double)closestDistSquared)
                    return;
            }
            chosen = t;
            closestDistSquared = horizontalSquared;
            bestPrio = num;
        }
    }
}
