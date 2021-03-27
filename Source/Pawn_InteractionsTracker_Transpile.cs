using System.Collections.Generic;
using Verse;
using RimWorld;
using System;

namespace RimThreaded
{
    public class Pawn_InteractionsTracker_Transpile
	{
		[ThreadStatic] public static List<Pawn> workingList;

		public static void InitializeThreadStatics()
        {
			workingList = new List<Pawn>();
		}

		internal static void RunNonDestructivePatches()
		{
			Type original = typeof(Pawn_InteractionsTracker);
			Type patched = typeof(Pawn_InteractionsTracker_Transpile);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "TryInteractRandomly");
		}


    }
}
