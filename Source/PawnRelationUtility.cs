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

    public class PawnRelationUtility_Patch
	{
        public static bool GetMostImportantColonyRelative(ref Pawn __result, Pawn pawn)
        {
            if (pawn.relations == null || !pawn.relations.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                __result = null;
                return false;
            }

            IEnumerable<Pawn> enumerable = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners.Where((Pawn x) => x.relations.everSeenByPlayer);
            float num = 0f;
            Pawn pawn2 = null;
            foreach (Pawn item in enumerable.ToList())
            {
                PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(item);
                if (mostImportantRelation != null && (pawn2 == null || mostImportantRelation.importance > num))
                {
                    num = mostImportantRelation.importance;
                    pawn2 = item;
                }
            }

            __result = pawn2;
            return false;
        }

    }
}
