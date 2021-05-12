using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace RimThreaded
{

    public class Region_Patch
    {
        public static AccessTools.FieldRef<Region, Room> roomIntFieldRef =
            AccessTools.FieldRefAccess<Region, Room>("roomInt");
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

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Region);
            Type patched = typeof(Region_Patch);
            RimThreadedHarmony.Prefix(original, patched, "DangerFor");
            RimThreadedHarmony.Prefix(original, patched, "get_Room");
        }


        public static bool get_Room(Region __instance, ref Room __result)
        {
            lock (RegionDirtyer_Patch.regionDirtyerLock)
            {
                __result = roomIntFieldRef(__instance);
                return false;
            }
        }
        public static bool DangerFor(Region __instance, ref Danger __result, Pawn p)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (cachedDangersForFrame(__instance) != Time_Patch.get_frameCount())
                {
                    lock (__instance)
                    {
                        cachedDangers(__instance) = new List<KeyValuePair<Pawn, Danger>>();
                    }
                    cachedDangersForFrame(__instance) = Time_Patch.get_frameCount();
                }
                else
                {
                    List<KeyValuePair<Pawn, Danger>> localCachedDangers = cachedDangers(__instance);
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
                    if (cachedSafeTemperatureRangesForFrame != Time_Patch.get_frameCount())
                    {
                        lock (cachedSafeTemperatureRanges)
                        {
                            cachedSafeTemperatureRanges = new Dictionary<Pawn, FloatRange>();
                        }
                        cachedSafeTemperatureRangesForFrame = Time_Patch.get_frameCount();
                    }
                    if (!cachedSafeTemperatureRanges.TryGetValue(p, out floatRange))
                    {
                        floatRange = p.SafeTemperatureRange();
                        lock (cachedSafeTemperatureRanges)
                        {
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
