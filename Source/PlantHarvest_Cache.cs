using System;
using System.Collections.Generic;
using Verse;
using static RimThreaded.Area_Patch;
using static RimThreaded.JumboCell;

namespace RimThreaded
{
    class PlantHarvest_Cache
	{
		private static readonly List<int> zoomLevels = new List<int>();
		private const float ZOOM_MULTIPLIER = 1.5f;
		[ThreadStatic] private static HashSet<IntVec3> retrunedThings;
		public static object cache_lock = new object();

		public static IEnumerable<IntVec3> GetClosestActionableLocations(Pawn pawn, Map map, Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict)
		{
			int jumboCellWidth;
			int XposOfJumboCell;
			int ZposOfJumboCell;
			int cellIndex;
			int mapSizeX = map.Size.x;
			if (retrunedThings == null)
			{
				retrunedThings = new HashSet<IntVec3>();
			}
			else
			{
				retrunedThings.Clear();
			}
			HashSet<IntVec3> objectsAtCellCopy;
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
				if (zoomLevel == 0)
				{
					cellIndex = CellToIndexCustom(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth);
					HashSet<IntVec3> objectsAtCell = objectsGrid[cellIndex];
					if (objectsAtCell != null && objectsAtCell.Count > 0)
					{
						objectsAtCellCopy = new HashSet<IntVec3>(objectsAtCell);
						foreach (IntVec3 actionalbeObject in objectsAtCellCopy)
						{
							if (!retrunedThings.Contains(actionalbeObject))
							{
								yield return actionalbeObject;
								retrunedThings.Add(actionalbeObject);
							}
						}
					}
				}
				IEnumerable<IntVec3> offsetOrder = GetOffsetOrder(position, zoomLevel, scannedRange, areaRange);
				foreach (IntVec3 offset in offsetOrder)
				{
					int newXposOfJumboCell = XposOfJumboCell + offset.x;
					int newZposOfJumboCell = ZposOfJumboCell + offset.z;
					if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap && newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
					{ //assuming square map
						HashSet<IntVec3> thingsAtCell = objectsGrid[CellToIndexCustom(newXposOfJumboCell, newZposOfJumboCell, jumboCellColumnsInMap)];
						if (thingsAtCell != null && thingsAtCell.Count > 0)
						{
							objectsAtCellCopy = new HashSet<IntVec3>(thingsAtCell);
							foreach (IntVec3 actionalbeObject in objectsAtCellCopy)
							{
								if (!retrunedThings.Contains(actionalbeObject))
								{
									yield return actionalbeObject;
									retrunedThings.Add(actionalbeObject);
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
						Log.Message("Caching Plant Cells...");
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
	}
}
