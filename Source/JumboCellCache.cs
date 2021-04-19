using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimThreaded.Area_Patch;

namespace RimThreaded
{
    static class JumboCellCache
    {
		
		private static readonly List<int> zoomLevels = new List<int>();
		private const float ZOOM_MULTIPLIER = 1.5f;
		public static void RemoveObjectFromAwaitingHaulingHashSets(Map map, IntVec3 location, object obj, Dictionary<Map, List<HashSet<object>[]>> awaitingActionMapDict)
		{
			int jumboCellWidth;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			List<HashSet<object>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(awaitingActionMapDict, map);
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int cellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
				HashSet<object> hashset = awaitingActionZoomLevels[zoomLevel][cellIndex];
				if (hashset != null)
				{
					hashset.Remove(obj);
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}
		public static void AddObjectToActionableObjects(Map map, IntVec3 location, object obj, Dictionary<Map, List<HashSet<object>[]>> awaitingActionMapDict)
		{
			int jumboCellWidth;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;

			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				List<HashSet<object>[]> awaitingHaulingZoomLevels = GetAwaitingActionsZoomLevels(awaitingActionMapDict, map);
				HashSet<object>[] awaitingHaulingGrid = awaitingHaulingZoomLevels[zoomLevel];
				int jumboCellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
				HashSet<object> hashset = awaitingHaulingGrid[jumboCellIndex];
				if (hashset == null)
				{
					hashset = new HashSet<object>();
					awaitingHaulingGrid[jumboCellIndex] = hashset;
				}
				hashset.Add(obj);
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

        private static List<HashSet<object>[]> GetAwaitingActionsZoomLevels(Dictionary<Map, List<HashSet<object>[]>> awaitingActionMapDict, Map map)
        {
            if(!awaitingActionMapDict.TryGetValue(map, out List<HashSet<object>[]> awaitingActionsZoomLevels))
            {
				awaitingActionsZoomLevels = new List<HashSet<object>[]>();
				int mapSizeX = map.Size.x;
				int mapSizeZ = map.Size.z;
				int jumboCellWidth;
				int zoomLevel = 0;
				do
				{
					jumboCellWidth = getJumboCellWidth(zoomLevel);
					int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
					awaitingActionsZoomLevels.Add(new HashSet<object>[numGridCells]);
					zoomLevel++;
				} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
				awaitingActionMapDict[map] = awaitingActionsZoomLevels;
			}
			return awaitingActionsZoomLevels;

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

		public static IEnumerable<object> GetClosestActionableObjects(Pawn pawn, Map map, Dictionary<Map, List<HashSet<object>[]>> awaitingActionMapDict)
		{
			int jumboCellWidth;
			int XposOfJumboCell;
			int ZposOfJumboCell;
			int cellIndex;
			int mapSizeX = map.Size.x;
			HashSet<object> objectsAtCellCopy;
			List<HashSet<object>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(awaitingActionMapDict, map);
			IntVec3 position = pawn.Position;
			Area effectiveAreaRestrictionInPawnCurrentMap = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
			Range2D areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
			Range2D scannedRange = new Range2D(position.x, position.z, position.x, position.z);
			for (int zoomLevel = 0; zoomLevel < awaitingActionZoomLevels.Count; zoomLevel++)
			{
				HashSet<object>[] objectsGrid = awaitingActionZoomLevels[zoomLevel];
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
				XposOfJumboCell = position.x / jumboCellWidth;
				ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
				if (zoomLevel == 0)
				{
					cellIndex = CellToIndexCustom(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth);
					HashSet<object> objectsAtCell = objectsGrid[cellIndex];
					if (objectsAtCell != null && objectsAtCell.Count > 0)
					{
						objectsAtCellCopy = new HashSet<object>(objectsAtCell);
						foreach (object haulableThing in objectsAtCellCopy)
						{
							yield return haulableThing;
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
						HashSet<object> thingsAtCell = objectsGrid[CellToIndexCustom(newXposOfJumboCell, newZposOfJumboCell, jumboCellColumnsInMap)];
						if (thingsAtCell != null && thingsAtCell.Count > 0)
						{
							objectsAtCellCopy = new HashSet<object>(thingsAtCell);
							foreach (object actionalbeObject in objectsAtCellCopy)
							{
								yield return actionalbeObject;
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
	}
}
