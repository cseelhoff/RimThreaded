using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class ResourceCounter_Patch
    {

        public static List<ThingDef> resources = StaticFieldRefAccess<List<ThingDef>>(typeof(ResourceCounter), "resources");

        public static FieldRef<ResourceCounter, Map> map =
            FieldRefAccess<ResourceCounter, Map>("map");
        public static FieldRef<ResourceCounter, Dictionary<ThingDef, int>> countedAmounts =
            FieldRefAccess<ResourceCounter, Dictionary<ThingDef, int>>("countedAmounts");
        public static bool get_TotalHumanEdibleNutrition(ResourceCounter __instance, ref float __result)
		{
            float num = 0f;
            lock (countedAmounts(__instance))
            {
                foreach (KeyValuePair<ThingDef, int> countedAmount in countedAmounts(__instance))
                {
                    if (countedAmount.Key.IsNutritionGivingIngestible && countedAmount.Key.ingestible.HumanEdible)
                    {
                        num += countedAmount.Key.GetStatValueAbstract(StatDefOf.Nutrition) * countedAmount.Value;
                    }
                }
            }
            __result = num;
            return false;
        }
        public static bool ResetDefs(ResourceCounter __instance)
        {
            lock (resources)
            {
                resources.Clear();
                resources.AddRange(from def in DefDatabase<ThingDef>.AllDefs
                                               where def.CountAsResource
                                               orderby def.resourceReadoutPriority descending
                                               select def);
            }
            return false;
        }

        public static bool ResetResourceCounts(ResourceCounter __instance)
        {
            lock (__instance.AllCountedAmounts)
            {
                __instance.AllCountedAmounts.Clear();
                for (int i = 0; i < resources.Count; i++)
                {
                    ThingDef resource;
                    try
                    {
                        resource = resources[i];
                    }
                    catch(ArgumentOutOfRangeException) { break; }
                    __instance.AllCountedAmounts.Add(resource, 0);
                }
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

            lock (__instance.AllCountedAmounts)
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

        public static bool GetCountIn(ResourceCounter __instance, ref int __result, ThingRequestGroup group)
        {
            int num = 0;
            foreach (KeyValuePair<ThingDef, int> countedAmount in __instance.AllCountedAmounts.ToList())
            {
                if (group.Includes(countedAmount.Key))
                {
                    num += countedAmount.Value;
                }
            }

            __result = num;
            return false;
        }
        public static bool UpdateResourceCounts(ResourceCounter __instance)
        {
            __instance.ResetResourceCounts();
            List<SlotGroup> allGroupsListForReading = map(__instance).haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < allGroupsListForReading.Count; i++)
            {
                foreach (Thing heldThing in allGroupsListForReading[i].HeldThings)
                {
                    Thing innerIfMinified = heldThing.GetInnerIfMinified();
                    if (innerIfMinified.def.CountAsResource && (!innerIfMinified.IsNotFresh()))
                    {
                        lock (__instance.AllCountedAmounts)
                        {
                            __instance.AllCountedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                        }
                    }
                }
            }
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(ResourceCounter);
            Type patched = typeof(ResourceCounter_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_TotalHumanEdibleNutrition");
            RimThreadedHarmony.Prefix(original, patched, "ResetDefs");
            RimThreadedHarmony.Prefix(original, patched, "ResetResourceCounts");
            RimThreadedHarmony.Prefix(original, patched, "GetCount");
            RimThreadedHarmony.Prefix(original, patched, "GetCountIn", new Type[] { typeof(ThingRequestGroup) });
            RimThreadedHarmony.Prefix(original, patched, "UpdateResourceCounts");
        }
    }
}
