using System.Collections.Generic;
using UnityEngine;
using Verse;
using static RimThreaded.Area_Patch;

namespace RimThreaded
{
    static class JumboCell
	{
		private static readonly List<int> zoomLevels = new List<int>();
		private const float ZOOM_MULTIPLIER = 1.5f;
		public static int getJumboCellWidth(int zoomLevel)
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
		public static int CellToIndexCustom(IntVec3 position, int mapSizeX, int jumboCellWidth)
		{
			int XposInJumboCell = position.x / jumboCellWidth;
			int ZposInJumboCell = position.z / jumboCellWidth;
			int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
			return CellXZToIndexCustom(XposInJumboCell, ZposInJumboCell, jumboCellColumnsInMap);
		}
		public static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int jumboCellWidth)
		{
			return GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth) * Mathf.CeilToInt(mapSizeZ / (float)jumboCellWidth);
		}
		public static int GetJumboCellColumnsInMap(int mapSizeX, int jumboCellWidth)
		{
			return Mathf.CeilToInt(mapSizeX / (float)jumboCellWidth);
		}
		public static int CellXZToIndexCustom(int XposOfJumboCell, int ZposOfJumboCell, int jumboCellColumnsInMap)
		{
			return (jumboCellColumnsInMap * ZposOfJumboCell) + XposOfJumboCell;
		}

		public static IEnumerable<IntVec3> GetOffsetOrder(IntVec3 position, int zoomLevel, Range2D scannedRange, Range2D areaRange)
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
