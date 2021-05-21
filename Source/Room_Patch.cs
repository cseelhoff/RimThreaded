using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Room_Patch
	{
		[ThreadStatic] public static HashSet<Thing> uniqueContainedThingsSet;
		[ThreadStatic] public static HashSet<Room> uniqueNeighborsSet;
		
		public static void InitializeThreadStatics()
        {
			uniqueContainedThingsSet = new HashSet<Thing>();
			uniqueNeighborsSet = new HashSet<Room>();
		}

		internal static void RunDestructivePatches()
		{
			Type original = typeof(Room);
			Type patched = typeof(Room_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched, false);
			RimThreadedHarmony.TranspileFieldReplacements(original, "get_ContainedAndAdjacentThings");
			RimThreadedHarmony.TranspileFieldReplacements(original, "get_Neighbors");
			RimThreadedHarmony.Prefix(original, patched, "OpenRoofCountStopAt");
			RimThreadedHarmony.Prefix(original, patched, "RemoveRegion");
			RimThreadedHarmony.Prefix(original, patched, "Notify_RoofChanged");
			RimThreadedHarmony.Prefix(original, patched, "Notify_RoomShapeOrContainedBedsChanged");
			RimThreadedHarmony.Prefix(original, patched, "Notify_ContainedThingSpawnedOrDespawned");
			
		}


		public static bool get_CellCount(Room __instance, ref int __result)
		{
			lock(__instance)
			{
				if (__instance.cachedCellCount == -1)
				{
					__instance.cachedCellCount = 0;
					for (int i = 0; i < __instance.Regions.Count; i++)
					{
						__instance.cachedCellCount += __instance.Regions[i].CellCount;
					}
				}

				__result = __instance.cachedCellCount;
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
		public static bool Notify_RoofChanged(Room __instance)
		{
			lock (__instance)
			{
				__instance.cachedOpenRoofCount = -1;
				__instance.cachedOpenRoofState = null;
				__instance.Group.Notify_RoofChanged();
			}
			return false;
		}
		public static bool Notify_RoomShapeOrContainedBedsChanged(Room __instance)
		{
			lock (__instance)
			{
				__instance.cachedCellCount = -1;
				__instance.cachedOpenRoofCount = -1;
				__instance.cachedOpenRoofState = null;
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
				__instance.statsAndRoleDirty = true;
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
						otherRegion.Room.Notify_ContainedThingSpawnedOrDespawned(th);
					}
				}
			}

			__instance.statsAndRoleDirty = true;
			return false;
		}
	}
}
