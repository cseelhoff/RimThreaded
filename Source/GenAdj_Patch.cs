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

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenAdj);
            Type patched = typeof(GenAdj_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindRandomAdjacentCell8WayWithRoomGroup", new Type[] {
                typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(Map), typeof(IntVec3).MakeByRefType() });
        }
    }
}
