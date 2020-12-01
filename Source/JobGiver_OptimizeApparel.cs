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

    public class JobGiver_OptimizeApparel_Patch
	{
        public static bool ApparelScoreGain(ref float __result, Pawn pawn, Apparel ap)
        {
            List<float> wornApparelScores = new List<float>();
            //wornApparelScores.Clear();
            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel;
                try
                {
                    apparel = pawn.apparel.WornApparel[i];
                } catch(ArgumentOutOfRangeException)
                {
                    break;
                }
                wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, apparel));
            }
            float scoreGain = 0f;
            ApparelScoreGain_NewTmp(ref scoreGain, pawn, ap, wornApparelScores);
            __result = scoreGain;
            return false;
        }
        public static bool ApparelScoreGain_NewTmp(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
        {
            if (ap is ShieldBelt && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWeaponUsingProjectiles)
            {
                __result = - 1000f;
                return false;
            }

            float num = JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel;
                try
                {
                    apparel = wornApparel[i];
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (!ApparelUtility.CanWearTogether(apparel.def, ap.def, pawn.RaceProps.body))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(apparel) || pawn.apparel.IsLocked(apparel))
                    {
                        __result = - 1000f;
                        return false;
                    }

                    num -= wornScoresCache[i];
                    flag = true;
                }
            }

            if (!flag)
            {
                num *= 10f;
            }

            __result = num;
            return false;
        }
    }
}
