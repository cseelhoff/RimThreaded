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
        public static bool IsInPrisonCell(ref bool __result, IntVec3 c, Map map)
        {
            Room roomOrAdjacent = c.GetRoomOrAdjacent(map);
            if (roomOrAdjacent != null)
            {
                __result = roomOrAdjacent.isPrisonCell;
                return false;
            }
            //TODO fix check for null room earlier
            //Log.Error("Checking prison cell status of " + c + " which is not in or adjacent to a room.");
            __result = false;
            return false;
        }
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
