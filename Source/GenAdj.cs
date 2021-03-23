using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class GenAdj_Patch
    {
        [ThreadStatic]
        static List<IntVec3> validCells;
        public static bool TryFindRandomAdjacentCell8WayWithRoomGroup(ref bool __result, IntVec3 center, Rot4 rot, IntVec2 size, Map map, out IntVec3 result)
        {
            GenAdj.AdjustForRotation(ref center, ref size, rot);
            if (validCells == null)
            {
                validCells = new List<IntVec3>();
            } else {
                validCells.Clear();
            }
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
