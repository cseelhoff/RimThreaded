using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class GenAdjFast_Patch
	{
        [ThreadStatic] public static List<IntVec3> resultList;
        [ThreadStatic] public static bool working;

        public static void InitializeThreadStatics()
        {
            resultList = new List<IntVec3>();
            working = false;
        }

    }
}
