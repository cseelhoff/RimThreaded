using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    class BeautyUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(BeautyUtility);
            Type patched = typeof(BeautyUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CellBeauty));
        }
        public static bool CellBeauty(ref float __result, IntVec3 c, Map map, List<Thing> countedThings = null)
        {
            float num1 = 0.0f;
            float num2 = 0.0f;
            bool flag = false;
            if (map == null) //added
            {
                __result = 0f;
                return false;
            }
            ThingGrid thingGrid = map.thingGrid;
            if (thingGrid == null) //added
            {
                __result = 0f;
                return false;
            }
            List<Thing> thingList = thingGrid.ThingsListAt(c); //changed
            for (int index = 0; index < thingList.Count; ++index)
            {
                Thing thing = thingList[index];
                if (BeautyUtility.BeautyRelevant(thing.def.category))
                {
                    if (countedThings != null)
                    {
                        if (!countedThings.Contains(thing))
                            countedThings.Add(thing);
                        else
                            continue;
                    }
                    SlotGroup slotGroup = thing.GetSlotGroup();
                    if (slotGroup == null || slotGroup.parent == thing || !slotGroup.parent.IgnoreStoredThingsBeauty)
                    {
                        float statValue = thing.GetStatValue(StatDefOf.Beauty);
                        if (thing is Filth && !map.roofGrid.Roofed(c))
                            statValue *= 0.3f;
                        if (thing.def.Fillage == FillCategory.Full)
                        {
                            flag = true;
                            num2 += statValue;
                        }
                        else
                            num1 += statValue;
                    }
                }
            }
            __result = flag ? num2 : num1 + map.terrainGrid.TerrainAt(c).GetStatValueAbstract(StatDefOf.Beauty);
            return false;
        }
    }
}
