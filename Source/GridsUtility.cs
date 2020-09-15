using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class GridsUtility_Patch
    {

        public static bool GetTerrain(ref TerrainDef __result, IntVec3 c, Map map)
        {
            __result = null;
            if (null != map)
            {
                if (null != map.terrainGrid)
                {
                    __result = map.terrainGrid.TerrainAt(c);
                }
            }
            return false;
        }
        public static bool GetGas(ref Gas __result, IntVec3 c, Map map)
        {
            List<Thing> thingList = c.GetThingList(map);
            Thing thing;
            for (int i = 0; i < thingList.Count; i++)
            {
                try
                {
                    thing = thingList[i];
                }
                catch (IndexOutOfRangeException) { break; }
                if (thingList[i].def.category == ThingCategory.Gas)
                {
                    __result = (Gas)thingList[i];
                    return false;
                }                
            } 
            __result = null;
            return false;
        }
    }
}
