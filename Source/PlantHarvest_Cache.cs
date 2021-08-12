using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static RimThreaded.Area_Patch;
using static RimThreaded.JumboCell;

namespace RimThreaded
{
    class PlantHarvest_Cache
	{
		[ThreadStatic] private static HashSet<IntVec3> retrunedThings;

		public static Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingHarvestCellsMapDict = new Dictionary<Map, List<HashSet<IntVec3>[]>>();
		static readonly IntVec3[] noOffset = new IntVec3[] { IntVec3.Zero };
		public static Dictionary<(Map, IntVec3), bool> isBeingWorkedOn = new Dictionary<(Map, IntVec3), bool>();
		internal static void InitializeThreadStatics()
		{
			retrunedThings = new HashSet<IntVec3>();
		}

		public static void ReregisterObject(Map map, IntVec3 location, Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict)
		{
			//try to remove an item that has a null map. only way is to check all maps...
			if(map == null)
            {
				foreach(KeyValuePair<Map, List<HashSet<IntVec3>[]>> kv in awaitingActionMapDict)
                {
					RemoveObjectFromAwaitingActionHashSets(kv.Key, location, kv.Value);
				}
				return;
            }
			List<HashSet<IntVec3>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(awaitingActionMapDict, map);
			RemoveObjectFromAwaitingActionHashSets(map, location, awaitingActionZoomLevels);
			AddObjectToActionableObjects(map, location, awaitingActionZoomLevels);
		}
		public static void RemoveObjectFromAwaitingActionHashSets(Map map, IntVec3 location, List<HashSet<IntVec3>[]> awaitingActionZoomLevels)
		{
			int jumboCellWidth;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int cellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
				HashSet<IntVec3> hashset = awaitingActionZoomLevels[zoomLevel][cellIndex];
				if (hashset != null)
				{
					lock (hashset)
					{
						hashset.Remove(location);
					}
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

		public static IEnumerable<IntVec3> GetClosestActionableLocations(Pawn pawn, Map map, Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict)
		{
			int jumboCellWidth;
			int XposOfJumboCell;
			int ZposOfJumboCell;
			int mapSizeX = map.Size.x;
			retrunedThings.Clear();
			IntVec3[] objectsAtCellCopy;
			List<HashSet<IntVec3>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(awaitingActionMapDict, map);
			IntVec3 position = pawn.Position;
			Area effectiveAreaRestrictionInPawnCurrentMap = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
			Range2D areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
			Range2D scannedRange = new Range2D(position.x, position.z, position.x, position.z);
			for (int zoomLevel = 0; zoomLevel < awaitingActionZoomLevels.Count; zoomLevel++)
			{
				HashSet<IntVec3>[] objectsGrid = awaitingActionZoomLevels[zoomLevel];
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
				XposOfJumboCell = position.x / jumboCellWidth;
				ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
				IEnumerable<IntVec3> offsetOrder;
				if (zoomLevel == 0)
				{
					offsetOrder = noOffset;
				} else
                {
					offsetOrder = GetOffsetOrder(position, zoomLevel, scannedRange, areaRange);						
				}
				foreach (IntVec3 offset in offsetOrder)
				{
					int newXposOfJumboCell = XposOfJumboCell + offset.x;
					int newZposOfJumboCell = ZposOfJumboCell + offset.z;
					if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap && newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
					{
						int jumboCellIndex = CellXZToIndexCustom(newXposOfJumboCell, newZposOfJumboCell, jumboCellColumnsInMap);
						HashSet<IntVec3> thingsAtCell = objectsGrid[jumboCellIndex];
						if (thingsAtCell != null && thingsAtCell.Count > 0)
						{
							objectsAtCellCopy = thingsAtCell.ToArray();
							foreach (IntVec3 actionableObject in objectsAtCellCopy)
							{
								if (!retrunedThings.Contains(actionableObject) && !isBeingWorkedOn[(map, actionableObject)])
								{
									Log.Message(actionableObject.ToString());
									yield return actionableObject;
									retrunedThings.Add(actionableObject);
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

		private static List<HashSet<IntVec3>[]> GetAwaitingActionsZoomLevels(Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict, Map map)
		{
			if (!awaitingActionMapDict.TryGetValue(map, out List<HashSet<IntVec3>[]> awaitingActionsZoomLevels))
			{
				lock (awaitingActionMapDict)
				{
					if (!awaitingActionMapDict.TryGetValue(map, out List<HashSet<IntVec3>[]> awaitingActionsZoomLevels2))
					{
						Log.Message("RimThreaded is caching Harvest Plant Cells...");
						awaitingActionsZoomLevels2 = new List<HashSet<IntVec3>[]>();
						int mapSizeX = map.Size.x;
						int mapSizeZ = map.Size.z;
						int jumboCellWidth;
						int zoomLevel = 0;
						do
						{
							jumboCellWidth = getJumboCellWidth(zoomLevel);
							int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
							awaitingActionsZoomLevels2.Add(new HashSet<IntVec3>[numGridCells]);
							zoomLevel++;
						} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);

						List<Zone> zones = map.zoneManager.AllZones;
						foreach (Zone zone in zones)
						{
							if (zone is Zone_Growing)
							{
								foreach (IntVec3 c in zone.Cells)
									AddObjectToActionableObjects(zone.Map, c, awaitingActionsZoomLevels2);
							}
						}
						awaitingActionMapDict[map] = awaitingActionsZoomLevels2;
					}
					awaitingActionsZoomLevels = awaitingActionsZoomLevels2;
				}
			}
			return awaitingActionsZoomLevels;

		}
		public static void AddObjectToActionableObjects(Map map, IntVec3 location, List<HashSet<IntVec3>[]> awaitingActionZoomLevels)
		{
			int jumboCellWidth;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			//---START--- For plant Harvest
			//WorkGiver_GrowerHarvest.HasJobOnCell
			Plant plant = location.GetPlant(map);
			bool hasJobOnCell = plant != null && !plant.IsForbidden(Faction.OfPlayer) && (plant.HarvestableNow && plant.LifeStage == PlantLifeStage.Mature) && (plant.CanYieldNow());
			if (!hasJobOnCell)
            {
				return;
			}
			LocalTargetInfo localTargetInfo = new LocalTargetInfo(location);
			bool isReserved = map.physicalInteractionReservationManager.IsReserved(localTargetInfo);
			if (isReserved)
				return;

			//---END--
			if (!isBeingWorkedOn.ContainsKey((map, location)))
			{
				isBeingWorkedOn[(map, location)] = false;
			}

			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				HashSet<IntVec3>[] awaitingActionGrid = awaitingActionZoomLevels[zoomLevel];
				int jumboCellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
				HashSet<IntVec3> hashset = awaitingActionGrid[jumboCellIndex];
				if (hashset == null)
				{
					hashset = new HashSet<IntVec3>();
					lock (awaitingActionGrid)
					{
						awaitingActionGrid[jumboCellIndex] = hashset;
					}
				}
				lock (hashset)
				{
					hashset.Add(location);
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

    }

}
