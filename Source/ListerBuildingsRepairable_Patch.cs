using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class ListerBuildingsRepairable_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(ListerBuildingsRepairable);
            Type patched = typeof(ListerBuildingsRepairable_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(UpdateBuilding));
        }

        public static bool UpdateBuilding(ListerBuildingsRepairable __instance, Building b)
        {
            Faction faction = b.Faction;
            if (faction == null || !b.def.building.repairable)
                return false;
            lock (__instance)
            {
                List<Thing> thingList = __instance.ListFor(faction);
                HashSet<Thing> thingSet = __instance.HashSetFor(faction);
                if (b.HitPoints < b.MaxHitPoints)
                {
                    if (!thingList.Contains((Thing)b))
                        thingList.Add((Thing)b);
                    thingSet.Add((Thing)b);
                }
                else
                {
                    List<Thing> newthingList = new List<Thing>(thingList);
                    newthingList.Remove((Thing)b);
                    __instance.repairables[faction] = newthingList;
                    HashSet<Thing> newthingSet = new HashSet<Thing>(thingSet);
                    newthingSet.Remove((Thing)b);
                    __instance.repairablesSet[faction] = newthingSet;
                }
            }
            return false;
        }
    }
}
