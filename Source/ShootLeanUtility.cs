
using System.Collections.Generic;
using Verse;
using System.Collections.Concurrent;
using System;

namespace RimThreaded
{

    public class ShootLeanUtility_Patch
    {
        public static ConcurrentQueue<bool[]> conBlockedArrays = new ConcurrentQueue<bool[]>();
        [ThreadStatic] public static List<IntVec3> listToFill_static;

        public static void RunDestructivePatches()
        {
            Type original = typeof(ShootLeanUtility);
            Type patched = typeof(ShootLeanUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "LeanShootingSourcesFromTo");
        }


        private static void ReturnWorkingBlockedArray(bool[] ar)
        {
            conBlockedArrays.Enqueue(ar);
            if (conBlockedArrays.Count > 128)
            {
                Log.ErrorOnce("Too many blocked arrays to be feasible. >128", 388121, false);
            }
        }
        private static bool[] GetWorkingBlockedArray()
        {
            if (!conBlockedArrays.TryDequeue(out bool[] boolArray))
            {
                boolArray = new bool[8];
            }
            return boolArray;
        }
        public static bool LeanShootingSourcesFromTo(IntVec3 shooterLoc, IntVec3 targetPos, Map map, List<IntVec3> listToFill)
        {
            listToFill_static = listToFill;
            listToFill_static.Clear();
            
            float angleFlat = (targetPos - shooterLoc).AngleFlat;
            bool flag = angleFlat > 270f || angleFlat < 90f;
            bool flag2 = angleFlat > 90f && angleFlat < 270f;
            bool flag3 = angleFlat > 180f;
            bool flag4 = angleFlat < 180f;
            bool[] workingBlockedArray = GetWorkingBlockedArray();
            for (int i = 0; i < 8; i++)
            {
                workingBlockedArray[i] = !(shooterLoc + GenAdj.AdjacentCells[i]).CanBeSeenOver(map);
            }
            if (!workingBlockedArray[1] && ((workingBlockedArray[0] && !workingBlockedArray[5] && flag) || (workingBlockedArray[2] && !workingBlockedArray[4] && flag2)))
            {
                
                    listToFill_static.Add(shooterLoc + new IntVec3(1, 0, 0));
                
            }
            if (!workingBlockedArray[3] && ((workingBlockedArray[0] && !workingBlockedArray[6] && flag) || (workingBlockedArray[2] && !workingBlockedArray[7] && flag2)))
            {
                
                    listToFill_static.Add(shooterLoc + new IntVec3(-1, 0, 0));
                
            }
            if (!workingBlockedArray[2] && ((workingBlockedArray[3] && !workingBlockedArray[7] && flag3) || (workingBlockedArray[1] && !workingBlockedArray[4] && flag4)))
            {
                
                    listToFill_static.Add(shooterLoc + new IntVec3(0, 0, -1));
                
            }
            if (!workingBlockedArray[0] && ((workingBlockedArray[3] && !workingBlockedArray[6] && flag3) || (workingBlockedArray[1] && !workingBlockedArray[5] && flag4)))
            {
                
                    listToFill_static.Add(shooterLoc + new IntVec3(0, 0, 1));
                
            }
            for (int j = 0; j < 4; j++)
            {
                if (!workingBlockedArray[j] && (j != 0 || flag) && (j != 1 || flag4) && (j != 2 || flag2) && (j != 3 || flag3) && (shooterLoc + GenAdj.AdjacentCells[j]).GetCover(map) != null)
                {
                    
                        listToFill_static.Add(shooterLoc + GenAdj.AdjacentCells[j]);
                    
                }
            }
            listToFill = listToFill_static; // what is this list even for...
            ReturnWorkingBlockedArray(workingBlockedArray);
            return false;
        }

    }
}
