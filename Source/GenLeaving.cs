using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class GenLeaving_Patch
    {
        [ThreadStatic] public static List<IntVec3> tmpCellsCandidates;

        public static void InitializeThreadStatics()
        {
            tmpCellsCandidates = new List<IntVec3>();
        }

    }
}
