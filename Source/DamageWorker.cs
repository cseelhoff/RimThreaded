using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class DamageWorker_Patch
    {
        [ThreadStatic] public static List<Thing> thingsToAffect;
        [ThreadStatic] public static List<IntVec3> openCells;
        [ThreadStatic] public static List<IntVec3> adjWallCells;

        public static void InitializeThreadStatics()
        {
            thingsToAffect = new List<Thing>();
            openCells = new List<IntVec3>();
            adjWallCells = new List<IntVec3>();
        }


    }
}