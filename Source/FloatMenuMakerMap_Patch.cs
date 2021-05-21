using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class FloatMenuMakerMap_Patch
	{
        [ThreadStatic] public static List<Pawn> tmpPawns;

        public static void InitializeThreadStatics()
        {
            tmpPawns = new List<Pawn>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(FloatMenuMakerMap);
            Type patched = typeof(FloatMenuMakerMap_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryMakeMultiSelectFloatMenu");
            patched = typeof(FloatMenuMakerMap_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "AddHumanlikeOrders");
        }
    }
}
