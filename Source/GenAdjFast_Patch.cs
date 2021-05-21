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

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenAdjFast);
            Type patched = typeof(GenAdjFast_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "AdjacentCells8Way", new Type[] { typeof(IntVec3) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "AdjacentCells8Way", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
        }
    }
}
