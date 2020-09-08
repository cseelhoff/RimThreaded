using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimThreaded
{
    class HediffGiver_Hypothermia_Patch
	{
		public static bool OnIntervalPassed(HediffGiver_Hypothermia __instance, Pawn pawn, Hediff cause)
		{
			float ambientTemperature = pawn.AmbientTemperature;
			float comfortableTemperatureMin = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
			float minTemp = comfortableTemperatureMin - 10f;
			HediffSet hediffSet = pawn.health.hediffSet;
			HediffDef hediffDef = pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid ? __instance.hediffInsectoid : __instance.hediff;
			Hediff firstHediffOfDef = hediffSet.GetFirstHediffOfDef(hediffDef, false);
			if (ambientTemperature < minTemp)
			{
				float sevOffset = Mathf.Max(Mathf.Abs(ambientTemperature - minTemp) * 6.45E-05f, 0.00075f);
				HealthUtility.AdjustSeverity(pawn, hediffDef, sevOffset);
				if (pawn.Dead)
					return false;
			}
			if (firstHediffOfDef == null)
				return false;
			if (ambientTemperature > comfortableTemperatureMin)
			{
				float num = Mathf.Clamp(firstHediffOfDef.Severity * 0.027f, 0.0015f, 0.015f);
				firstHediffOfDef.Severity -= num;
			}
			else
			{
				BodyPartRecord result;
				if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid || (double)ambientTemperature >= 0.0 || ((double)firstHediffOfDef.Severity <= 0.370000004768372 || (double)Rand.Value >= (double)(0.025f * firstHediffOfDef.Severity)) || !pawn.RaceProps.body.AllPartsVulnerableToFrostbite.Where<BodyPartRecord>((Func<BodyPartRecord, bool>)(x => !hediffSet.PartIsMissing(x))).TryRandomElementByWeight<BodyPartRecord>((Func<BodyPartRecord, float>)(x => x.def.frostbiteVulnerability), out result))
					return false;
				int num = Mathf.CeilToInt(result.def.hitPoints * 0.5f);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Frostbite, (float)num, 0.0f, -1f, (Verse.Thing)null, result, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Verse.Thing)null);
				pawn.TakeDamage(dinfo);
			}
			return false;
		}
	}
}
