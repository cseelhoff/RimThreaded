using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimThreaded
{
    class HediffGiver_Heat_Patch
	{
		internal static void RunDestructivePatches()
		{
			Type original = typeof(HediffGiver_Heat);
			Type patched = typeof(HediffGiver_Heat_Patch);
			RimThreadedHarmony.Prefix(original, patched, "OnIntervalPassed");
		}

		public static bool OnIntervalPassed(HediffGiver_Heat __instance, Pawn pawn, Hediff cause)
		{
			float ambientTemperature = pawn.AmbientTemperature;
			float comfortableTemperatureMax = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
			float maxTemp = comfortableTemperatureMax + 10f;
			Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(__instance.hediff);
			if (ambientTemperature > maxTemp)
			{
				float x = ambientTemperature - maxTemp;
				float sevOffset = Mathf.Max(HediffGiver_Heat.TemperatureOverageAdjustmentCurve.Evaluate(x) * 6.45E-05f, 0.000375f);
				HealthUtility.AdjustSeverity(pawn, __instance.hediff, sevOffset);
			}
			else if (firstHediffOfDef != null && ambientTemperature < comfortableTemperatureMax)
			{
				float num = Mathf.Clamp(firstHediffOfDef.Severity * 0.027f, 0.0015f, 0.015f);
				firstHediffOfDef.Severity -= num;
			}
			if (pawn.Dead || !pawn.IsNestedHashIntervalTick(60, 420))			
				return false;
			
			float num4 = comfortableTemperatureMax + 150f;
			if (ambientTemperature <= num4)
				return false;

			float x1 = ambientTemperature - num4;
			int num2 = Mathf.Max(GenMath.RoundRandom(HediffGiver_Heat.TemperatureOverageAdjustmentCurve.Evaluate(x1) * 0.06f), 3);
			DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, num2);
			dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
			pawn.TakeDamage(dinfo);
			if (pawn.Faction == Faction.OfPlayer)
			{
				Find.TickManager.slower.SignalForceNormalSpeed();
				if (MessagesRepeatAvoider.MessageShowAllowed("PawnBeingBurned", 60f))
				{
					Messages.Message("MessagePawnBeingBurned".Translate(pawn.LabelShort, pawn), pawn, MessageTypeDefOf.ThreatSmall, true);
				}
			}
			pawn.GetLord()?.ReceiveMemo(HediffGiver_Heat.MemoPawnBurnedByAir);

			return false;
		}

    }
}
