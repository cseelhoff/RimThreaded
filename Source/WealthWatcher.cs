using HarmonyLib;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimThreaded
{

    public class WealthWatcher_Patch
	{
		[ThreadStatic]
		static List<Thing> tmpThings;

		public static AccessTools.FieldRef<WealthWatcher, float> wealthItems =
			AccessTools.FieldRefAccess<WealthWatcher, float>("wealthItems");
		public static AccessTools.FieldRef<WealthWatcher, float> wealthBuildings =
			AccessTools.FieldRefAccess<WealthWatcher, float>("wealthBuildings");
		public static AccessTools.FieldRef<WealthWatcher, float> wealthPawns =
			AccessTools.FieldRefAccess<WealthWatcher, float>("wealthPawns");
		public static AccessTools.FieldRef<WealthWatcher, float> wealthFloorsOnly =
			AccessTools.FieldRefAccess<WealthWatcher, float>("wealthFloorsOnly");
		public static AccessTools.FieldRef<WealthWatcher, int> totalHealth =
			AccessTools.FieldRefAccess<WealthWatcher, int>("totalHealth");
		public static AccessTools.FieldRef<WealthWatcher, Map> map =
			AccessTools.FieldRefAccess<WealthWatcher, Map>("map");
		public static AccessTools.FieldRef<WealthWatcher, float> lastCountTick =
			AccessTools.FieldRefAccess<WealthWatcher, float>("lastCountTick");

		public static float[] cachedTerrainMarketValue =
			AccessTools.StaticFieldRefAccess<float[]>(typeof(WealthWatcher), "cachedTerrainMarketValue");

		private static float CalculateWealthItems(WealthWatcher __instance)
		{
			//this.tmpThings.Clear();
			if (tmpThings == null)
			{
				tmpThings = new List<Thing>();
			} else
            {
				tmpThings.Clear();
			}
			ThingOwnerUtility.GetAllThingsRecursively(map(__instance), ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), tmpThings, false, delegate (IThingHolder x)
			{
				if (x is PassingShip || x is MapComponent)
				{
					return false;
				}
				Pawn pawn = x as Pawn;
				return (pawn == null || pawn.Faction == Faction.OfPlayer) && (pawn == null || !pawn.IsQuestLodger());
			}, true);
			float num = 0f;
			for (int i = 0; i < tmpThings.Count; i++)
			{
				if (tmpThings[i].SpawnedOrAnyParentSpawned && !tmpThings[i].PositionHeld.Fogged(map(__instance)))
				{
					num += tmpThings[i].MarketValue * tmpThings[i].stackCount;
				}
			}
			//this.tmpThings.Clear();
			return num;
		}
		private static float CalculateWealthFloors(WealthWatcher __instance)
		{
			TerrainDef[] topGrid = map(__instance).terrainGrid.topGrid;
			bool[] fogGrid = map(__instance).fogGrid.fogGrid;
			IntVec3 size = map(__instance).Size;
			float num = 0f;
			int i = 0;
			int num2 = size.x * size.z;
			while (i < num2)
			{
				if (!fogGrid[i])
				{
					num += cachedTerrainMarketValue[topGrid[i].index];
				}
				i++;
			}
			return num;
		}
		public static bool ForceRecount(WealthWatcher __instance, bool allowDuringInit = false)
		{
			if (!allowDuringInit && Current.ProgramState != ProgramState.Playing)
			{
				Log.Error("WealthWatcher recount in game mode " + Current.ProgramState, false);
				return false;
			}
			wealthItems(__instance) = CalculateWealthItems(__instance);
			wealthBuildings(__instance) = 0f;
			wealthPawns(__instance) = 0f;
			wealthFloorsOnly(__instance) = 0f;
			totalHealth(__instance) = 0;
			List<Thing> list = map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing = list[i];
				if (thing.Faction == Faction.OfPlayer)
				{
					wealthBuildings(__instance) += thing.GetStatValue(StatDefOf.MarketValueIgnoreHp, true);
					totalHealth(__instance) += thing.HitPoints;
				}
			}
			wealthFloorsOnly(__instance) = CalculateWealthFloors(__instance);
			wealthBuildings(__instance) += wealthFloorsOnly(__instance);
			Pawn pawn;
			List<Pawn> pawnList = map(__instance).mapPawns.PawnsInFaction(Faction.OfPlayer);
			for (int index = 0; index < pawnList.Count; index++)
			{
				try
                {
					pawn = pawnList[index];
				} catch (ArgumentOutOfRangeException) { break; }
				if (!pawn.IsQuestLodger())
				{
					wealthPawns(__instance) += pawn.MarketValue;
					if (pawn.IsFreeColonist)
					{
						totalHealth(__instance) += Mathf.RoundToInt(pawn.health.summaryHealth.SummaryHealthPercent * 100f);
					}
				}
			}
			lastCountTick(__instance) = (float)Find.TickManager.TicksGame;
			return false;
		}

	}
}
