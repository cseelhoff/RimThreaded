using System;
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
		const int sixteen = 16;

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

		public static IEnumerable<IntVec3> GetOptimalOffsetOrder(IntVec3 position, int zoomLevel, Range2D scannedRange, Range2D areaRange, int jumboCellWidth)
		{
			//optimization is a bit more costly for performance, but should help find "nearer" next jumbo cell to check
			int angle16 = GetAngle16(position, jumboCellWidth);
			foreach (int cardinalDirection in GetClosestDirections(angle16))
			{
				switch (cardinalDirection)
				{
					case 0:
						if (scannedRange.maxZ < areaRange.maxZ)
							yield return IntVec3.North;
						break;

					case 1:
						if (scannedRange.maxZ < areaRange.maxZ && scannedRange.maxX < areaRange.maxX)
							yield return IntVec3.NorthEast;
						break;

					case 2:
						if (scannedRange.maxX < areaRange.maxX)
							yield return IntVec3.East;
						break;

					case 3:
						if (scannedRange.minZ > areaRange.minZ && scannedRange.maxX < areaRange.maxX)
							yield return IntVec3.SouthEast;
						break;

					case 4:
						if (scannedRange.minZ > areaRange.minZ)
							yield return IntVec3.South;
						break;

					case 5:
						if (scannedRange.minZ > areaRange.minZ && scannedRange.minX > areaRange.minX)
							yield return IntVec3.SouthWest;
						break;

					case 6:
						if (scannedRange.maxX < areaRange.maxX)
							yield return IntVec3.West;
						break;

					case 7:
						if (scannedRange.maxZ < areaRange.maxZ && scannedRange.minX > areaRange.minX)
							yield return IntVec3.NorthWest;
						break;
				}
			}
		}

		public static int GetAngle16(IntVec3 position, int jumboCellWidth)
		{
			int relativeX = position.x % jumboCellWidth;
			int relativeZ = position.z % jumboCellWidth;
			int widthOffset = jumboCellWidth - 1;
			int cartesianX = (relativeX * 2) - widthOffset;
			int cartesianZ = (relativeZ * 2) - widthOffset;
			int slope2 = (cartesianZ * 2) / cartesianX;
			if (cartesianX >= 0)
			{
				if (slope2 >= 0)
				{
					if (slope2 >= 2)
					{
						if (slope2 >= 4)
							return 0;
						else //if (slope2 >= 2)
							return 1;
					}
					else if (slope2 >= 1)
						return 2;
					else //if (slope2 >= 0)
						return 3;
				}
				else
				{
					if (slope2 <= -2)
					{
						if (slope2 <= -4)
							return 7;
						else //if (slope2 <= -2)
							return 6;
					}
					else if (slope2 <= -1)
						return 5;
					else // -1 < slope < 0
						return 4;
				}
			}
			else
			{
				if (slope2 >= 0)
				{
					if (slope2 >= 2)
					{
						if (slope2 >= 4)
							return 8;
						else //if (slope2 >= 2)
							return 9;
					}
					else if (slope2 >= 1)
						return 10;
					else //if (slope2 >= 0)
						return 11;
				}
				else
				{
					if (slope2 <= -2)
					{
						if (slope2 <= -4)
							return 15;
						else //if (slope2 <= -2)
							return 14;
					}
					else if (slope2 <= -1)
						return 13;
					else // -1 < slope < 0
						return 12;
				}
			}
		}

		public static IEnumerable<int> GetClosestDirections(int startingPosition)
		{
			int starting8 = ((startingPosition + 1) / 2) % 8;
			yield return starting8;
			int startingDirection = startingPosition % 2;
			switch (startingDirection)
			{
				case 0:
					yield return (starting8 + 1) % 8;
					yield return (starting8 + 7) % 8;
					yield return (starting8 + 2) % 8;
					yield return (starting8 + 6) % 8;
					yield return (starting8 + 3) % 8;
					yield return (starting8 + 5) % 8;
					yield return (starting8 + 4) % 8;
					break;
				case 1:
					yield return (starting8 + 7) % 8;
					yield return (starting8 + 1) % 8;
					yield return (starting8 + 6) % 8;
					yield return (starting8 + 2) % 8;
					yield return (starting8 + 3) % 8;
					yield return (starting8 + 4) % 8;
					yield return (starting8 + 5) % 8;
					break;
			}
		}
	}
}
