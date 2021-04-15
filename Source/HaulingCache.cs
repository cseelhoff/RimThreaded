using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class HaulingCache
    {
		static readonly Dictionary<Map, HashSet<Thing>[]> waitingForZoneBetterThanMapDict = new Dictionary<Map, HashSet<Thing>[]>(); //each Map has sets of Things for each storage priority (typically 6)
		public static int[] power2array = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 }; // a 16384x16384 map is probably too big
		public static Dictionary<Map, List<HashSet<Thing>[]>> awaitingHaulingMapDict = new Dictionary<Map, List<HashSet<Thing>[]>>();
		// Map, (jumbo cell zoom level, #0 item=zoom 2x2, #1 item=4x4), jumbo cell index converted from x,z coord, HashSet<Thing>

		public static bool RegisterHaulableItem(Thing haulableThing) {
			int storagePriority = (int)StoreUtility.CurrentStoragePriorityOf(haulableThing);
			bool foundDestination = false;
			Map map = haulableThing.Map;
			
			if (StoreUtility.TryFindBestBetterStoreCellFor(haulableThing, null, map, StoreUtility.CurrentStoragePriorityOf(haulableThing), null, out _, false)) {
				AddThingToAwaitingHaulingHashSets(haulableThing);
			} else			
            {
				getWaitingForZoneBetterThan(map)[storagePriority].Add(haulableThing);
			}
			return foundDestination;
		}

		private static void AddThingToAwaitingHaulingHashSets(Thing haulableThing)
		{
			int power2;
			Map map = haulableThing.Map;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;

			zoomLevel = 0;
			do
			{
				power2 = power2array[zoomLevel];
				int cellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, power2);
                List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
                HashSet<Thing> hashset = awaitingHaulingZoomLevels[zoomLevel][cellIndex];
				if(hashset == null)
                {
					hashset = new HashSet<Thing>();
					awaitingHaulingZoomLevels[zoomLevel][cellIndex] = hashset;
				}
				hashset.Add(haulableThing);
				zoomLevel++;
			} while (power2 < mapSizeX || power2 < mapSizeZ);
		}

		private static void RemoveThingToAwaitingHaulingHashSets(Thing haulableThing)
		{
			int power2;
			Map map = haulableThing.Map;
			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;
			int zoomLevel;
			List<HashSet<Thing>[]> awaitingHaulingZoomLevels = awaitingHaulingMapDict[map];
			zoomLevel = 0;
			do
			{
				power2 = power2array[zoomLevel];
				int cellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, power2);
				HashSet<Thing> hashset = awaitingHaulingZoomLevels[zoomLevel][cellIndex];
				if (hashset != null)
				{
					hashset.Remove(haulableThing);
				}
				zoomLevel++;
			} while (power2 < mapSizeX || power2 < mapSizeZ);
		}

		private static List<HashSet<Thing>[]> GetAwaitingHauling(Map map)
        {
			if (!awaitingHaulingMapDict.TryGetValue(map, out List<HashSet<Thing>[]> awaitingHaulingZoomLevels))
			{
				awaitingHaulingZoomLevels = new List<HashSet<Thing>[]>();
				int mapSizeX = map.Size.x;
				int mapSizeZ = map.Size.z;
				int power2;
				int zoomLevel = 0;
				do
				{
					power2 = power2array[zoomLevel];
					int numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, power2);
					awaitingHaulingZoomLevels.Add(new HashSet<Thing>[numGridCells]);
					zoomLevel++;
				} while (power2 < mapSizeX || power2 < mapSizeZ);
				awaitingHaulingMapDict[map] = awaitingHaulingZoomLevels;
			}
			return awaitingHaulingZoomLevels;
		}

		private static int CellToIndexCustom(IntVec3 c, int mapSizeX, int cellSize)
		{
			return (mapSizeX * c.z + c.x) / cellSize;
		}
		private static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int cellSize)
		{
			return Mathf.CeilToInt((mapSizeX * mapSizeZ) / (float)cellSize);
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

		public static bool IsValidProcess(Thing t, Predicate<Thing> validator = null)
		{
			return t.Spawned && (validator == null || validator(t));
		}

		public static Thing ClosestThingReachable(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
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

				Predicate<Thing> validator2 = delegate (Thing t)
				{
					if (!map.reachability.CanReach(root, t, peMode, traverseParams))
					{
						return false;
					}

					return (validator == null || validator(t)) ? true : false;
				};
				IEnumerable<Thing> searchSet = GetClosestHaulableItems(root, map);
				if (validator == null)
				{
					foreach (Thing tryThing in searchSet)
					{
						if (tryThing.Spawned) {
							thing = tryThing;
							break;
						}
						else
						{
							ReregisterHaulableItem(tryThing);
						}
					}
				} else
                {
					foreach (Thing tryThing in searchSet)
					{
						if (tryThing.Spawned && validator(tryThing)) {
							thing = tryThing;
							break;
						} else
                        {
							ReregisterHaulableItem(tryThing);
                        }
					}
				}
			}

			return thing;
		}

        private static IEnumerable<Thing> GetClosestHaulableItems(IntVec3 position, Map map)
        {
			int power2;
			int cellIndex;
			int mapSizeX = map.Size.x;
			HashSet<Thing> thingsAtCellCopy;
			List<HashSet<Thing>[]> awaitingHaulingZoomLevels = GetAwaitingHauling(map);
			for (int zoomLevel = 0; zoomLevel < awaitingHaulingZoomLevels.Count; zoomLevel++)
            {
				HashSet<Thing>[] thingsGrid = awaitingHaulingZoomLevels[zoomLevel];
				power2 = power2array[zoomLevel];
				if (zoomLevel == 0)
				{
					cellIndex = CellToIndexCustom(position, mapSizeX, power2);
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
				IEnumerable<IntVec3> offsetOrder = GetOffsetOrder(position, zoomLevel);
				foreach(IntVec3 offset in offsetOrder)
                {
					cellIndex = CellToIndexCustom(position + offset, mapSizeX, power2);
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
			}
		}

        private static IEnumerable<IntVec3> GetOffsetOrder(IntVec3 position, int zoomLevel)
        {
			yield return IntVec3.North;
			yield return IntVec3.NorthEast;
			yield return IntVec3.East;
			yield return IntVec3.SouthEast;
			yield return IntVec3.South;
			yield return IntVec3.SouthWest;
			yield return IntVec3.West;
			yield return IntVec3.NorthWest;
		}
	}
}
