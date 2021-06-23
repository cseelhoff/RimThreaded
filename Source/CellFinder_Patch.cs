using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class CellFinder_Patch
    {
        [ThreadStatic] public static List<IntVec3>[] mapSingleEdgeCells;

        internal static void InitializeThreadStatics()
        {
            mapSingleEdgeCells = new List<IntVec3>[4];
        }

    }
}
