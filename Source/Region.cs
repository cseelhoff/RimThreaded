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

namespace RimThreaded
{

    public class Region_Patch
    {
        public static AccessTools.FieldRef<Region, int> cachedDangersForFrame =
            AccessTools.FieldRefAccess<Region, int>("cachedDangersForFrame");
        public static AccessTools.FieldRef<Region, List<KeyValuePair<Pawn, Danger>>> cachedDangers =
            AccessTools.FieldRefAccess<Region, List<KeyValuePair<Pawn, Danger>>>("cachedDangers");
        public static Dictionary<Pawn, FloatRange> cachedSafeTemperatureRanges =
            AccessTools.StaticFieldRefAccess<Dictionary<Pawn, FloatRange>>(typeof(Region), "cachedSafeTemperatureRanges");
        public static int cachedSafeTemperatureRangesForFrame =
            AccessTools.StaticFieldRefAccess<int>(typeof(Region), "cachedSafeTemperatureRangesForFrame");

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
            if (room != null)
            {
                float temperature = room.Temperature;
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
