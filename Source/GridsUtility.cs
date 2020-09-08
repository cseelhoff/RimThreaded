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

    }
}
