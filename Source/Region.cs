using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Text.RegularExpressions;

namespace RimThreaded
{

    public class Region_Patch
    {
        public static AccessTools.FieldRef<Region, Dictionary<Area, AreaOverlap>> cachedAreaOverlaps =
            AccessTools.FieldRefAccess<Region, Dictionary<Area, AreaOverlap>>("cachedAreaOverlaps");
        public static AccessTools.FieldRef<Region, int> cachedDangersForFrame =
            AccessTools.FieldRefAccess<Region, int>("cachedDangersForFrame");
        public static AccessTools.FieldRef<Region, List<KeyValuePair<Pawn, Danger>>> cachedDangers =
            AccessTools.FieldRefAccess<Region, List<KeyValuePair<Pawn, Danger>>>("cachedDangers");
        public static Dictionary<Pawn, FloatRange> cachedSafeTemperatureRanges =
            AccessTools.StaticFieldRefAccess<Dictionary<Pawn, FloatRange>>(typeof(Region), "cachedSafeTemperatureRanges");
        public static int cachedSafeTemperatureRangesForFrame =
            AccessTools.StaticFieldRefAccess<int>(typeof(Region), "cachedSafeTemperatureRangesForFrame");

        public static bool OverlapWith(Region __instance, ref AreaOverlap __result, Area a)
        {
            
            if (a.TrueCount == 0)
            {
                __result = AreaOverlap.None;
                return false;
            }

            if (__instance.Map != a.Map)
            {
                __result = AreaOverlap.None;
                return false;
            }

            if (cachedAreaOverlaps(__instance) == null)
            {
                cachedAreaOverlaps(__instance) = new Dictionary<Area, AreaOverlap>();
            }
            Dictionary<Area, AreaOverlap> cao = cachedAreaOverlaps(__instance);
            AreaOverlap value;
            lock (cao)
            {
                bool valueExists = cao.TryGetValue(a, out value);
                if (!valueExists)
                {
                    int num = 0;
                    int num2 = 0;
                    foreach (IntVec3 cell in __instance.Cells)
                    {
                        num2++;
                        if (a[cell])
                        {
                            num++;
                        }
                    }

                    value = ((num != 0) ? ((num == num2) ? AreaOverlap.Entire : AreaOverlap.Partial) : AreaOverlap.None);

                    cao.Add(a, value);
                }
            }

            __result = value;
            return false;
        }



        public static bool get_AnyCell(Region __instance, ref IntVec3 __result)
        {
            Map map = Find.Maps[__instance.mapIndex];
            CellIndices cellIndices = map.cellIndices;
            Region[] directGrid = map.regionGrid.DirectGrid;
            foreach (IntVec3 item in __instance.extentsClose)
            {
                if (directGrid[cellIndices.CellToIndex(item)] == __instance)
                {
                    __result = item;
                    return false;
                }
            }

            Log.Warning("Couldn't find any cell in region " + __instance.ToString());
            __result = __instance.extentsClose.RandomCell;
            return false;

        }
        public static bool DangerFor(Region __instance, ref Danger __result, Pawn p)
		{
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (cachedDangersForFrame(__instance) != Time.frameCount)
                {
                    lock (cachedDangers(__instance))
                    {
                        cachedDangers(__instance).Clear();
                    }
                    cachedDangersForFrame(__instance) = Time.frameCount;
                }
                else
                {
                    lock (cachedDangers(__instance)) { 
                        for (int index = 0; index < cachedDangers(__instance).Count; ++index)
                        {
                            if (cachedDangers(__instance)[index].Key == p)
                            {
                                __result = cachedDangers(__instance)[index].Value;
                                return false;
                            }
                        }
                    }
                }
            }
            Danger danger = Danger.Unspecified;
            Room room = __instance.Room;
            RoomGroup group = null;
            if (room != null)
            {
                group = room.Group;
            }
            if (room != null && group != null)
            {
                float temperature = group.Temperature;
                FloatRange floatRange;
                if (Current.ProgramState == ProgramState.Playing)
                {
                    if (cachedSafeTemperatureRangesForFrame != Time.frameCount)
                    {
                        lock (cachedSafeTemperatureRanges)
                        {
                            cachedSafeTemperatureRanges.Clear();
                        }
                        cachedSafeTemperatureRangesForFrame = Time.frameCount;
                    }
                    lock (cachedSafeTemperatureRanges)
                    {
                        if (!cachedSafeTemperatureRanges.TryGetValue(p, out floatRange))
                        {
                            floatRange = p.SafeTemperatureRange();
                            cachedSafeTemperatureRanges.Add(p, floatRange);
                        }
                    }
                }
                else
                    floatRange = p.SafeTemperatureRange();
                danger = !floatRange.Includes(temperature) ? (!floatRange.ExpandedBy(80f).Includes(temperature) ? Danger.Deadly : Danger.Some) : Danger.None;
                if (Current.ProgramState == ProgramState.Playing)
                {
                    lock (cachedDangers(__instance))
                    {
                        cachedDangers(__instance).Add(new KeyValuePair<Pawn, Danger>(p, danger));
                    }
                }
            }
            __result = danger;
            return false;
        }

	}
}
