using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class Room_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Room);
            Type patched = typeof(Room_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(AddDistrict));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveDistrict));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_RoofChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(UpdateRoomStatsAndRole));

            RimThreadedHarmony.Prefix(original, patched, nameof(get_Fogged));


            RimThreadedHarmony.Prefix(original, patched, nameof(get_ContainedAndAdjacentThings));
            //RimThreadedHarmony.Postfix(original, patched, nameof(get_Regions));
        }
        //tmpRegions && uniqueContainedThingsOfDef can't be threadstatic
        public static void get_Regions(Room __instance, ref List<Region> __result)
        {
            //return a copy of the list instead of the actually list
            //this is caused by a bad mod that tries to modify the returned list
            if (__result != null)
            {
                lock (__instance)
                {
                    List<Region> tmp = __result;
                    __result = new List<Region>(tmp);
                    //__result.Clear();
                    //__result.AddRange(tmp);
                    //__result = new List<Region>(__result);
                }
            }
        }
        public static bool get_ContainedAndAdjacentThings(Room __instance, ref List<Thing> __result)//fixes problems with rimfridge.
        {
            HashSet<Thing> uniqueContainedThingsSet = new HashSet<Thing>();
            List<Thing> uniqueContainedThings = new List<Thing>();
            List<Region> regions = __instance.Regions;
            for (int i = 0; i < regions.Count; i++)
            {
                List<Thing> allThings = regions[i].ListerThings.AllThings;
                if (allThings == null)
                {
                    continue;
                }
                for (int j = 0; j < allThings.Count; j++)
                {
                    Thing item = allThings[j];
                    if (uniqueContainedThingsSet.Add(item))
                    {
                        uniqueContainedThings.Add(item);
                    }
                }
            }
            __result = uniqueContainedThings;
            return false;
        }
        public static bool get_Fogged(Room __instance, ref bool __result)
        {
            __result = false;
            if (__instance.RegionCount == 0)
            {
                return false;
            }
            Region tmpRegion = __instance.FirstRegion;
            Map tmpMap = __instance.Map;
            if (tmpMap == null || tmpRegion == null)
            {
                return false;
            }
            IntVec3 pos = tmpRegion.AnyCell;
            __result = pos.Fogged(tmpMap);
            return false;
        }
        public static bool UpdateRoomStatsAndRole(Room __instance)
        {
            lock (__instance)
            {
                __instance.statsAndRoleDirty = false;
                if (__instance.ProperRoom && __instance.RegionCount <= 36)
                {
                    DefMap<RoomStatDef, float> stats = __instance.stats;
                    if (stats == null)
                        stats = new DefMap<RoomStatDef, float>();
                    foreach (RoomStatDef def in DefDatabase<RoomStatDef>.AllDefs.OrderByDescending(x => x.updatePriority))
                        stats[def] = def.Worker.GetScore(__instance);
                    __instance.role = DefDatabase<RoomRoleDef>.AllDefs.MaxBy(x => x.Worker.GetScore(__instance));
                    __instance.stats = stats;
                }
                else
                {
                    __instance.stats = null;
                    __instance.role = RoomRoleDefOf.None;
                }
            }
            return false;
        }

        public static bool AddDistrict(Room __instance, District district)
        {
            bool newRoom = false; //ADDED
            lock (__instance) //ADDED
            {
                if (__instance.districts.Contains(district))
                {
                    Log.Error(string.Concat("Tried to add the same district twice to Room. district=", district, ", room=", __instance));
                }
                else
                {
                    __instance.districts.Add(district);
                    if (__instance.districts.Count == 1)
                    {
                        newRoom = true;
                    }
                }
            }
            if (newRoom)
            {
                lock (__instance.Map.regionGrid)
                {
                    __instance.Map.regionGrid.allRooms.Add(__instance);
                }
            }
            return false;
        }
        public static bool RemoveDistrict(Room __instance, District district)
        {
            lock (__instance) //ADDED
            {
                if (!__instance.districts.Contains(district))
                {
                    Log.Error(string.Concat("Tried to remove district from Room but this district is not here. district=", district, ", room=", __instance));
                    return false;
                }

                Map map = __instance.Map;

                List<District> newDistrictList = new List<District>(__instance.districts);
                newDistrictList.Remove(district);
                __instance.districts = newDistrictList;

                if (newDistrictList.Count == 0)
                {

                    lock (map.regionGrid) //ADDED
                    {
                        List<Room> newAllRooms = new List<Room>(map.regionGrid.allRooms);
                        newAllRooms.Remove(__instance);
                        map.regionGrid.allRooms = newAllRooms;
                    }
                }

                __instance.statsAndRoleDirty = true;
            }
            return false;
        }
        public static bool Notify_RoofChanged(Room __instance)
        {
            lock (__instance)
            {
                __instance.cachedOpenRoofCount = -1;
                __instance.tempTracker.RoofChanged();
            }
            return false;
        }

    }
}
