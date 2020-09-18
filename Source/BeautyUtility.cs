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

    public class BeautyUtility_Patch
	{
        public static bool AverageBeautyPerceptible(ref float __result, IntVec3 root, Map map)
        {
            if (!root.IsValid || !root.InBounds(map))
            {
                __result = 0.0f;
                return false;
            }
            //BeautyUtility.tempCountedThings.Clear();
            List<Thing> tempCountedThings = new List<Thing>();
            float num1 = 0.0f;
            int num2 = 0;
            BeautyUtility.FillBeautyRelevantCells(root, map);
            IntVec3 cells;
            for (int index = 0; index < BeautyUtility.beautyRelevantCells.Count; ++index)
            {
                try
                {
                    cells = BeautyUtility.beautyRelevantCells[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                num1 += BeautyUtility.CellBeauty(cells, map, tempCountedThings);
                ++num2;
            }
            //BeautyUtility.tempCountedThings.Clear();
            __result = num2 == 0 ? 0.0f : num1 / (float)num2;
            return false;
        }

    }
}
