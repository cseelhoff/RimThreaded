using HarmonyLib;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;

namespace RimThreaded
{

    public class WealthWatcher_Patch
	{
		[ThreadStatic] public static List<Thing> tmpThings;

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

		static Type original = typeof(WealthWatcher);
		static Type patched = typeof(WealthWatcher_Patch);

		public static void InitializeThreads()
        {
			tmpThings = new List<Thing>();
		}

		public static void RunNonDestructivePatches()
        {
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateWealthItems");
			RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateWealthFloors");
		}
		public static void RunDestructivePatches()
		{
			RimThreadedHarmony.Prefix(original, patched, "ResetStaticData");
		}

		public static bool ResetStaticData(WealthWatcher __instance)
		{
			int num = -1;
			List<TerrainDef> allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				num = Mathf.Max(num, allDefsListForReading[i].index);
			}

			float[] newCachedTerrainMarketValue = new float[num + 1];
			for (int j = 0; j < allDefsListForReading.Count; j++)
			{
				newCachedTerrainMarketValue[allDefsListForReading[j].index] = allDefsListForReading[j].GetStatValueAbstract(StatDefOf.MarketValue);
			}
			cachedTerrainMarketValue = newCachedTerrainMarketValue;
			return false;
		}


	}
}
