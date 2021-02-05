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
    public class FireUtility_Patch
    {
        public static bool ContainsStaticFire(ref bool __result, IntVec3 c, Map map)
        {
            List<Thing> thingList = map.thingGrid.ThingsListAt(c);

            for (int index = 0; index < thingList.Count; ++index)
            {
                Thing thing;
                try
                {
                    thing = thingList[index];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (thing is Fire fire && fire.parent == null)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }


        public static bool ChanceToStartFireIn(ref float __result, IntVec3 c, Map map)
        {
            List<Thing> thingList = c.GetThingList(map);
            float num = c.TerrainFlammableNow(map) ? c.GetTerrain(map).GetStatValueAbstract(StatDefOf.Flammability) : 0f;
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing is Fire)
                {
                    __result = 0f;
                    return false;
                }

                if (thing != null && thing.def != null && thing.def.category != ThingCategory.Pawn && thing.FlammableNow)
                {
                    num = Mathf.Max(num, thing.GetStatValue(StatDefOf.Flammability));
                }
            }

            if (num > 0f)
            {
                Building edifice = c.GetEdifice(map);
                if (edifice != null && edifice.def.passability == Traversability.Impassable && edifice.OccupiedRect().ContractedBy(1).Contains(c))
                {
                    __result = 0f;
                    return false;
                }

                List<Thing> thingList2 = c.GetThingList(map);
                for (int j = 0; j < thingList2.Count; j++)
                {
                    if (thingList2[j].def.category == ThingCategory.Filth && !thingList2[j].def.filth.allowsFire)
                    {
                        __result = 0f;
                        return false;
                    }
                }
            }

            __result = num;
            return false;
        }

    }
}
