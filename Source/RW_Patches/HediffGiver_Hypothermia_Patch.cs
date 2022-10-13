using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{
    class HediffGiver_Hypothermia_Patch
    {
        public static bool OnIntervalPassed(HediffGiver_Hypothermia __instance, Pawn pawn, Hediff cause)
        {
            float ambientTemperature = pawn.AmbientTemperature;
            //FloatRange floatRange = pawn.ComfortableTemperatureRange(); //REMOVED
            //FloatRange floatRange2 = pawn.SafeTemperatureRange(); //REMOVED
            float comfortableTemperatureMin = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin); //ADDED
            float minTemp = comfortableTemperatureMin - 10f; //ADDED
            HediffSet hediffSet = pawn.health.hediffSet;
            HediffDef hediffDef = pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid ? __instance.hediffInsectoid : __instance.hediff;
            Hediff firstHediffOfDef = hediffSet.GetFirstHediffOfDef(hediffDef);
            //if (ambientTemperature < floatRange2.min) //REMOVED
            if (ambientTemperature < minTemp) //ADDED
            {
                //float a = Mathf.Abs(ambientTemperature - floatRange2.min) * 6.45E-05f; //REMOVED
                float a = Mathf.Abs(ambientTemperature - minTemp) * 6.45E-05f; //ADDED
                a = Mathf.Max(a, 0.00075f);
                HealthUtility.AdjustSeverity(pawn, hediffDef, a);
                if (pawn.Dead)
                    return false;
            }
            if (firstHediffOfDef == null)
                return false;
            //if (ambientTemperature > floatRange.min) //REMOVED
            if (ambientTemperature > comfortableTemperatureMin) //ADDED
            {
                float value = firstHediffOfDef.Severity * 0.027f;
                value = Mathf.Clamp(value, 0.0015f, 0.015f);
                firstHediffOfDef.Severity -= value;
            }
            else if (pawn.RaceProps.FleshType != FleshTypeDefOf.Insectoid && ambientTemperature < 0f && firstHediffOfDef.Severity > 0.37f)
            {
                float num = 0.025f * firstHediffOfDef.Severity;
                if (Rand.Value < num && pawn.RaceProps.body.AllPartsVulnerableToFrostbite.Where(x => !hediffSet.PartIsMissing(x)).TryRandomElementByWeight(x => x.def.frostbiteVulnerability, out BodyPartRecord result))
                {
                    int num2 = Mathf.CeilToInt(result.def.hitPoints * 0.5f);
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Frostbite, num2, 0f, -1f, null, result);
                    pawn.TakeDamage(dinfo);
                }
            }
            return false;
        }
    }
}
