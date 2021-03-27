using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace RimThreaded
{

	public class PawnDiedOrDownedThoughtsUtility_Patch
	{
		[ThreadStatic] public static List<IndividualThoughtToAdd> tmpIndividualThoughtsToAdd = new List<IndividualThoughtToAdd>();
		[ThreadStatic] public static List<ThoughtToAddToAll> tmpAllColonistsThoughts = new List<ThoughtToAddToAll>();

		public static void InitializeThreadStatics()
        {
			tmpIndividualThoughtsToAdd = new List<IndividualThoughtToAdd>();
			tmpAllColonistsThoughts = new List<ThoughtToAddToAll>();
		}

		public static void RunNonDestructivePatches()
        {
			Type original = typeof(PawnDiedOrDownedThoughtsUtility);
			Type patched = typeof(PawnDiedOrDownedThoughtsUtility_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "TryGiveThoughts");
			RimThreadedHarmony.TranspileFieldReplacements(original, "BuildMoodThoughtsListString",
				new Type[] { typeof(Pawn), typeof(DamageInfo), typeof(PawnDiedOrDownedThoughtsKind), typeof(StringBuilder), typeof(string), typeof(string) });
			RimThreadedHarmony.TranspileFieldReplacements(original, "BuildMoodThoughtsListString",
				new Type[] { typeof(IEnumerable <Pawn>), typeof(PawnDiedOrDownedThoughtsKind), typeof(StringBuilder), typeof(string), typeof(string), typeof(string) });
		}

	}
}
