using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static RimThreaded.Area_Patch;
using static RimThreaded.JumboCell;

namespace RimThreaded
{    
    public class ListerThings_Patch
    {
		public static void RunDestructivePatches()
		{
			Type original = typeof(ListerThings);
			Type patched = typeof(ListerThings_Patch);
			RimThreadedHarmony.Prefix(original, patched, nameof(Remove));
			RimThreadedHarmony.Prefix(original, patched, nameof(Add));
			//RimThreadedHarmony.Postfix(original, patched, nameof(ThingsMatching));
			//RimThreadedHarmony.Postfix(original, patched, nameof(get_AllThings));

		}
		public static void get_AllThings(ListerThings __instance, ref List<Thing> __result)
        {
			if (__result != null)
			{
				lock (__instance)
				{
					List<Thing> tmp = __result;
					__result = new List<Thing>(tmp);
					//__result. AddRange(tmp);
					//__result = new List<Thing>(__result);
				}
			}
		}
		public static void ThingsMatching(ListerThings __instance, ref List<Thing> __result, ThingRequest req)//this has to give a snapshot not just a reference -Sernior
        {
			if ( __result != null )
            {
                lock ( __instance )
                {
					List<Thing> tmp = __result;
					__result = new List<Thing>(tmp);
					//__result.Clear();
					//__result.AddRange(tmp);
					//__result = new List<Thing>(__result);
				}
            }
        }


		public static bool Add(ListerThings __instance, Thing t)		
		{
			ThingDef thingDef = t.def;
			if (!ListerThings.EverListable(thingDef, __instance.use))
			{
				return false;
			}

			lock (__instance)
			{
				if (!__instance.listsByDef.TryGetValue(thingDef, out List<Thing> value))
				{
					value = new List<Thing>();
					__instance.listsByDef.Add(t.def, value);
				} 
				value.Add(t);
			}

			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			foreach (ThingRequestGroup thingRequestGroup in allGroups)
			{
				if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(thingDef))
				{
					lock (__instance)
					{
						List<Thing> list = __instance.listsByGroup[(uint)thingRequestGroup];
						if (list == null)
						{
							list = new List<Thing>();
							__instance.listsByGroup[(uint)thingRequestGroup] = list;
							__instance.stateHashByGroup[(uint)thingRequestGroup] = 0;
						}
						list.Add(t);
						AddThingToHashSets(t, thingRequestGroup);
						__instance.stateHashByGroup[(uint)thingRequestGroup]++;
					}
				}
			}



			return false;
		}

		public static bool Remove(ListerThings __instance, Thing t)
		{
			ThingDef thingDef = t.def;
			if (!ListerThings.EverListable(thingDef, __instance.use))
			{
				return false;
			}
			lock(__instance)
            {
                List<Thing> newListsByDef = new List<Thing>(__instance.listsByDef[thingDef]);
				newListsByDef.Remove(t);
				__instance.listsByDef[thingDef] = newListsByDef;
			}
			
			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			for (int i = 0; i < allGroups.Length; i++)
			{
				ThingRequestGroup thingRequestGroup = allGroups[i];
				if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(thingDef))
				{
					lock (__instance)
					{
                        List<Thing> newListsByGroup = new List<Thing>(__instance.listsByGroup[i]);
						newListsByGroup.Remove(t);
						RemoveThingFromHashSets(t, thingRequestGroup);
						__instance.listsByGroup[i] = newListsByGroup;
						__instance.stateHashByGroup[(uint)thingRequestGroup]++;
					}
				}
			}
			return false;
		}


		public static List<int> zoomLevels = new List<int>();
		public static Dictionary<Map, List<HashSet<Thing>[]>[]> trGroupsOfZoomLevelsOfThingsMapDict = new Dictionary<Map, List<HashSet<Thing>[]>[]>();
		public const float ZOOM_MULTIPLIER = 1.5f; //must be greater than 1. lower numbers will make searches slower, but ensure pawns find the closer things first.
												   // Map, (jumbo cell zoom level, #0 item=zoom 2x2, #1 item=4x4), jumbo cell index converted from x,z coord, HashSet<Thing>
		[ThreadStatic] private static HashSet<Thing> returnedThings;
		internal static void InitializeThreadStatics()
		{
			returnedThings = new HashSet<Thing>();
		}
		private static List<HashSet<Thing>[]> GetThingsZoomLevels(Map map, ThingRequest thingReq)
		{
			if (thingReq.singleDef == null)
			{
				return GetThingRequestGroupZoomLevels(map, thingReq.group);
			}
			return null;
		}
		private static List<HashSet<Thing>[]> GetThingRequestGroupZoomLevels(Map map, ThingRequestGroup group)
		{
			List<HashSet<Thing>[]> thingsZoomLevels = null;
			if (!trGroupsOfZoomLevelsOfThingsMapDict.TryGetValue(map, out List<HashSet<Thing>[]>[] trGroupsOfZoomLevelsOfThings))
			{
				lock (trGroupsOfZoomLevelsOfThingsMapDict)
				{
					if (!trGroupsOfZoomLevelsOfThingsMapDict.TryGetValue(map, out List<HashSet<Thing>[]>[] trGroupsOfZoomLevelsOfThings2))
					{
						trGroupsOfZoomLevelsOfThings2 = new List<HashSet<Thing>[]>[ThingListGroupHelper.AllGroups.Length];
						trGroupsOfZoomLevelsOfThingsMapDict[map] = trGroupsOfZoomLevelsOfThings2;
					}
					trGroupsOfZoomLevelsOfThings = trGroupsOfZoomLevelsOfThings2;
				}
			}
			thingsZoomLevels = GetZoomLevelOfThings(trGroupsOfZoomLevelsOfThings, group, map.Size.x, map.Size.z);
			return thingsZoomLevels;
		}
		private static List<HashSet<Thing>[]> GetThingsZoomLevels(List<HashSet<Thing>[]>[] trGroupsOfZoomLevelsOfThings, ThingRequestGroup group)
        {
			List<HashSet<Thing>[]> thingsZoomLevels = trGroupsOfZoomLevelsOfThings[(uint)group];
			if(thingsZoomLevels == null)
            {
				thingsZoomLevels = new List<HashSet<Thing>[]>();
				trGroupsOfZoomLevelsOfThings[(uint)group] = thingsZoomLevels;
			}
			return thingsZoomLevels;
		}

		private static List<HashSet<Thing>[]> GetZoomLevelOfThings(List<HashSet<Thing>[]>[] trGroupsOfZoomLevelsOfThings, ThingRequestGroup group, int mapSizeX, int mapSizeZ)
		{
			List<HashSet<Thing>[]> thingsZoomLevels = GetThingsZoomLevels(trGroupsOfZoomLevelsOfThings, group);
			int jumboCellWidth;
			int zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
				thingsZoomLevels.Add(new HashSet<Thing>[numGridCells]);
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
			return thingsZoomLevels;
		}

		private static int CellToIndexCustom2(int XposOfJumboCell, int ZposOfJumboCell, int jumboCellColumnsInMap)
		{
			return (jumboCellColumnsInMap * ZposOfJumboCell) + XposOfJumboCell;
		}
		public static IEnumerable<Thing> GetClosestThingRequestGroup(Pawn pawn, Map map, ThingRequest thingReq)
		{
			int mapSizeX = map.Size.x;
			returnedThings.Clear(); //hashset used to ensure same item is not retured twice

			List<HashSet<Thing>[]> zoomLevelsOfThings = GetThingsZoomLevels(map, thingReq);
			IntVec3 position = pawn.Position;
			Area effectiveAreaRestrictionInPawnCurrentMap = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
			Range2D areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
			Range2D scannedRange = new Range2D(position.x, position.z, position.x, position.z);
			for (int zoomLevel = 0; zoomLevel < zoomLevelsOfThings.Count; zoomLevel++)
			{
				HashSet<Thing>[] thingsGrid = zoomLevelsOfThings[zoomLevel];
				int jumboCellWidth = getJumboCellWidth(zoomLevel);
				int jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
				int XposOfJumboCell = position.x / jumboCellWidth;
				int ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
				if (zoomLevel == 0) //this is needed to grab the center (9th square) otherwise, only the outside 8 edges/corners are normally needed
				{
					foreach (Thing haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth, thingsGrid))
						yield return haulableThing;
				}
				IEnumerable<IntVec3> offsetOrder = GetOptimalOffsetOrder(position, zoomLevel, scannedRange, areaRange, jumboCellWidth);
				foreach (IntVec3 offset in offsetOrder)
				{
					int newXposOfJumboCell = XposOfJumboCell + offset.x;
					int newZposOfJumboCell = ZposOfJumboCell + offset.z;
					if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap && newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
					{
						foreach (Thing haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth, thingsGrid))
							yield return haulableThing;
					}
				}
				scannedRange.minX = Math.Min(scannedRange.minX, (XposOfJumboCell - 1) * jumboCellWidth);
				scannedRange.minZ = Math.Min(scannedRange.minZ, (ZposOfJumboCell - 1) * jumboCellWidth);
				scannedRange.maxX = Math.Max(scannedRange.maxX, ((XposOfJumboCell + 2) * jumboCellWidth) - 1);
				scannedRange.maxZ = Math.Max(scannedRange.maxZ, ((ZposOfJumboCell + 2) * jumboCellWidth) - 1);
			}
		}

		private static IEnumerable<Thing> GetThingsAtCellCopy(int XposOfJumboCell, int ZposOfJumboCell, int jumboCellWidth, HashSet<Thing>[] thingsGrid)
		{
			int cellIndex = CellToIndexCustom2(XposOfJumboCell, ZposOfJumboCell, jumboCellWidth);
			HashSet<Thing> thingsAtCell = thingsGrid[cellIndex];
			if (thingsAtCell != null && thingsAtCell.Count > 0)
			{
				HashSet<Thing> thingsAtCellCopy = new HashSet<Thing>(thingsAtCell);
				foreach (Thing haulableThing in thingsAtCellCopy)
				{
					if (!returnedThings.Contains(haulableThing))
					{
						yield return haulableThing;
						returnedThings.Add(haulableThing);
					}
				}
			}
		}

		private static void AddThingToHashSets(Thing haulableThing, ThingRequestGroup thingRequestGroup)
		{
			int jumboCellWidth;
			Map map = haulableThing.Map;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;

			List<HashSet<Thing>[]> thingsZoomLevels = GetThingRequestGroupZoomLevels(map, thingRequestGroup);
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				HashSet<Thing>[] thingsGrid = thingsZoomLevels[zoomLevel];
				int jumboCellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, jumboCellWidth);
				HashSet<Thing> hashset = thingsGrid[jumboCellIndex];
				if (hashset == null)
				{
					hashset = new HashSet<Thing>();
					lock (thingsGrid)
					{
						thingsGrid[jumboCellIndex] = hashset;
					}
				}
				lock (hashset)
				{
					hashset.Add(haulableThing);
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

		private static void RemoveThingFromHashSets(Thing haulableThing, ThingRequestGroup thingRequestGroup)
		{
			int jumboCellWidth;
			Map map = haulableThing.Map;
			if (map == null)
				return; //not optimal
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			List<HashSet<Thing>[]> thingsZoomLevels = GetThingRequestGroupZoomLevels(map, thingRequestGroup);
			zoomLevel = 0;
			do
			{
				jumboCellWidth = getJumboCellWidth(zoomLevel);
				HashSet<Thing>[] thingsGrid = thingsZoomLevels[zoomLevel];
				int jumboCellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, jumboCellWidth);
				HashSet<Thing> hashset = thingsGrid[jumboCellIndex];
				if (hashset != null)
				{
					HashSet<Thing> newHashSet = new HashSet<Thing>(hashset);
					newHashSet.Remove(haulableThing);
					thingsGrid[jumboCellIndex] = newHashSet;
				}
				zoomLevel++;
			} while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
		}

	}

}
