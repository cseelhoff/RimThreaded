using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class TickList_Patch
    {
        public static AccessTools.FieldRef<TickList, List<List<Thing>>> thingLists =
            AccessTools.FieldRefAccess<TickList, List<List<Thing>>>("thingLists");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToRegister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToRegister");
        public static AccessTools.FieldRef<TickList, List<Thing>> thingsToDeregister =
            AccessTools.FieldRefAccess<TickList, List<Thing>>("thingsToDeregister");
        public static AccessTools.FieldRef<TickList, TickerType> tickType =
            AccessTools.FieldRefAccess<TickList, TickerType>("tickType");

        private static int get_TickInterval2(TickList __instance)
        {
            switch (tickType(__instance))
            {
                case TickerType.Normal:
                    return 1;
                case TickerType.Rare:
                    return 250;
                case TickerType.Long:
                    return 2000;
                default:
                    return -1;
            }            
        }
        private static List<Thing> BucketOf2(TickList __instance, Thing t, int currentTickInterval)
        {
            int hashCode = t.GetHashCode();
            if (hashCode < 0)
                hashCode *= -1;
            return thingLists(__instance)[hashCode % currentTickInterval];
        }
        public static bool Tick(TickList __instance)
        {
            TickerType currentTickType = tickType(__instance);
            int currentTickInterval = get_TickInterval2(__instance);

            List<Thing> tr = thingsToRegister(__instance);
            for (int index = 0; index < tr.Count; ++index)
            {
                Thing i = tr[index];
                List<Thing> b = BucketOf2(__instance, i, currentTickInterval);
                b.Add(i);
            }
            tr.Clear();

            List<Thing> td = thingsToDeregister(__instance);
            for (int index = 0; index < td.Count; ++index)
            {
                Thing i = td[index];
                List<Thing> b = BucketOf2(__instance, i, currentTickInterval);
                b.Remove(i);
            }
            td.Clear();

            if (DebugSettings.fastEcology)
            {
                Find.World.tileTemperatures.ClearCaches();
                for (int index1 = 0; index1 < thingLists(__instance).Count; ++index1)
                {
                    List<Thing> thingList = thingLists(__instance)[index1];
                    for (int index2 = 0; index2 < thingList.Count; ++index2)
                    {
                        if (thingList[index2].def.category == ThingCategory.Plant)
                            thingList[index2].TickLong();
                    }
                }
            }
            
            switch (currentTickType)
            {
                case TickerType.Normal:
                    RimThreaded.thingListNormal = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListNormalTicks = RimThreaded.thingListNormal.Count;
                    break;
                case TickerType.Rare:
                    RimThreaded.thingListRare = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListRareTicks = RimThreaded.thingListRare.Count;
                    break;
                case TickerType.Long:
                    RimThreaded.thingListLong = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListLongTicks = RimThreaded.thingListLong.Count;
                    break;
            }

            return false;            
        }

    }
}
