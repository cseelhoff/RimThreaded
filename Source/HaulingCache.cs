using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static RimThreaded.Area_Patch;

namespace RimThreaded
{
	class HaulingCache
	{
		static readonly Dictionary<Map, HashSet<Thing>[]> waitingForZoneBetterThanMapDict = new Dictionary<Map, HashSet<Thing>[]>(); //each Map has sets of Things for each storage priority (typically 6)
		public static List<int> zoomLevels = new List<int>();
		public static Dictionary<Map, List<HashSet<Thing>[]>> awaitingHaulingMapDict = new Dictionary<Map, List<HashSet<Thing>[]>>();
		public const float ZOOM_MULTIPLIER = 1.5f; //must be greater than 1. lower numbers will make searches slower, but ensure pawns find the closer things first.
												   // Map, (jumbo cell zoom level, #0 item=zoom 2x2, #1 item=4x4), jumbo cell index converted from x,z coord, HashSet<Thing>


		private static int getJumboCellWidth(int zoomLevel)
        {
			if (zoomLevels.Count <= zoomLevel)
			{
				int lastZoomLevel = 1;
				for (int i = zoomLevels.Count; i <= zoomLevel; i++)
				{
					if (i > 0)
						lastZoomLevel = zoomLevels[i - 1];
					zoomLevels.Add(Mathf.CeilToInt(lastZoomLevel * ZOOM_MULTIPLIER));
				}
			}
			return zoomLevels[zoomLevel];
		}

		public static void RegisterHaulableItem(Thing haulableThing) {
			Map map = haulableThing.Map;
			if (!map.physicalInteractionReservationManager.IsReserved(haulableThing))
			{
				int storagePriority = (int)StoreUtility.CurrentStoragePriorityOf(haulableThing);
				if (StoreUtility.TryFindBestBetterStoreCellFor(haulableThing, null, map, StoreUtility.CurrentStoragePriorityOf(haulableThing), null, out _, false)) {
					AddThingToAwaitingHaulingHashSets(haulableThing);
				} else
				{
					getWaitingForZoneBetterThan(map)[storagePriority].Add(haulableThing);
				}
			}
		}

		private static void AddThingToAwaitingHaulingHashSets(Thing haulableThing)
		{
			int jumboCellWidth;
			Map map = haulableThing.Map;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;

			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
				HashSet<Thing>[] awaitingHaulingGrid = awaitingHaulingZoomLevels[zoomLevel];
				int jumboCellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, jumboCellWidth);
				HashSet<Thing> hashset = awaitingHaulingGrid[jumboCellIndex];
				if (hashset == null)
				{
					hashset = new HashSet<Thing>();
					awaitingHaulingGrid[jumboCellIndex] = hashset;
				}
				hashset.Add(haulableThing);
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

		private static void RemoveThingToAwaitingHaulingHashSets(Thing haulableThing)
		{
			int jumboCellWidth;
			Map map = haulableThing.Map;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int cellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, jumboCellWidth);
				HashSet<Thing> hashset = awaitingHaulingZoomLevels[zoomLevel][cellIndex];
				if (hashset != null)
				{
					hashset.Remove(haulableThing);
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

		private static List<HashSet<Thing>[]> GetAwaitingHauling(Map map)
		{
			if (!awaitingHaulingMapDict.TryGetValue(map, out List<HashSet<Thing>[]> awaitingHaulingZoomLevels))
			{
				awaitingHaulingZoomLevels = new List<HashSet<Thing>[]>();
				int mapSizeX = map.Size.x;
				int mapSizeZ = map.Size.z;
				int jumboCellWidth;
				int zoomLevel = 0;
				do
				{
					jumboCellWidth = getJumboCellWidth(zoomLevel);
					int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
					awaitingHaulingZoomLevels.Add(new HashSet<Thing>[numGridCells]);
					zoomLevel++;
				} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
				awaitingHaulingMapDict[map] = awaitingHaulingZoomLevels;
			}
			return awaitingHaulingZoomLevels;
		}


		public static void DeregisterHaulableItem(Thing haulableThing)
		{
			int storagePriority = (int)StoreUtility.CurrentStoragePriorityOf(haulableThing);
			getWaitingForZoneBetterThan(haulableThing.Map)[storagePriority].Remove(haulableThing);
			RemoveThingToAwaitingHaulingHashSets(haulableThing);
		}
		private static HashSet<Thing>[] getWaitingForZoneBetterThan(Map map)
        {
			if(!waitingForZoneBetterThanMapDict.TryGetValue(map, out HashSet<Thing>[] waitingForZoneBetterThan)) {
                int storagePriorityCount = Enum.GetValues(typeof(StoragePriority)).Length;
				waitingForZoneBetterThan = new HashSet<Thing>[storagePriorityCount];
				for(int i = 0; i < storagePriorityCount; i++)
                {
					waitingForZoneBetterThan[i] = new HashSet<Thing>();
				}
				waitingForZoneBetterThanMapDict[map] = waitingForZoneBetterThan;
			}
			return waitingForZoneBetterThan;
		}
		public static void NewStockpileCreatedOrMadeUnfull(SlotGroup __instance)
        {
			Map map = __instance.parent.Map;
			int storagePriorityCount = Enum.GetValues(typeof(StoragePriority)).Length;
			HashSet<Thing>[] waitingForZoneBetterThanArray = getWaitingForZoneBetterThan(map);
            StorageSettings settings = __instance.Settings;

			for (int i = (int)settings.Priority; i < storagePriorityCount; i++)
            {
                HashSet<Thing> waitingThings = waitingForZoneBetterThanArray[i];
                HashSet<Thing> waitingThingsCopy = new HashSet<Thing>(waitingThings);
				foreach(Thing waitingThing in waitingThingsCopy)
                {
					if(settings.AllowedToAccept(waitingThing))
                    {
						waitingThings.Remove(waitingThing);
						RegisterHaulableItem(waitingThing);
					}
                }
			}
		}

		public static void ReregisterHaulableItem(Thing haulableThing)
        {
				DeregisterHaulableItem(haulableThing);
				RegisterHaulableItem(haulableThing);
		}

        private static IEnumerable<Thing> GetClosestHaulableItems(Pawn pawn, Map map)
        {
			int jumboCellWidth;
			int XposOfJumboCell;
			int ZposOfJumboCell;
			int cellIndex;
			int mapSizeX = map.Size.x;
			HashSet<Thing> thingsAtCellCopy;
			List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
			IntVec3 position = pawn.Position;
			Area effectiveAreaRestrictionInPawnCurrentMap = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
			Range2D areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
			Range2D scannedRange = new Range2D(position.x, position.z, position.x, position.z);
			for (int zoomLevel = 0; zoomLevel < awaitingHaulingZoomLevels.Count; zoomLevel++)
            {
				HashSet<Thing>[] thingsGrid = awaitingHaulingZoomLevels[zoomLevel];
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				//Log.Message("Searching with jumboCellSize of: " + jumboCellWidth.ToString());
				int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
				XposOfJumboCell = position.x / jumboCellWidth;
				ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
				if (zoomLevel == 0)
				{
					cellIndex = CellToIndexCustom(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth);
					HashSet<Thing> thingsAtCell = thingsGrid[cellIndex];
					if (thingsAtCell != null && thingsAtCell.Count > 0)
					{
						thingsAtCellCopy = new HashSet<Thing>(thingsAtCell);
						foreach (Thing haulableThing in thingsAtCellCopy)
						{
							yield return haulableThing;
						}
					}
				}
				IEnumerable<IntVec3> offsetOrder = GetOffsetOrder(position, zoomLevel, scannedRange, areaRange);
				foreach(IntVec3 offset in offsetOrder)
                {
					int newXposOfJumboCell = XposOfJumboCell + offset.x;
					int newZposOfJumboCell = ZposOfJumboCell + offset.z;
					if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap && newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap) { //assuming square map
						HashSet<Thing> thingsAtCell = thingsGrid[CellToIndexCustom(newXposOfJumboCell, newZposOfJumboCell, jumboCellColumnsInMap)];
						if (thingsAtCell != null && thingsAtCell.Count > 0)
						{
							thingsAtCellCopy = new HashSet<Thing>(thingsAtCell);
							foreach (Thing haulableThing in thingsAtCellCopy)
							{
								yield return haulableThing;
							}
						}
					}
				}
				scannedRange.minX = Math.Min(scannedRange.minX, (XposOfJumboCell - 1) * jumboCellWidth);
				scannedRange.minZ = Math.Min(scannedRange.minZ, (ZposOfJumboCell - 1) * jumboCellWidth);
				scannedRange.maxX = Math.Max(scannedRange.maxX, ((XposOfJumboCell + 2) * jumboCellWidth) - 1);
				scannedRange.maxZ = Math.Max(scannedRange.maxZ, ((ZposOfJumboCell + 2) * jumboCellWidth) - 1);
			}
		}

        private static IEnumerable<IntVec3> GetOffsetOrder(IntVec3 position, int zoomLevel, Range2D scannedRange, Range2D areaRange)
        {
			//TODO optimize direction to scan first
			if (scannedRange.maxZ < areaRange.maxZ)
				yield return IntVec3.North;
			
			if (scannedRange.maxZ < areaRange.maxZ && scannedRange.maxX < areaRange.maxX)
				yield return IntVec3.NorthEast;

			if (scannedRange.maxX < areaRange.maxX)
				yield return IntVec3.East;

			if (scannedRange.minZ > areaRange.minZ && scannedRange.maxX < areaRange.maxX)
				yield return IntVec3.SouthEast;

			if (scannedRange.minZ > areaRange.minZ)
				yield return IntVec3.South;

			if (scannedRange.minZ > areaRange.minZ && scannedRange.minX > areaRange.minX)
				yield return IntVec3.SouthWest;

			if (scannedRange.maxX < areaRange.maxX)
				yield return IntVec3.West;

			if (scannedRange.maxZ < areaRange.maxZ && scannedRange.minX > areaRange.minX)
				yield return IntVec3.NorthWest;

		}
		private static int CellToIndexCustom(IntVec3 position, int mapSizeX, int jumboCellWidth)
		{
			int XposInJumboCell = position.x / jumboCellWidth;
			int ZposInJumboCell = position.z / jumboCellWidth;
			int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
			return CellToIndexCustom(XposInJumboCell, ZposInJumboCell, jumboCellColumnsInMap);
		}
		private static int CellToIndexCustom(int XposOfJumboCell, int ZposOfJumboCell, int jumboCellColumnsInMap)
		{
			return (jumboCellColumnsInMap * ZposOfJumboCell) + XposOfJumboCell;
		}
		private static int GetJumboCellColumnsInMap(int mapSizeX, int jumboCellWidth)
		{
			return Mathf.CeilToInt(mapSizeX / (float)jumboCellWidth);
		}
		private static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int jumboCellWidth)
		{
			return GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth) * Mathf.CeilToInt(mapSizeZ / (float)jumboCellWidth);
		}






		public static Thing ClosestThingReachable(Pawn pawn, WorkGiver_Scanner scanner, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
		{
			bool flag = searchRegionsMax < 0 || forceAllowGlobalSearch;
			if (!flag && customGlobalSearchSet != null)
			{
				Log.ErrorOnce("searchRegionsMax >= 0 && customGlobalSearchSet != null && !forceAllowGlobalSearch. customGlobalSearchSet will never be used.", 634984);
			}

			if (!flag && !thingReq.IsUndefined && !thingReq.CanBeFoundInRegion)
			{
				Log.ErrorOnce(string.Concat("ClosestThingReachable with thing request group ", thingReq.group, " and global search not allowed. This will never find anything because this group is never stored in regions. Either allow global search or don't call this method at all."), 518498981);
				return null;
			}

			Thing thing = null;
			bool flag2 = false;

			if (thing == null && flag && !flag2)
			{
				if (traversableRegionTypes != RegionType.Set_Passable)
				{
					Log.ErrorOnce("ClosestThingReachable had to do a global search, but traversableRegionTypes is not set to passable only. It's not supported, because Reachability is based on passable regions only.", 14384767);
				}
				IntVec3 root = pawn.Position;
				IEnumerable<Thing> searchSet = GetClosestHaulableItems(pawn, map);
				int i = 0;
				foreach (Thing tryThing in searchSet)
				{
					if (tryThing.Spawned)
					{
						if (!tryThing.IsForbidden(pawn))
						{
							if (!map.physicalInteractionReservationManager.IsReserved(tryThing) || map.physicalInteractionReservationManager.IsReservedBy(pawn, tryThing))
							{
								UnfinishedThing unfinishedThing = tryThing as UnfinishedThing;
								Building building;
								if (!(unfinishedThing != null && unfinishedThing.BoundBill != null && ((building = (unfinishedThing.BoundBill.billStack.billGiver as Building)) == null || (building.Spawned && building.OccupiedRect().ExpandedBy(1).Contains(unfinishedThing.Position)))))
								{
									if (pawn.CanReach(tryThing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
									{
										if (pawn.CanReserve(tryThing, 1, -1, null, false))
										{
											if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
											{
												if (!tryThing.def.IsNutritionGivingIngestible || !tryThing.def.ingestible.HumanEdible || tryThing.IsSociallyProper(pawn, forPrisoner: false, animalsCare: true))
												{
													if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, tryThing, false))
													{
														if (HaulAIUtility.HaulToStorageJob(pawn, tryThing) != null)
														{
															if (scanner.HasJobOnThing(pawn, tryThing))
															{
																Log.Message(pawn.ToString() + " is going to haul thing: " + tryThing.ToString() + " at pos " + tryThing.Position.ToString());
																thing = tryThing;
																break;
															}
															else if (i > 400) { Log.Warning("No Job " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
														}
														else if (i > 400) { Log.Warning("Can't HaulToStorageJob " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
													}
													else if (i > 400) { Log.Warning("Can't PawnCanAutomaticallyHaulFast " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
												}
												else if (i > 400) { Log.Warning("Can't ReservedForPrisonersTrans " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
											}
											else if (i > 400) { Log.Warning("Not capable of Manipulation " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
										}
										else if (i > 400) { Log.Warning("Can't Reserve " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
									}
									else if (i > 400) { Log.Warning("Can't Haul unfinishedThing " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
								}								
								else if (i > 400) { Log.Warning("Can't PawnCanAutomaticallyHaulFast " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
							} //else if(i > -40) { Log.Warning("Can't Reserve " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
						} //else if (i > -40) { Log.Warning("Not Allowed " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
					} else if (i > -40) { Log.Warning("Not Spawned " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
					i++;
					ReregisterHaulableItem(tryThing);
				}
				if (i > 400)
				{
					Log.Warning("took more than 400 haulable tries: " + i.ToString());
				}
			}
			return thing;
		}

	}
}
