using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class TickList_Patch
    {
        public static FieldRef<TickList, List<List<Thing>>> thingLists =
            FieldRefAccess<TickList, List<List<Thing>>>("thingLists");
        public static FieldRef<TickList, List<Thing>> thingsToRegister =
            FieldRefAccess<TickList, List<Thing>>("thingsToRegister");
        public static FieldRef<TickList, List<Thing>> thingsToDeregister =
            FieldRefAccess<TickList, List<Thing>>("thingsToDeregister");
        public static FieldRef<TickList, TickerType> tickType =
            FieldRefAccess<TickList, TickerType>("tickType");


        private static readonly MethodInfo methodGetTickInterval =
            Method(typeof(TickList), "get_TickInterval");
        private static readonly Func<TickList, int> funcGetTickInterval =
            (Func<TickList, int>)Delegate.CreateDelegate(typeof(Func<TickList, int>), methodGetTickInterval);

        public static void RunDestructivePatches()
        {
            Type original = typeof(TickList);
            Type patched = typeof(TickList_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Tick");
        }

        public static bool DeregisterThing(TickList __instance, Thing t)
        {
            lock (thingsToDeregister(__instance))
            {
                thingsToDeregister(__instance).Add(t);
            }
            return false;
        }
        public static bool RegisterThing(TickList __instance, Thing t)
        {
            lock (thingsToRegister(__instance))
            {
                thingsToRegister(__instance).Add(t);
            }
            return false;
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
            int currentTickInterval = funcGetTickInterval(__instance);

            Thing i;
            List<Thing> tr = thingsToRegister(__instance);
            for (int index = 0; index < tr.Count; ++index)
            {
                try
                {
                    i = tr[index];
                } catch (ArgumentOutOfRangeException) { break; }
                List<Thing> b = BucketOf2(__instance, i, currentTickInterval);
                b.Add(i);
            }
            lock (tr)
            {
                tr.Clear();
            }

            List<Thing> td = thingsToDeregister(__instance);
            for (int index = 0; index < td.Count; ++index)
            {
                try
                {
                    i = td[index];
                } catch (ArgumentOutOfRangeException) { break; }
                List<Thing> b = BucketOf2(__instance, i, currentTickInterval);
                b.Remove(i);
            }
            lock (td)
            {
                td.Clear();
            }

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
