using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimThreaded
{

    public class Room_Patch
	{

		internal static void RunDestructivePatches()
		{
			Type original = typeof(Room);
			Type patched = typeof(Room_Patch);

#if RW12
			RimThreadedHarmony.Prefix(original, patched, nameof(OpenRoofCountStopAt));
			RimThreadedHarmony.Prefix(original, patched, nameof(RemoveRegion));
			RimThreadedHarmony.Prefix(original, patched, nameof(Notify_ContainedThingSpawnedOrDespawned));
#endif
#if RW13
			RimThreadedHarmony.Prefix(original, patched, nameof(AddDistrict));
			RimThreadedHarmony.Prefix(original, patched, nameof(RemoveDistrict));
#endif
			RimThreadedHarmony.Prefix(original, patched, nameof(Notify_RoofChanged));
			RimThreadedHarmony.Prefix(original, patched, nameof(UpdateRoomStatsAndRole));

			RimThreadedHarmony.Prefix(original, patched, nameof(get_Fogged));


			RimThreadedHarmony.Prefix(original, patched, nameof(get_ContainedAndAdjacentThings));
		}

		public static bool get_ContainedAndAdjacentThings(Room __instance, ref List<Thing> __result)//fixes problems with rimfridge.
        {
			__instance.uniqueContainedThingsSet = new HashSet<Thing>();
			__instance.uniqueContainedThings = new List<Thing>();
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
					if (__instance.uniqueContainedThingsSet.Add(item))
					{
						__instance.uniqueContainedThings.Add(item);
					}
				}
			}
			__result = __instance.uniqueContainedThings;
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
#if RW12
		public static bool Notify_ContainedThingSpawnedOrDespawned(Room __instance, Thing th)
		{
			if (th.def.category == ThingCategory.Mote || th.def.category == ThingCategory.Projectile || th.def.category == ThingCategory.Ethereal || th.def.category == ThingCategory.Pawn)
			{
				return false;
			}

			if (__instance.IsDoorway)
			{
				Region regions0 = __instance.Regions[0];
				for (int i = 0; i < regions0.links.Count; i++)
				{
					Region otherRegion = regions0.links[i].GetOtherRegion(regions0);
					if (otherRegion != null && !otherRegion.IsDoorway)
					{
						Room room = otherRegion.Room;
						if(room != null)
							otherRegion.Room.Notify_ContainedThingSpawnedOrDespawned(th);
					}
				}
			}

			__instance.statsAndRoleDirty = true;
			return false;
		}
		public static bool OpenRoofCountStopAt(Room __instance, ref int __result, int threshold)
		{
			//IEnumerator<IntVec3> cachedOpenRoofState2 = __instance.Cells.GetEnumerator();
			//int cachedOpenRoofCount2 = -1;
			lock (__instance)
			{
				if (__instance.cachedOpenRoofCount == -1 && __instance.cachedOpenRoofState == null)
				{
					__instance.cachedOpenRoofCount = 0;
					__instance.cachedOpenRoofState = __instance.Cells.GetEnumerator();
				}
				if (__instance.cachedOpenRoofCount < threshold && __instance.cachedOpenRoofState != null)
				{
					RoofGrid roofGrid = __instance.Map.roofGrid;
					if (null != roofGrid)
					{
						while (__instance.cachedOpenRoofCount < threshold && __instance.cachedOpenRoofState.MoveNext())
						{
							IntVec3 currentRoofState = __instance.cachedOpenRoofState.Current;
							if (null != currentRoofState)
							{
								if (!roofGrid.Roofed(currentRoofState))
								{
									__instance.cachedOpenRoofCount++;
								}
							}
						}
						if (__instance.cachedOpenRoofCount < threshold)
						{
							__instance.cachedOpenRoofState = null;
						}
					}
				}
				__result = __instance.cachedOpenRoofCount;
			}
			return false;
		}
		public static bool RemoveRegion(Room __instance, Region r)
		{
			lock (__instance.Regions) //ADDED
			{
				if (!__instance.Regions.Contains(r))
				{
					Log.Error("Tried to remove region from Room but this region is not here. region=" + r + ", room=" + __instance);
					return false;
				}
				List<Region> newRegionList = new List<Region>(__instance.Regions);
				newRegionList.Remove(r);
				__instance.regions = newRegionList;
				if (r.touchesMapEdge)
				{
					__instance.numRegionsTouchingMapEdge--;
				}

				if (__instance.Regions.Count == 0)
				{
					__instance.Group = null;
					__instance.cachedOpenRoofCount = -1;
					__instance.cachedOpenRoofState = null;
					__instance.statsAndRoleDirty = true;
					lock (__instance.Map.regionGrid) //ADDED
					{
						List<Room> newAllRooms = new List<Room>(__instance.Map.regionGrid.allRooms);
						newAllRooms.Remove(__instance);
						__instance.Map.regionGrid.allRooms = newAllRooms;
					}
				}
			}
			return false;
		}
#endif
		public static bool UpdateRoomStatsAndRole(Room __instance)
		{
			lock (__instance)
			{
				__instance.statsAndRoleDirty = false;
#if RW12
				if (!__instance.TouchesMapEdge && __instance.RegionType == RegionType.Normal && __instance.regions.Count <= 36)
#endif
#if RW13
				if (__instance.ProperRoom && __instance.RegionCount <= 36)
#endif
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

#if RW13
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
			if(newRoom)
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
#endif
		public static bool Notify_RoofChanged(Room __instance)
		{
			lock (__instance)
			{
				__instance.cachedOpenRoofCount = -1;
#if RW12

				__instance.cachedOpenRoofState = null;
				__instance.Group.Notify_RoofChanged();
#endif
#if RW13
				__instance.tempTracker.RoofChanged();
#endif
			}
			return false;
		}

	}
}
