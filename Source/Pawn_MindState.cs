using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace RimThreaded
{
	public class Pawn_MindState_Patch
	{
		public static bool MindStateTick(Pawn_MindState __instance)
		{
			if (__instance.wantsToTradeWithColony)
				TradeUtility.CheckInteractWithTradersTeachOpportunity(__instance.pawn);
			if (__instance.meleeThreat != null && !__instance.MeleeThreatStillThreat)
				__instance.meleeThreat = null;
			__instance.mentalStateHandler.MentalStateHandlerTick();
			__instance.mentalBreaker.MentalBreakerTick();
			__instance.inspirationHandler.InspirationHandlerTick();
			if (!__instance.pawn.GetPosture().Laying())
				__instance.applyBedThoughtsTick = 0;
			if (__instance.pawn.IsHashIntervalTick(100))
				__instance.anyCloseHostilesRecently = __instance.pawn.Spawned && PawnUtility.EnemiesAreNearby(__instance.pawn, __instance.anyCloseHostilesRecently ? 24 : 18, true);
			if (__instance.WillJoinColonyIfRescued && __instance.AnythingPreventsJoiningColonyIfRescued)
				__instance.WillJoinColonyIfRescued = false;
			if (__instance.pawn.Spawned && __instance.pawn.IsWildMan() && (!__instance.WildManEverReachedOutside && __instance.pawn.GetRoom(RegionType.Set_Passable) != null) && __instance.pawn.GetRoom(RegionType.Set_Passable).TouchesMapEdge)
				__instance.WildManEverReachedOutside = true;
			if (Find.TickManager.TicksGame % 123 == 0 && __instance.pawn.Spawned && (__instance.pawn.RaceProps.IsFlesh && __instance.pawn.needs.mood != null))
			{
				TerrainDef terrain = __instance.pawn.Position.GetTerrain(__instance.pawn.Map);
				if (terrain.traversedThought != null)
					__instance.pawn.needs.mood.thoughts.memories.TryGainMemoryFast(terrain.traversedThought);
				WeatherDef curWeatherLerped = __instance.pawn.Map.weatherManager.CurWeatherLerped;
				if (curWeatherLerped.exposedThought != null && !__instance.pawn.Position.Roofed(__instance.pawn.Map))
					__instance.pawn.needs.mood.thoughts.memories.TryGainMemoryFast(curWeatherLerped.exposedThought);
			}
			//dirty hack for easy speedup - i'm sure this breaks things like pawn conversation interval.
			//if (GenLocalDate.DayTick((Thing)__instance.pawn) != 0)
			//	return;
			__instance.interactionsToday = 0;
			return false;
		}
	}
}