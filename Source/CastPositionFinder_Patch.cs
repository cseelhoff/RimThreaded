using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class CastPositionFinder_Patch
    {

        public static CastPositionRequest req = StaticFieldRefAccess<CastPositionRequest>(typeof(CastPositionFinder), "req");
        public static float rangeFromTarget = StaticFieldRefAccess<float>(typeof(CastPositionFinder), "rangeFromTarget");
        public static float rangeFromTargetToCellSquared = StaticFieldRefAccess<float>(typeof(CastPositionFinder), "rangeFromTargetToCellSquared");
        public static float optimalRangeSquared = StaticFieldRefAccess<float>(typeof(CastPositionFinder), "optimalRangeSquared");
        public static float rangeFromCasterToCellSquared = StaticFieldRefAccess<float>(typeof(CastPositionFinder), "rangeFromCasterToCellSquared");
        public static float rangeFromTargetSquared = StaticFieldRefAccess<float>(typeof(CastPositionFinder), "rangeFromTargetSquared");
        public static bool CastPositionPreference(IntVec3 c, ref float __result)
        {
            bool flag = true;
            if (req.caster != null && req.caster.Map != null)
            {
                List<Thing> list = req.caster.Map.thingGrid.ThingsListAtFast(c);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing = list[i];
                    Fire fire = thing as Fire;
                    if (fire != null && fire.parent == null)
                    {
                        __result = -1f;
                        return false;
                    }

                    if (thing.def.passability == Traversability.PassThroughOnly)
                    {
                        flag = false;
                    }
                }
            }

            float num = 0.3f;
            if (req.caster.kindDef.aiAvoidCover)
            {
                num += 8f - CoverUtility.TotalSurroundingCoverScore(c, req.caster.Map);
            }

            if (req.wantCoverFromTarget)
            {
                num += CoverUtility.CalculateOverallBlockChance(c, req.target.Position, req.caster.Map) * 0.55f;
            }

            float num2 = (req.caster.Position - c).LengthHorizontal;
            if (rangeFromTarget > 100f)
            {
                num2 -= rangeFromTarget - 100f;
                if (num2 < 0f)
                {
                    num2 = 0f;
                }
            }

            num *= Mathf.Pow(0.967f, num2);
            float num3 = 1f;
            rangeFromTargetToCellSquared = (c - req.target.Position).LengthHorizontalSquared;
            float num4 = Mathf.Abs(rangeFromTargetToCellSquared - optimalRangeSquared) / optimalRangeSquared;
            num4 = 1f - num4;
            num4 = 0.7f + 0.3f * num4;
            num3 *= num4;
            if (rangeFromTargetToCellSquared < 25f)
            {
                num3 *= 0.5f;
            }

            num *= num3;
            if (rangeFromCasterToCellSquared > rangeFromTargetSquared)
            {
                num *= 0.4f;
            }

            if (!flag)
            {
                num *= 0.2f;
            }

            __result = num;
            return false;
        }

    }
}
