using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
	public static class PawnUtility_Patch
	{
        public static Dictionary<Pawn, bool> isPawnInvisible = new Dictionary<Pawn, bool>();

        public static bool IsInvisible(ref bool __result, Pawn pawn)
        {
            if (!isPawnInvisible.TryGetValue(pawn, out bool isInvisible))
            {
                lock (isPawnInvisible)
                {
                    if (!isPawnInvisible.TryGetValue(pawn, out bool isInvisible2))
                    {
                        isInvisible = RecalculateInvisibility(pawn);
                    }
                    else
                    {
                        isInvisible = isInvisible2;
                    }
                }
            }
            __result = isInvisible;
            return false;
        }

        public static bool RecalculateInvisibility(Pawn pawn)
        {
            bool isInvisible = false;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].TryGetComp<HediffComp_Invisibility>() != null)
                {
                    isInvisible = true;
                    break;
                }
            }
            isPawnInvisible[pawn] = isInvisible;
            return isInvisible;
        }


    }
}