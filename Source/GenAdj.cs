using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class GenAdj_Patch
    {
        [ThreadStatic] public static List<IntVec3> validCells;

        public static void InitializeThreadStatics()
        {
            validCells = new List<IntVec3>();
        }

    }
}
