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

            float num = 0f;
            Pawn pawn2 = null;
            //IEnumerable<Pawn> enumerable = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners.Where((Pawn x) => x.relations.everSeenByPlayer);
            List<Pawn> pawnList = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners;
            for (int i = 0; i < pawnList.Count; i++)
            {
                Pawn x;
                try
                {
                    x = pawnList[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (x != null && x.relations.everSeenByPlayer)
                {
                    PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(x);
                    if (mostImportantRelation != null && (pawn2 == null || mostImportantRelation.importance > num))
                    {
                        num = mostImportantRelation.importance;
                        pawn2 = x;
                    }
                }
            }
            __result = pawn2;
            return false;
        }

    }
}
