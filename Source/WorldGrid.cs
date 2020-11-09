using RimWorld.Planet;
using System;
using System.Collections.Generic;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class WorldGrid_Patch
    {

        public static List<int> tmpNeighbors = 
            StaticFieldRefAccess<List<int>>(typeof(WorldGrid), "tmpNeighbors");

        public static bool IsNeighbor(WorldGrid __instance, ref bool __result, int tile1, int tile2)
        {
            __instance.GetTileNeighbors(tile1, tmpNeighbors);
            __result = false;
            for (int i = 0; i < tmpNeighbors.Count; i++)
            {
                int tmpTile;
                try
                {
                    tmpTile = tmpNeighbors[i];
                } catch(ArgumentOutOfRangeException)
                {
                    break;
                }
                if(tile2 == tmpTile)
                {
                    __result = true;
                    return false;
                }
            }
            return false;
        }

    }
}
