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

    public class GenRadial_Patch
	{
        public static bool ProcessEquidistantCells(
      IntVec3 center,
      float radius,
      Func<List<IntVec3>, bool> processor,
      Map map = null)
        {
            //if (GenRadial.working)
            //{
                //Log.Error("Nested calls to ProcessEquidistantCells() are not allowed.", false);
            //}
            //else
            //{
                //GenRadial.tmpCells.Clear();
                List<IntVec3> tmpCells = new List<IntVec3>();
                //GenRadial.working = true;
                try
                {
                    float num1 = -1f;
                    int num2 = GenRadial.NumCellsInRadius(radius);
                    for (int index = 0; index < num2; ++index)
                    {
                        IntVec3 intVec3 = center + GenRadial.RadialPattern[index];
                        if (map == null || intVec3.InBounds(map))
                        {
                            float squared = intVec3.DistanceToSquared(center);
                            if (Mathf.Abs(squared - num1) > 9.99999974737875E-05)
                            {
                                if (tmpCells.Any() && processor(tmpCells))
                                    return false;
                                num1 = squared;
                                tmpCells.Clear();
                            }
                            tmpCells.Add(intVec3);
                        }
                    }
                    if (!tmpCells.Any())
                        return false;
                    int num3 = processor(tmpCells) ? 1 : 0;
                }
                finally
                {
                    //GenRadial.tmpCells.Clear();
                    //GenRadial.working = false;
                }
            //}
            return false;
        }

    }
}
