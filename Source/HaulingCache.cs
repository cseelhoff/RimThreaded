using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static RimThreaded.Area_Patch;
using static Verse.AI.ReservationManager;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{
	class HaulingCache
	{
		static readonly Dictionary<Map, HashSet<Thing>[]> waitingForZoneBetterThanMapDict = new Dictionary<Map, HashSet<Thing>[]>(); //each Map has sets of Things for each storage priority (typically 6)
		public static List<int> zoomLevels = new List<int>();
		public static Dictionary<Map, List<HashSet<Thing>[]>> awaitingHaulingMapDict = new Dictionary<Map, List<HashSet<Thing>[]>>();
		public const float ZOOM_MULTIPLIER = 1.5f; //must be greater than 1. lower numbers will make searches slower, but ensure pawns find the closer things first.
												   // Map, (jumbo cell zoom level, #0 item=zoom 2x2, #1 item=4x4), jumbo cell index converted from x,z coord, HashSet<Thing>
		[ThreadStatic] private static HashSet<Thing> retrunedThings;

		internal static void InitializeThreadStatics()
        {
			retrunedThings = new HashSet<Thing>();
		}

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

		public static void RegisterHaulableItem(Thing haulableThing)
		{
			Map map = haulableThing.Map;
			if (map == null)
			{
				return;
			}

			if (haulableThing.IsForbidden(Faction.OfPlayer))
            {
				return;
            }
			//---SHOULD HELP WITH NOT HAULING ROCK CHUNKS---
			if(!haulableThing.def.EverHaulable)
            {
				return;
            }
			if (!haulableThing.def.alwaysHaulable && (map.designationManager.DesignationOn(haulableThing, DesignationDefOf.Haul) == null))
            {
				return;
            }
			//---SHOULD HELP WITH NOT HAULING ROCK CHUNKS---


			int num = haulableThing.stackCount;
			int maxPawns = 1;
			if (map.physicalInteractionReservationManager.IsReserved(haulableThing))
			{
				//Log.Warning("IsReserved");
				return;
			}
            List<Reservation> reservations = ReservationManager_Patch.getReservationTargetList(map.reservationManager, haulableThing);
			if (reservations != null && reservations.Count > 0)
            {
				return;
            }
			int num3 = 0;
			int num4 = 0;
			List<Reservation> reservationTargetList = ReservationManager_Patch.getReservationTargetList(map.reservationManager, haulableThing);
			foreach (Reservation reservation in reservationTargetList)
			{
				if (reservation.Layer == null)
				{
					if (reservation.Claimant != null && (reservation.StackCount == -1 || reservation.StackCount >= num))
					{
						break;
					}
					if (reservation.Claimant != null)
					{
						if (reservation.MaxPawns != maxPawns)
						{
							//Log.Warning("maxPawns");
							return;
						}

						num3++;
						num4 = (reservation.StackCount != -1) ? (num4 + reservation.StackCount) : (num4 + num);
						if (num3 >= maxPawns || num + num4 > num)
						{
							//Log.Warning(reservation.Claimant.ToString() + " StackCount");
							return;
						}
					}
				}
			}

			int storagePriority = (int)StoreUtility.CurrentStoragePriorityOf(haulableThing);
			if (TryFindBestBetterStoreCellFor(haulableThing, null, map, StoreUtility.CurrentStoragePriorityOf(haulableThing), null, out _, false) && //fast check
				HaulToStorageJobTest(haulableThing)) { //slower check
				AddThingToAwaitingHaulingHashSets(haulableThing);
			} else
			{
				getWaitingForZoneBetterThan(map)[storagePriority].Add(haulableThing);
			}
			
		}

		public static bool TryFindBestBetterStoreCellFor(
		  Thing t,
		  Pawn carrier,
		  Map map,
		  StoragePriority currentPriority,
		  Faction faction,
		  out IntVec3 foundCell,
		  bool needAccurateResult = true)
		{
			List<SlotGroup> listInPriorityOrder = map.haulDestinationManager.AllGroupsListInPriorityOrder;
			if (listInPriorityOrder.Count == 0)
			{
				foundCell = IntVec3.Invalid;
				return false;
			}
			StoragePriority foundPriority = currentPriority;
			float maxValue = (float)int.MaxValue;
			IntVec3 invalid = IntVec3.Invalid;
			int count = listInPriorityOrder.Count;
			for (int index = 0; index < count; ++index)
			{
				SlotGroup slotGroup = listInPriorityOrder[index];
				StoragePriority priority = slotGroup.Settings.Priority;
				if (priority >= foundPriority && priority > currentPriority)
					TryFindBestBetterStoreCellForWorker(t, carrier, map, faction, slotGroup, needAccurateResult, 
						ref invalid, ref maxValue, ref foundPriority);  //changed
				else
					break;
			}
			if (!invalid.IsValid)
			{
				foundCell = IntVec3.Invalid;
				return false;
			}
			foundCell = invalid;
			return true;
		}
		private static void TryFindBestBetterStoreCellForWorker(
		  Thing t,
		  Pawn carrier,
		  Map map,
		  Faction faction,
		  SlotGroup slotGroup,
		  bool needAccurateResult,
		  ref IntVec3 closestSlot,
		  ref float closestDistSquared,
		  ref StoragePriority foundPriority)
		{
			if (slotGroup == null || !slotGroup.parent.Accepts(t))
				return;
			IntVec3 intVec3 = t.PositionHeld; //changed
			List<IntVec3> cellsList = slotGroup.CellsList;
			int count = cellsList.Count;
			int num = !needAccurateResult ? 0 : Mathf.FloorToInt((float)count * Rand.Range(0.005f, 0.018f));
			for (int index = 0; index < count; ++index)
			{
				IntVec3 c = cellsList[index];
				float horizontalSquared = (float)(intVec3 - c).LengthHorizontalSquared;
				if ((double)horizontalSquared <= (double)closestDistSquared && StoreUtility.IsGoodStoreCell(c, map, t, carrier, faction))
				{
					closestSlot = c;
					closestDistSquared = horizontalSquared;
					foundPriority = slotGroup.Settings.Priority;
					if (index >= num)
						break;
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

			List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
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

		private static void RemoveThingFromAwaitingHaulingHashSets(Thing haulableThing)
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
				lock (awaitingHaulingMapDict)
				{
					if (!awaitingHaulingMapDict.TryGetValue(map, out List<HashSet<Thing>[]> awaitingHaulingZoomLevels2))
					{
						awaitingHaulingZoomLevels2 = new List<HashSet<Thing>[]>();
						int mapSizeX = map.Size.x;
						int mapSizeZ = map.Size.z;
						int jumboCellWidth;
						int zoomLevel = 0;
						do
						{
							jumboCellWidth = getJumboCellWidth(zoomLevel);
							int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
							awaitingHaulingZoomLevels2.Add(new HashSet<Thing>[numGridCells]);
							zoomLevel++;
						} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
						awaitingHaulingMapDict[map] = awaitingHaulingZoomLevels2;
					}
					awaitingHaulingZoomLevels = awaitingHaulingZoomLevels2;
				}
			}
			return awaitingHaulingZoomLevels;
		}


		public static void DeregisterHaulableItem(Thing haulableThing)
		{
			Map map = haulableThing.Map;
			if (map != null)
			{
				int storagePriority = (int)StoreUtility.CurrentStoragePriorityOf(haulableThing);
				getWaitingForZoneBetterThan(map)[storagePriority].Remove(haulableThing);
				RemoveThingFromAwaitingHaulingHashSets(haulableThing);
			}
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

		public static void Notify_AddedCell(SlotGroup __instance, IntVec3 c)
		{
			Map map = __instance.parent.Map;
			NewStockpileCreatedOrMadeUnfull(__instance, map);
		}

		public static void Notify_SlotGroupChanged(ListerHaulables __instance, SlotGroup sg)
		{
			Map map = __instance.map;
			if(map == null) 
				map = sg.parent.Map;
			NewStockpileCreatedOrMadeUnfull(sg, map);
		}


		public static void NewStockpileCreatedOrMadeUnfull(SlotGroup __instance, Map map)
        {
			if (Current.ProgramState == ProgramState.Playing)
			{
				//Map map = __instance.parent.Map;
				//int storagePriorityCount = Enum.GetValues(typeof(StoragePriority)).Length;
				HashSet<Thing>[] waitingForZoneBetterThanArray = getWaitingForZoneBetterThan(map);
				StorageSettings settings = __instance.Settings;

				for (int i = (int)settings.Priority; i >= 0; i--)
				{
					HashSet<Thing> waitingThings = waitingForZoneBetterThanArray[i];
					HashSet<Thing> waitingThingsCopy = new HashSet<Thing>(waitingThings);
					foreach (Thing waitingThing in waitingThingsCopy)
					{
						if (settings.AllowedToAccept(waitingThing))
						{
							waitingThings.Remove(waitingThing);
							RegisterHaulableItem(waitingThing);
						}
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
			retrunedThings.Clear(); //hashset used to ensure same item is not retured twice

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
							if (!retrunedThings.Contains(haulableThing)) {
								yield return haulableThing;
								retrunedThings.Add(haulableThing);
							}
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
								if (!retrunedThings.Contains(haulableThing))
								{
									yield return haulableThing;
									retrunedThings.Add(haulableThing);
								}
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
										if (CanReserveTest(pawn.Map.reservationManager, pawn, tryThing))
										{
											if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
											{
												if (!tryThing.def.IsNutritionGivingIngestible || !tryThing.def.ingestible.HumanEdible || tryThing.IsSociallyProper(pawn, forPrisoner: false, animalsCare: true))
												{
													if (PawnCanAutomaticallyHaulFastTest(pawn, tryThing, false))
													{
														if (HaulToStorageJobTest(tryThing))
														{
															if (scanner.HasJobOnThing(pawn, tryThing))
															{
#if DEBUG
                                                                Log.Message(pawn + " is going to haul thing: " + tryThing + " at pos " + tryThing.Position);
#endif
																thing = tryThing;
																break;
															}
															//else if (i > -40) { Log.Warning("No Hauling Job " + tryThing.ToString() + " at pos " + tryThing.Position.ToString() + " for pawn " + pawn.ToString() + " tries: " + i.ToString()); }
														}
#if DEBUG
														else if (i > -4000) { Log.Warning("Can't HaulToStorageJob " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
													}
#if DEBUG
													else if (i > -40) { Log.Warning("Can't PawnCanAutomaticallyHaulFast " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
												}
#if DEBUG
												else if (i > -40) { Log.Warning("Can't ReservedForPrisonersTrans " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
											}
#if DEBUG
											else if (i > -40) { Log.Warning("Not capable of Manipulation " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
										}
#if DEBUG
										else if (i > -40) { Log.Warning("Can't Reserve " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " reserved by: " + ReservationManager_Patch.getFirstPawnReservingTarget(pawn.Map.reservationManager, tryThing) + " tries: " + i); }
#endif
									}
#if DEBUG
									else if (i > -40) { Log.Warning("Can't Haul unfinishedThing " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
								}
#if DEBUG
								else if (i > -40) { Log.Warning("Can't PawnCanAutomaticallyHaulFast " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
							}
#if DEBUG
							else if(i > -40) { Log.Warning("Can't Reserve " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
						}
#if DEBUG
						else if (i > -40) { Log.Warning("Not Allowed " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
					}
#if DEBUG
					else if (i > -40) { Log.Warning("Not Spawned " + tryThing + " at pos " + tryThing.Position + " for pawn " + pawn + " tries: " + i); }
#endif
					i++;
					ReregisterHaulableItem(tryThing);
				}
#if DEBUG
				if (i > 0)
				{
					Log.Warning("took more than 0 haulable tries: " + i);
				}
#endif
            }
			return thing;
		}
		public static bool PawnCanAutomaticallyHaulFastTest(Pawn p, Thing t, bool forced)
		{
			UnfinishedThing unfinishedThing = t as UnfinishedThing;
			Building building;
			if (unfinishedThing != null && unfinishedThing.BoundBill != null && ((building = (unfinishedThing.BoundBill.billStack.billGiver as Building)) == null || (building.Spawned && building.OccupiedRect().ExpandedBy(1).Contains(unfinishedThing.Position))))
			{
#if DEBUG
				Log.Warning("unfinishedThing");
#endif
				return false;
			}

			if (!p.CanReach(t, PathEndMode.ClosestTouch, p.NormalMaxDanger()))
			{
#if DEBUG
				Log.Warning("CanReach");
#endif
				return false;
			}

			//if (!p.CanReserve(t, 1, -1, null, forced))
			//if (!p.Map.reservationManager.CanReserve(p, t, 1, -1, null, false))
			if (!p.Map.reservationManager.CanReserve(p, t))
			{
#if DEBUG
				Log.Warning(CanReserveTest(p.Map.reservationManager, p, t).ToString());
				Log.Warning("CanReserve");
#endif
				return false;
			}

			if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
#if DEBUG
				Log.Warning("CapableOf");
#endif
				return false;
			}

			if (t.def.IsNutritionGivingIngestible && t.def.ingestible.HumanEdible && !t.IsSociallyProper(p, forPrisoner: false, animalsCare: true))
			{
#if DEBUG
				Log.Warning("IsNutritionGivingIngestible");
#endif
				return false;
			}

			if (t.IsBurning())
			{
#if DEBUG
				Log.Warning("IsBurning");
#endif
				return false;
			}

			return true;
		}

		public static bool CanReserveTest(ReservationManager __instance, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			Map mapInstance = claimant.Map;

			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant");
				return false;
			}

			if (!claimant.Spawned || claimant.Map != mapInstance)
			{
#if DEBUG
				Log.Warning("Spawned");
#endif
				return false;
			}

			if (!target.IsValid || target.ThingDestroyed)
			{
#if DEBUG
				Log.Warning("IsValid");
#endif
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != mapInstance)
			{
#if DEBUG
				Log.Warning("HasThing");
#endif
				return false;
			}

			int num = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = (stackCount == -1) ? num : stackCount;
			if (num2 > num)
			{
#if DEBUG
				Log.Warning("num2");
#endif
				return false;
			}

			if (!ignoreOtherReservations)
			{
				if (mapInstance.physicalInteractionReservationManager.IsReserved(target) && !mapInstance.physicalInteractionReservationManager.IsReservedBy(claimant, target))
				{
#if DEBUG
					Log.Warning("IsReserved");
#endif
					return false;
				}
				int num3 = 0;
				int num4 = 0;
				List<Reservation> reservationTargetList = ReservationManager_Patch.getReservationTargetList(__instance, target);
				foreach (Reservation reservation in reservationTargetList)
				{
					if (reservation.Layer == layer)
					{
						if (reservation.Claimant == claimant && (reservation.StackCount == -1 || reservation.StackCount >= num2))
						{
							return true;
						}
						if (reservation.Claimant != claimant && ReservationManager_Patch.RespectsReservationsOf(claimant, reservation.Claimant))
						{
							if (reservation.MaxPawns != maxPawns)
							{
#if DEBUG
								Log.Warning("maxPawns");
#endif
								return false;
							}

							num3++;
							num4 = (reservation.StackCount != -1) ? (num4 + reservation.StackCount) : (num4 + num);
							if (num3 >= maxPawns || num2 + num4 > num)
							{
#if DEBUG
								Log.Warning(reservation.Claimant  + " StackCount");
#endif
								return false;
							}
						}
					}
				}
			}

			return true;
		}




		public static bool HaulToStorageJobTest(Thing t)
		{
			StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(t);
			if (!TryFindBestBetterStorageForTest(t, t.Map, currentPriority, Faction.OfPlayer))
			{
				//Log.Warning("!TryFindBestBetterStorageForTest");
				return false;
			}
			return true;
		}
		public static bool TryFindBestBetterStorageForTest(Thing t, Map map, StoragePriority currentPriority, Faction faction, bool needAccurateResult = true)
		{
			if (!TryFindBestBetterStoreCellForTest(t, map, currentPriority, faction, needAccurateResult) && 
				!TryFindBestBetterNonSlotGroupStorageForTest(t, map, currentPriority, faction, out IHaulDestination _))
			{
				//Log.Warning("!TryFindBestBetterStoreCellForTest && !TryFindBestBetterNonSlotGroupStorageForTest");
				return false;

			}

			return true;
		}


		public static bool TryFindBestBetterStoreCellForTest(Thing t, Map map, StoragePriority currentPriority, Faction faction, bool needAccurateResult = true)
		{
			List<SlotGroup> allGroupsListInPriorityOrder = map.haulDestinationManager.AllGroupsListInPriorityOrder;
			if (allGroupsListInPriorityOrder.Count == 0)
			{
				//Log.Warning("allGroupsListInPriorityOrder.Count == 0");
				return false;
			}

			StoragePriority foundPriority = currentPriority;
			float closestDistSquared = 2.14748365E+09f;
			IntVec3 closestSlot = IntVec3.Invalid;
			int count = allGroupsListInPriorityOrder.Count;
			for (int i = 0; i < count; i++)
			{
				SlotGroup slotGroup = allGroupsListInPriorityOrder[i];
				StoragePriority priority = slotGroup.Settings.Priority;
				if ((int)priority < (int)foundPriority || (int)priority <= (int)currentPriority)
				{
					break;
				}

				TryFindBestBetterStoreCellForWorkerTest(t, map, faction, slotGroup, needAccurateResult, ref closestSlot, ref closestDistSquared, ref foundPriority);
			}

			if (!closestSlot.IsValid)
			{
				//Log.Warning("!TryFindBestBetterStoreCellForWorker");
				return false;
			}

			return true;
		}
		private static void TryFindBestBetterStoreCellForWorkerTest(Thing t, Map map, Faction faction, SlotGroup slotGroup, bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared, ref StoragePriority foundPriority)
		{
			if (slotGroup == null || !slotGroup.parent.Accepts(t))
			{
				return;
			}

			IntVec3 a = t.PositionHeld;
			List<IntVec3> cellsList = slotGroup.CellsList;
			int count = cellsList.Count;
			int num = needAccurateResult ? Mathf.FloorToInt(count * Rand.Range(0.005f, 0.018f)) : 0;
			for (int i = 0; i < count; i++)
			{
				IntVec3 intVec = cellsList[i];
				float num2 = (a - intVec).LengthHorizontalSquared;
				if (!(num2 > closestDistSquared) && IsGoodStoreCellTest(intVec, map, t, faction))
				{
					closestSlot = intVec;
					closestDistSquared = num2;
					foundPriority = slotGroup.Settings.Priority;
					if (i >= num)
					{
						break;
					}
				}
			}
		}
		public static bool TryFindBestBetterNonSlotGroupStorageForTest(Thing t, Map map, StoragePriority currentPriority, Faction faction, out IHaulDestination haulDestination, bool acceptSamePriority = false)
		{
			List<IHaulDestination> allHaulDestinationsListInPriorityOrder = map.haulDestinationManager.AllHaulDestinationsListInPriorityOrder;
			IntVec3 intVec = t.PositionHeld;
			float num = float.MaxValue;
			StoragePriority storagePriority = StoragePriority.Unstored;
			haulDestination = null;
			for (int i = 0; i < allHaulDestinationsListInPriorityOrder.Count; i++)
			{
				if (allHaulDestinationsListInPriorityOrder[i] is ISlotGroupParent)
				{
					continue;
				}

				StoragePriority priority = allHaulDestinationsListInPriorityOrder[i].GetStoreSettings().Priority;
				if ((int)priority < (int)storagePriority || (acceptSamePriority && (int)priority < (int)currentPriority) || (!acceptSamePriority && (int)priority <= (int)currentPriority))
				{
					break;
				}

				float num2 = intVec.DistanceToSquared(allHaulDestinationsListInPriorityOrder[i].Position);
				if (num2 > num || !allHaulDestinationsListInPriorityOrder[i].Accepts(t))
				{
					continue;
				}

				Thing thing = allHaulDestinationsListInPriorityOrder[i] as Thing;
				if (thing != null && thing.Faction != faction)
				{
					continue;
				}

				if (thing != null)
				{
					if (faction != null && thing.IsForbidden(faction))
					{
						continue;
					}
				}

				if (thing != null)
				{
					if (faction != null && map.reservationManager.IsReservedByAnyoneOf(thing, faction))
					{
						continue;
					}
				}

				num = num2;
				storagePriority = priority;
				haulDestination = allHaulDestinationsListInPriorityOrder[i];
			}

			return haulDestination != null;
		}

		private static readonly MethodInfo methodNoStorageBlockersIn =
			Method(typeof(StoreUtility), "NoStorageBlockersIn", new Type[] { typeof(IntVec3), typeof(Map), typeof(Thing) });
		private static readonly Func<IntVec3, Map, Thing, bool> funcNoStorageBlockersIn =
			(Func<IntVec3, Map, Thing, bool>)Delegate.CreateDelegate(
				typeof(Func<IntVec3, Map, Thing, bool>), methodNoStorageBlockersIn);


		public static bool IsGoodStoreCellTest(IntVec3 c, Map map, Thing t, Faction faction)
		{
			if (t.IsForbidden(Faction.OfPlayer))
			{
				//Log.Warning("IsForbidden");
				return false;
			}

			if (!funcNoStorageBlockersIn(c, map, t))
			{
				//Log.Warning("funcNoStorageBlockersIn");
				return false;
			}
			
			if (faction != null && map.reservationManager.IsReservedByAnyoneOf(c, faction))
			{
				//Log.Warning("IsReservedByAnyoneOf");
				return false;
			}

			if (c.ContainsStaticFire(map))
			{
				//Log.Warning("ContainsStaticFire");
				return false;
			}

			List<Thing> thingList = c.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i] is IConstructible && GenConstruct.BlocksConstruction(thingList[i], t))
				{
					//Log.Warning("BlocksConstruction");
					return false;
				}
			}

			return true;
		}
	}
}
