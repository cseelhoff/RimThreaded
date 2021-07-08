using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
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

				if (__instance.districts.Count == 0)
				{

					lock (__instance.Map.regionGrid) //ADDED
					{
						List<Room> newAllRooms = new List<Room>(__instance.Map.regionGrid.allRooms);
						newAllRooms.Remove(__instance);
						__instance.Map.regionGrid.allRooms = newAllRooms;
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
