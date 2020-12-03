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
            List<Pawn> pawnList = PawnsFinder.AllMapsWorldAndTemporary_Alive;
			for (int p = 0; p < pawnList.Count; p++)
			{
				Pawn pawn2;
                try
                {
					pawn2 = pawnList[p];
				} catch (ArgumentOutOfRangeException)
                {
					break;
                }
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
		public static bool RemoveDiedThoughts(Pawn pawn)
		{
            //Pawn[] pawnsAlive = PawnsFinder.AllMapsWorldAndTemporary_Alive.ToArray();
			List<Pawn> pawnsAlive = PawnsFinder.AllMapsWorldAndTemporary_Alive;
			//foreach (Pawn pawn2 in PawnsFinder.AllMapsWorldAndTemporary_Alive)
			for (int a = 0; a < pawnsAlive.Count; a++)
			{
				Pawn pawn2;
				try
				{
					pawn2 = pawnsAlive[a];
				}
				catch(ArgumentOutOfRangeException)
                {
					break;
                }
				if (null != pawn.needs)
				{
					if (pawn2.needs != null && pawn2.needs.mood != null && pawn2 != pawn)
					{
						MemoryThoughtHandler memories = pawn2.needs.mood.thoughts.memories;
						memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.KnowColonistDied, pawn);
						memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.KnowPrisonerDiedInnocent, pawn);
						memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.PawnWithGoodOpinionDied, pawn);
						memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.PawnWithBadOpinionDied, pawn);
						List<PawnRelationDef> allDefsListForReading = DefDatabase<PawnRelationDef>.AllDefsListForReading;
						for (int i = 0; i < allDefsListForReading.Count; i++)
						{
							ThoughtDef genderSpecificDiedThought = allDefsListForReading[i].GetGenderSpecificDiedThought(pawn);
							if (genderSpecificDiedThought != null)
							{
								memories.RemoveMemoriesOfDefWhereOtherPawnIs(genderSpecificDiedThought, pawn);
							}
						}
					}
				}
			}
			return false;
		}
		public static bool RemoveResuedRelativeThought(Pawn pawn)
		{
            List<Pawn> pawnList = PawnsFinder.AllMapsWorldAndTemporary_Alive;
			for (int i = 0; i < pawnList.Count; i++)
			{
				Pawn pawn2;
				try
                {
					pawn2 = pawnList[i];
				} catch (ArgumentOutOfRangeException)
				{ break; }
				if (pawn2 != null && pawn2.needs != null && pawn2.needs.mood != null && pawn2 != pawn)
				{
					pawn2.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.RescuedRelative, pawn);
				}
			}
			return false;
		}
	}
}
