using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Region_Patch
    {

        [ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex; //newly defined
        public static object cachedSafeTemperatureRange = new object();
        public static Dictionary<Region, HashSet<IntVec3>> RegionCells = new Dictionary<Region, HashSet<IntVec3>>();

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Region);
            Type patched = typeof(Region_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(DangerFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_District));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_RandomCell));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_CellCount));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Cells));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_AnyCell));
        }
        public static bool get_Cells(Region __instance, ref IEnumerable<IntVec3> __result)
        {
            __result = GetRegionCells(__instance);
            return false;
        }
        public static bool get_RandomCell(Region __instance, ref IntVec3 __result)
        {
            __result = GetRegionCells(__instance).RandomElement();
            return false;
        }
        public static bool get_AnyCell(Region __instance, ref IntVec3 __result)
        {
            HashSet<IntVec3> cells = GetRegionCells(__instance);
            if(cells.Count == 0)
            {
                __result = __instance.extentsClose.RandomCell;
                return false;
            }
            __result = GetRegionCells(__instance).RandomElement();
            return false;
        }
        public static HashSet<IntVec3> GetRegionCells(Region region)
        {
            if (!RegionCells.TryGetValue(region, out HashSet<IntVec3> regionCells))
            {
                regionCells = new HashSet<IntVec3>();
                RegionGrid regions = region.Map.regionGrid;
                for (int z = region.extentsClose.minZ; z <= region.extentsClose.maxZ; z++)
                {
                    for (int x = region.extentsClose.minX; x <= region.extentsClose.maxX; x++)
                    {
                        IntVec3 intVec = new IntVec3(x, 0, z);
                        if (regions.GetRegionAt_NoRebuild_InvalidAllowed(intVec) == region)
                        {
                            regionCells.Add(intVec);
                        }
                    }
                }
                RegionCells[region] = regionCells;
            }
            return regionCells;
        }

        internal static void InitializeThreadStatics()
        {
            regionClosedIndex = new Dictionary<Region, uint[]>();
        }
        public static bool get_CellCount(Region __instance, ref int __result)
        {
            __result = GetRegionCells(__instance).Count;
            return false;
            /*
            if (__instance.cachedCellCount == -1)
            {
                __instance.cachedCellCount = 0;
                RegionGrid regionGrid = __instance.Map.regionGrid;
                for (int i = __instance.extentsClose.minZ; i <= __instance.extentsClose.maxZ; i++)
                {
                    for (int j = __instance.extentsClose.minX; j <= __instance.extentsClose.maxX; j++)
                    {
                        IntVec3 c = new IntVec3(j, 0, i);
                        if (regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c) == __instance)
                        {
                            __instance.cachedCellCount++;
                        }
                    }
                }
                __instance.cachedCellCount = Math.Max(1, __instance.cachedCellCount);
            }
            __result = __instance.cachedCellCount;
            return false;
            */
        }
        /*
        public static bool get_RandomCell(Region __instance, ref IntVec3 __result)
        {
            Map map = __instance.Map;
            CellIndices cellIndices = map.cellIndices;
            Region[] directGrid = map.regionGrid.DirectGrid;
            for (int i = 0; i < 1000; i++)
            {
                IntVec3 randomCell = __instance.extentsClose.RandomCell;
                if (directGrid[cellIndices.CellToIndex(randomCell)] == __instance)
                {
                    __result = randomCell;
                    return false;
                }
            }
            __result = __instance.AnyCell;
            return false;
        }
        */
        public static uint[] GetRegionClosedIndex(Region region)
        {
            if (regionClosedIndex.TryGetValue(region, out uint[] closedIndex)) return closedIndex;
            closedIndex = new uint[8];
            regionClosedIndex[region] = closedIndex;
            return closedIndex;
        }
        public static void SetRegionClosedIndex(Region region, uint[] value)
        {
            regionClosedIndex[region] = value;
            return;
        }

        internal static void AddFieldReplacements()
        {
            Dictionary<OpCode, MethodInfo> regionTraverserReplacements = new Dictionary<OpCode, MethodInfo>();
            regionTraverserReplacements.Add(OpCodes.Ldfld, Method(typeof(Region_Patch), nameof(GetRegionClosedIndex)));
            regionTraverserReplacements.Add(OpCodes.Stfld, Method(typeof(Region_Patch), nameof(SetRegionClosedIndex)));
            RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), nameof(Region.closedIndex)), regionTraverserReplacements);
        }
        public static bool get_District(Region __instance, ref District __result)
        {
            __result = __instance.districtInt;
            return false;
        }
        public static bool DangerFor(Region __instance, ref Danger __result, Pawn p)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (__instance.cachedDangersForFrame != Time_Patch.get_frameCount())
                {
                    lock (__instance)
                    {
                        __instance.cachedDangers = new List<KeyValuePair<Pawn, Danger>>();
                    }
                    __instance.cachedDangersForFrame = Time_Patch.get_frameCount();
                }
                else
                {
                    List<KeyValuePair<Pawn, Danger>> localCachedDangers = __instance.cachedDangers;
                    for (int index = 0; index < localCachedDangers.Count; ++index)
                    {
                        if (localCachedDangers[index].Key == p)
                        {
                            __result = localCachedDangers[index].Value;
                            return false;
                        }
                    }
                }
            }
            Danger danger = Danger.Unspecified;
            Room room = __instance.Room;
            District district = null;
            if (room != null)
            {
                district = __instance.District;
            }
            if (room != null && district != null)
            {
                float temperature = room.Temperature;
                FloatRange floatRange;
                if (Current.ProgramState == ProgramState.Playing)
                {
                    if (Region.cachedSafeTemperatureRangesForFrame != Time_Patch.get_frameCount())
                    {
                        lock (cachedSafeTemperatureRange)
                        {
                            Region.cachedSafeTemperatureRanges = new Dictionary<Pawn, FloatRange>();
                        }
                        Region.cachedSafeTemperatureRangesForFrame = Time_Patch.get_frameCount();
                    }
                    if (!Region.cachedSafeTemperatureRanges.TryGetValue(p, out floatRange))
                    {
                        lock (cachedSafeTemperatureRange)
                        {
                            if (!Region.cachedSafeTemperatureRanges.TryGetValue(p, out FloatRange floatRange2))
                            {
                                floatRange2 = p.SafeTemperatureRange();
                                Region.cachedSafeTemperatureRanges.Add(p, floatRange2);
                            }
                            floatRange = floatRange2;
                        }
                    }
                }
                else
                    floatRange = p.SafeTemperatureRange();
                danger = !floatRange.Includes(temperature) ? (!floatRange.ExpandedBy(80f).Includes(temperature) ? Danger.Deadly : Danger.Some) : Danger.None;
                if (Current.ProgramState == ProgramState.Playing)
                {
                    lock (__instance.cachedDangers)
                    {
                        __instance.cachedDangers.Add(new KeyValuePair<Pawn, Danger>(p, danger));
                    }
                }
            }
            __result = danger;
            return false;
        }
        
    }
}
