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

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Region);
            Type patched = typeof(Region_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(DangerFor));
#if RW13
            RimThreadedHarmony.Prefix(original, patched, nameof(get_District));
#endif
        }

        internal static void InitializeThreadStatics()
        {
            regionClosedIndex = new Dictionary<Region, uint[]>();
        }

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
#if RW12
        public static bool get_Room(Region __instance, ref Room __result)
#endif
#if RW13
        public static bool get_District(Region __instance, ref District __result)
        
#endif
        {
            lock (RegionDirtyer_Patch.regionDirtyerLock)
            {
#if RW12
                __result = __instance.roomInt;
#endif
#if RW13
                __result = __instance.districtInt;
#endif
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
#if RW12
            RoomGroup group = null;
#endif
#if RW13
            District district = null;
#endif
            if (room != null)
            {
#if RW12
                group = room.Group;
#endif
#if RW13
                district = __instance.District;
#endif
            }
#if RW12
            if (room != null && group != null)
#endif
#if RW13
            if (room != null && district != null)
#endif
            {
#if RW12
                float temperature = group.Temperature;
#endif
#if RW13
                float temperature = room.Temperature;
#endif
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
