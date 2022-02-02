using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RimThreaded.Area_Patch;

namespace RimThreaded
{
    static class JumboCell
	{
		[ThreadStatic] private static HashSet<IntVec3> retrunedThings;
		static readonly IntVec3[] noOffset = new IntVec3[] { IntVec3.Zero };
		private static readonly List<int> zoomLevels = new List<int>();
		private const float ZOOM_MULTIPLIER = 1.5f;

		internal static void InitializeThreadStatics()
		{
			retrunedThings = new HashSet<IntVec3>();
		}
		public static void ReregisterObject(Map map, IntVec3 location, JumboCell_Cache jumboCell_Cache)
		{
            Dictionary<Map, List<HashSet<IntVec3>[]>> positionsAwaitingAction = jumboCell_Cache.positionsAwaitingAction;
			//try to remove an item that has a null map. only way is to check all maps...
			if (map == null)
			{
				foreach (KeyValuePair<Map, List<HashSet<IntVec3>[]>> kv in positionsAwaitingAction)
				{
					Map map2 = kv.Key;
					if (map2 != null)
						RemoveObjectFromAwaitingActionHashSets(map2, location, kv.Value);
				}
				return;
			}
			List<HashSet<IntVec3>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(jumboCell_Cache.positionsAwaitingAction, map);
			RemoveObjectFromAwaitingActionHashSets(map, location, awaitingActionZoomLevels);
			if (jumboCell_Cache.IsActionableObject(map, location))
				AddObjectToActionableObjects(map, location, awaitingActionZoomLevels);
		}

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
			int slope2 = (cartesianZ * 200) / ((cartesianX * 100) + 1);
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

		public static IEnumerable<IntVec3> GetClosestActionableLocations(Pawn pawn, Map map, JumboCell_Cache jumboCell_Cache)
		{
			int jumboCellWidth;
			int XposOfJumboCell;
			int ZposOfJumboCell;
			int mapSizeX = map.Size.x;
			retrunedThings.Clear();
			IntVec3[] objectsAtCellCopy;
			List<HashSet<IntVec3>[]> awaitingActionZoomLevels = GetAwaitingActionsZoomLevels(jumboCell_Cache.positionsAwaitingAction, map);
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
				}
				else
				{
					offsetOrder = GetOptimalOffsetOrder(position, zoomLevel, scannedRange, areaRange, jumboCellWidth);
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
								if (!retrunedThings.Contains(actionableObject))
								{
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
		public static void RemoveObjectFromAwaitingActionHashSets(Map map, IntVec3 location, List<HashSet<IntVec3>[]> awaitingActionZoomLevels)
		{
			if (map == null || map.info == null)
				return;
			IntVec3 size = map.Size;
			if (size == null)
				return;
			int jumboCellWidth;
			int mapSizeX = size.x;
			int mapSizeZ = size.z;
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

		public static List<HashSet<IntVec3>[]> GetAwaitingActionsZoomLevels(Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict, Map map)
		{
			if (!awaitingActionMapDict.TryGetValue(map, out List<HashSet<IntVec3>[]> awaitingActionsZoomLevels))
			{
				lock (awaitingActionMapDict)
				{
					if (!awaitingActionMapDict.TryGetValue(map, out List<HashSet<IntVec3>[]> awaitingActionsZoomLevels2))
					{
						Log.Message("RimThreaded is caching Cells...");
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
