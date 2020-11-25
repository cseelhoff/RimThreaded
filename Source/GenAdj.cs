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

    public class GenAdj_Patch
    {
        public static bool TryFindRandomAdjacentCell8WayWithRoomGroup(ref bool __result, IntVec3 center, Rot4 rot, IntVec2 size, Map map, out IntVec3 result)
        {
            GenAdj.AdjustForRotation(ref center, ref size, rot);
            //validCells.Clear();
            List<IntVec3> validCells = new List<IntVec3>();
            foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(center, rot, size))
            {
                if (item.InBounds(map) && item.GetRoomGroup(map) != null)
                {
                    validCells.Add(item);
                }
            }
            
            __result = validCells.TryRandomElement(out result);
            return false;
        }

    }
}
