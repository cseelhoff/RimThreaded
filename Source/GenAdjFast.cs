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

    public class GenAdjFast_Patch
	{
        public static bool AdjacentCells8Way(ref List<IntVec3> __result, IntVec3 root)
        {
            //GenAdjFast.resultList.Clear();
            List<IntVec3> resultList = new List<IntVec3>();
            //GenAdjFast.working = true;
            for (int index = 0; index < 8; ++index)
                resultList.Add(root + GenAdj.AdjacentCells[index]);
            //GenAdjFast.working = false;
            __result = resultList;
            return false;
        }
        public static bool AdjacentCells8Way(ref List<IntVec3> __result,
      IntVec3 thingCenter,
      Rot4 thingRot,
      IntVec2 thingSize)
        {
            if (thingSize.x == 1 && thingSize.z == 1)
            {
                List<IntVec3> r1 = null;
                AdjacentCells8Way(ref r1, thingCenter);
                __result = r1;
                return false;
            }
            //if (GenAdjFast.working)
            //throw new InvalidOperationException("GenAdjFast is already working.");
            //GenAdjFast.resultList.Clear();
            List<IntVec3> resultList = new List<IntVec3>();
            //GenAdjFast.working = true;
            GenAdj.AdjustForRotation(ref thingCenter, ref thingSize, thingRot);
            int num1 = thingCenter.x - (thingSize.x - 1) / 2 - 1;
            int num2 = num1 + thingSize.x + 1;
            int newZ = thingCenter.z - (thingSize.z - 1) / 2 - 1;
            int num3 = newZ + thingSize.z + 1;
            IntVec3 intVec3 = new IntVec3(num1 - 1, 0, newZ);
            do
            {
                ++intVec3.x;
                resultList.Add(intVec3);
            }
            while (intVec3.x < num2);
            do
            {
                ++intVec3.z;
                resultList.Add(intVec3);
            }
            while (intVec3.z < num3);
            do
            {
                --intVec3.x;
                resultList.Add(intVec3);
            }
            while (intVec3.x > num1);
            do
            {
                --intVec3.z;
                resultList.Add(intVec3);
            }
            while (intVec3.z > newZ + 1);
            //GenAdjFast.working = false;
            __result = resultList;
            return false;
        }
        public static bool AdjacentCellsCardinal(ref List<IntVec3> __result, IntVec3 root)
        {
            //if (GenAdjFast.working)
                //throw new InvalidOperationException("GenAdjFast is already working.");
            //GenAdjFast.resultList.Clear();
            List<IntVec3> resultList = new List<IntVec3>();
            //GenAdjFast.working = true;
            for (int index = 0; index < 4; ++index)
                resultList.Add(root + GenAdj.CardinalDirections[index]);
            //GenAdjFast.working = false;
            __result = resultList;
            return false;
        }
        public static bool AdjacentCellsCardinal(ref List<IntVec3> __result,
      IntVec3 thingCenter,
      Rot4 thingRot,
      IntVec2 thingSize)
        {
            if (thingSize.x == 1 && thingSize.z == 1)
            {
                List<IntVec3> r1 = null;
                AdjacentCellsCardinal(ref r1, thingCenter);
                __result = r1;
                return false;
            }
            //if (GenAdjFast.working)
            //throw new InvalidOperationException("GenAdjFast is already working.");
            //GenAdjFast.resultList.Clear();
            List<IntVec3> resultList = new List<IntVec3>();
            //GenAdjFast.working = true;
            GenAdj.AdjustForRotation(ref thingCenter, ref thingSize, thingRot);
            int newX = thingCenter.x - (thingSize.x - 1) / 2 - 1;
            int num1 = newX + thingSize.x + 1;
            int newZ = thingCenter.z - (thingSize.z - 1) / 2 - 1;
            int num2 = newZ + thingSize.z + 1;
            IntVec3 intVec3 = new IntVec3(newX, 0, newZ);
            do
            {
                ++intVec3.x;
                resultList.Add(intVec3);
            }
            while (intVec3.x < num1 - 1);
            ++intVec3.x;
            do
            {
                ++intVec3.z;
                resultList.Add(intVec3);
            }
            while (intVec3.z < num2 - 1);
            ++intVec3.z;
            do
            {
                --intVec3.x;
                resultList.Add(intVec3);
            }
            while (intVec3.x > newX + 1);
            --intVec3.x;
            do
            {
                --intVec3.z;
                resultList.Add(intVec3);
            }
            while (intVec3.z > newZ + 1);
            //GenAdjFast.working = false;
            __result = resultList;
            return false;
        }
    }
}
