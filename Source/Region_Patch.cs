using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Region_Patch
    {

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
                __result = __instance.roomInt;
                return false;
            }
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
                    if (Region.cachedSafeTemperatureRangesForFrame != Time_Patch.get_frameCount())
                    {
                        lock (Region.cachedSafeTemperatureRanges)
                        {
                            Region.cachedSafeTemperatureRanges = new Dictionary<Pawn, FloatRange>();
                        }
                        Region.cachedSafeTemperatureRangesForFrame = Time_Patch.get_frameCount();
                    }
                    if (!Region.cachedSafeTemperatureRanges.TryGetValue(p, out floatRange))
                    {
                        floatRange = p.SafeTemperatureRange();
                        lock (Region.cachedSafeTemperatureRanges)
                        {
                            Region.cachedSafeTemperatureRanges.Add(p, floatRange);
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
