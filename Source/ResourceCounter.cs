using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class ResourceCounter_Patch
    {

        public static List<ThingDef> resources = StaticFieldRefAccess<List<ThingDef>>(typeof(ResourceCounter), "resources");
        private static readonly FieldRef<ResourceCounter, Dictionary<ThingDef, int>> countedAmountsFieldRef = FieldRefAccess<ResourceCounter, Dictionary<ThingDef, int>>("countedAmounts");


        public static FieldRef<ResourceCounter, Map> map =
            FieldRefAccess<ResourceCounter, Map>("map");
        public static FieldRef<ResourceCounter, Dictionary<ThingDef, int>> countedAmounts =
            FieldRefAccess<ResourceCounter, Dictionary<ThingDef, int>>("countedAmounts");

        public static object lockObject = new object();

        internal static void RunDestructivePatches()
        {
            Type original = typeof(ResourceCounter);
            Type patched = typeof(ResourceCounter_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ResetDefs");
            RimThreadedHarmony.Prefix(original, patched, "ResetResourceCounts");
            RimThreadedHarmony.Prefix(original, patched, "GetCount"); //maybe not needed
        }

        public static bool ResetDefs()
        {
            lock (lockObject)
            {
                resources = new List<ThingDef>((from def in DefDatabase<ThingDef>.AllDefs
                                               where def.CountAsResource
                                               orderby def.resourceReadoutPriority descending
                                               select def));
            }
            return false;
        }

        public static bool ResetResourceCounts(ResourceCounter __instance)
        {
            lock (lockObject)
            {                
                Dictionary<ThingDef, int> newCountedAmounts = new Dictionary<ThingDef, int>();
                List<ThingDef> tempResources = resources;
                for (int i = 0; i < tempResources.Count; i++)
                {
                    __instance.AllCountedAmounts.Add(tempResources[i], 0);
                }
                countedAmountsFieldRef(__instance) = newCountedAmounts;
            }

            return false;
        }

        public static bool GetCount(ResourceCounter __instance, ref int __result, ThingDef rDef)
        {
            if (rDef.resourceReadoutPriority == ResourceCountPriority.Uncounted)
            {
                __result = 0;
                return false;
            }

            lock (lockObject)
            {
                if (__instance.AllCountedAmounts.TryGetValue(rDef, out int value))
                {
                    __result = value;
                    return false;
                }

                Log.Error("Looked for nonexistent key " + rDef + " in counted resources.");
                {
                    __instance.AllCountedAmounts.Add(rDef, 0);
                }
            }
            __result = 0;
            return false;
        }


    }
}
