using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Room_Patch
	{
		[ThreadStatic]
		private static HashSet<Thing> uniqueContainedThingsSet;
		[ThreadStatic]
		private static HashSet<Room> uniqueNeighborsSet;

		public static AccessTools.FieldRef<Room, int> cachedOpenRoofCount =
			AccessTools.FieldRefAccess<Room, int>("cachedOpenRoofCount");
		public static AccessTools.FieldRef<Room, int> cachedCellCount =
			AccessTools.FieldRefAccess<Room, int>("cachedCellCount");
		public static AccessTools.FieldRef<Room, int> numRegionsTouchingMapEdge =
			AccessTools.FieldRefAccess<Room, int>("numRegionsTouchingMapEdge");
		public static AccessTools.FieldRef<Room, bool> statsAndRoleDirty =
			AccessTools.FieldRefAccess<Room, bool>("statsAndRoleDirty");
		public static AccessTools.FieldRef<Room, IEnumerator<IntVec3>> cachedOpenRoofState =
			AccessTools.FieldRefAccess<Room, IEnumerator<IntVec3>>("cachedOpenRoofState");
		public static AccessTools.FieldRef<Room, List<Region>> regions =
			AccessTools.FieldRefAccess<Room, List<Region>>("regions");

		public static object roomLock = new object();

		public static bool get_ContainedAndAdjacentThings(Room __instance, ref List<Thing> __result)
		{
			if(uniqueContainedThingsSet == null)
            {
				uniqueContainedThingsSet = new HashSet<Thing>();
			} else
            {
				uniqueContainedThingsSet.Clear();
			}
			List<Thing> uniqueContainedThings = new List<Thing>();
			for (int i = 0; i < regions(__instance).Count; i++)
			{
				List<Thing> allThings = regions(__instance)[i].ListerThings.AllThings;
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
		public static bool get_Neighbors(Room __instance, ref List<Room> __result)
		{
			if(uniqueNeighborsSet == null)
            {
				uniqueNeighborsSet = new HashSet<Room>();
			} else
            {
				uniqueNeighborsSet.Clear();
			}
			List<Room> uniqueNeighbors = new List<Room>();            
            List<Region> regionsList = regions(__instance);
			for (int i = 0; i < regionsList.Count; i++)
			{
				foreach (Region neighbor in regionsList[i].Neighbors)
				{
					if (uniqueNeighborsSet.Add(neighbor.Room) && neighbor.Room != __instance)
					{
						uniqueNeighbors.Add(neighbor.Room);
					}
				}
			}
			__result = uniqueNeighbors;
			return false;
			
		}



		public static bool get_CellCount(Room __instance, ref int __result)
		{
			lock(__instance)
			{
				if (cachedCellCount(__instance) == -1)
				{
					cachedCellCount(__instance) = 0;
					for (int i = 0; i < __instance.Regions.Count; i++)
					{
						cachedCellCount(__instance) += __instance.Regions[i].CellCount;
					}
				}

				__result = cachedCellCount(__instance);
				return true;
			}
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

				__instance.Regions.Remove(r);
				if (r.touchesMapEdge)
				{
					numRegionsTouchingMapEdge(__instance)--;
				}

				if (__instance.Regions.Count == 0)
				{
					__instance.Group = null;
					cachedOpenRoofCount(__instance) = -1;
					cachedOpenRoofState(__instance) = null;
					statsAndRoleDirty(__instance) = true;
					lock (__instance.Map.regionGrid.allRooms) //ADDED
					{
						__instance.Map.regionGrid.allRooms.Remove(__instance);
					}
				}
			}
			return false;
		}
		public static bool Notify_RoofChanged(Room __instance)
		{
			lock (__instance)
			{
				cachedOpenRoofCount(__instance) = -1;
				cachedOpenRoofState(__instance) = null;
				__instance.Group.Notify_RoofChanged();
			}
			return false;
		}
		public static bool Notify_RoomShapeOrContainedBedsChanged(Room __instance)
		{
			lock (__instance)
			{
				cachedCellCount(__instance) = -1;
				cachedOpenRoofCount(__instance) = -1;
				cachedOpenRoofState(__instance) = null;
				if (Current.ProgramState == ProgramState.Playing && !__instance.Fogged)
				{
					__instance.Map.autoBuildRoofAreaSetter.TryGenerateAreaFor(__instance);
				}

				__instance.isPrisonCell = false;
				if (Building_Bed.RoomCanBePrisonCell(__instance))
				{
					List<Thing> containedAndAdjacentThings = __instance.ContainedAndAdjacentThings;
					for (int i = 0; i < containedAndAdjacentThings.Count; i++)
					{
						Building_Bed building_Bed = containedAndAdjacentThings[i] as Building_Bed;
						if (building_Bed != null && building_Bed.ForPrisoners)
						{
							__instance.isPrisonCell = true;
							break;
						}
					}
				}

				List<Thing> list = __instance.Map.listerThings.ThingsOfDef(ThingDefOf.NutrientPasteDispenser);
				for (int j = 0; j < list.Count; j++)
				{
					list[j].Notify_ColorChanged();
				}

				if (Current.ProgramState == ProgramState.Playing && __instance.isPrisonCell)
				{
					foreach (Building_Bed containedBed in __instance.ContainedBeds)
					{
						containedBed.ForPrisoners = true;
					}
				}

				__instance.lastChangeTick = Find.TickManager.TicksGame;
				statsAndRoleDirty(__instance) = true;
				FacilitiesUtility.NotifyFacilitiesAboutChangedLOSBlockers(__instance.Regions);
			}
			return false;
		}

		public static bool OpenRoofCountStopAt(Room __instance, ref int __result, int threshold)
		{
			//IEnumerator<IntVec3> cachedOpenRoofState2 = __instance.Cells.GetEnumerator();
			//int cachedOpenRoofCount2 = -1;
			lock (__instance)
			{
				if (cachedOpenRoofCount(__instance) == -1 && cachedOpenRoofState(__instance) == null)
				{
					cachedOpenRoofCount(__instance) = 0;
					cachedOpenRoofState(__instance) = __instance.Cells.GetEnumerator();
				}
				if (cachedOpenRoofCount(__instance) < threshold && cachedOpenRoofState(__instance) != null)
				{
					RoofGrid roofGrid = __instance.Map.roofGrid;
					if (null != roofGrid)
					{
						while (cachedOpenRoofCount(__instance) < threshold && cachedOpenRoofState(__instance).MoveNext())
						{
							IntVec3 currentRoofState = cachedOpenRoofState(__instance).Current;
							if (null != currentRoofState)
							{
								if (!roofGrid.Roofed(currentRoofState))
								{
									cachedOpenRoofCount(__instance)++;
								}
							}
						}
						if (cachedOpenRoofCount(__instance) < threshold)
						{
							cachedOpenRoofState(__instance) = null;
						}
					}
				}
				__result = cachedOpenRoofCount(__instance);
			}
			return false;
		}
		public static bool get_Temperature(Room __instance, ref float __result)
		{
			float temperature = 0f;
			try
			{
				RoomGroup group = __instance.Group;
				temperature = group.Temperature;
			}
			catch(NullReferenceException)
			{
				__result = 0f;
			}
			__result = temperature;
			return false;
		}
		public static bool get_PsychologicallyOutdoors(Room __instance, ref bool __result)
		{
			if (__instance.OpenRoofCountStopAt(300) >= 300)
			{
				__result = true;
				return false;
			}

			RoomGroup roomGroup = __instance.Group;
			if (roomGroup !=null && roomGroup.AnyRoomTouchesMapEdge && __instance.OpenRoofCount / (float)__instance.CellCount >= 0.5f)
			{
				__result = true;
				return false;
			}
			__result = false;
			return false;
		}

	}
}
