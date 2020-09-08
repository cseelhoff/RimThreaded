using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class PawnDiedOrDownedThoughtsUtility_Patch
	{

		public static bool RemoveLostThoughts(Pawn pawn)
		{
			foreach (Pawn pawn2 in PawnsFinder.AllMapsWorldAndTemporary_Alive)
			{
				if (null != pawn2)
				{
					if (null != pawn2.needs)
					{
						if (pawn2.needs != null && pawn2.needs.mood != null && pawn2 != pawn)
						{
							MemoryThoughtHandler memories = pawn2.needs.mood.thoughts.memories;
							memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.ColonistLost, pawn);
							memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.PawnWithGoodOpinionLost, pawn);
							memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.PawnWithBadOpinionLost, pawn);
							List<PawnRelationDef> allDefsListForReading = DefDatabase<PawnRelationDef>.AllDefsListForReading;
							for (int i = 0; i < allDefsListForReading.Count; i++)
							{
								ThoughtDef genderSpecificLostThought = allDefsListForReading[i].GetGenderSpecificLostThought(pawn);
								if (genderSpecificLostThought != null)
								{
									memories.RemoveMemoriesOfDefWhereOtherPawnIs(genderSpecificLostThought, pawn);
								}
							}
						}
					}
				}
			}
			return false;
		}

	}
}
