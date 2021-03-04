using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using UnityEngine;

namespace RimThreaded
{

	public class GenTemperature_Patch
    {

		public static Dictionary<int, float> SeasonalShiftAmplitudeCache = new Dictionary<int, float>();
		public static Dictionary<int, float> tileTemperature = new Dictionary<int, float>();
		public static Dictionary<int, float> absTickOffset = new Dictionary<int, float>();
		public static Dictionary<int, Dictionary<int, float>> tileAbsTickTemperature = new Dictionary<int, Dictionary<int, float>>();

		public static RoomGroup[] beqRoomGroups = AccessTools.StaticFieldRefAccess<RoomGroup[]>(typeof(GenTemperature), "beqRoomGroups");
        public static MethodInfo InsulateUtility = AccessTools.Method("InsulateUtility:GetInsulationRate");
        private static FastInvokeHandler GetInsulationRate = null;

		public static bool SeasonalShiftAmplitudeAt(ref float __result, int tile)
		{
			if (!SeasonalShiftAmplitudeCache.TryGetValue(tile, out __result))
			{
				__result = (double)Find.WorldGrid.LongLatOf(tile).y >= 0.0 ?
					TemperatureTuning.SeasonalTempVariationCurve.Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile)) :
					-TemperatureTuning.SeasonalTempVariationCurve.Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile));
				SeasonalShiftAmplitudeCache[tile] = __result;
			}
			return false;
		}
		public static bool GetTemperatureFromSeasonAtTile(ref float __result, int absTick, int tile)
		{
			if (absTick == 0)
			{
				absTick = 1;
			}
			
			if (!tileAbsTickTemperature.TryGetValue(tile, out Dictionary<int, float> absTickTemperature))
			{
				absTickTemperature = new Dictionary<int, float>();
				tileAbsTickTemperature[tile] = absTickTemperature;
			}
			if (!absTickTemperature.TryGetValue(absTick, out float temperature))
			{
				if (!tileTemperature.TryGetValue(tile, out float temperatureFromTile))
				{
					temperatureFromTile = Find.WorldGrid[tile].temperature;
					tileTemperature[tile] = temperatureFromTile;
				}
				temperature = temperatureFromTile + GenTemperature.OffsetFromSeasonCycle(absTick, tile);
				lock (absTickTemperature)
				{
					absTickTemperature.SetOrAdd(absTick, temperature);
				}
			}
			__result = temperature;
			return false;
		}


		public static bool PushHeat(ref bool __result, IntVec3 c, Map map, float energy)
		{
			if (map == null)
			{
				Log.Error("Added heat to null map.", false);
				__result = false;
				return false;
			}
			RoomGroup roomGroup1 = c.GetRoomGroup(map);
			if (roomGroup1 != null)
			{
				__result = roomGroup1.PushHeat(energy);
				return false;
			}
			List<RoomGroup> neighRoomGroups = new List<RoomGroup>();
			for (int index = 0; index < 8; ++index)
			{
				IntVec3 intVec3 = c + GenAdj.AdjacentCells[index];
				if (intVec3.InBounds(map))
				{
					RoomGroup roomGroup2 = intVec3.GetRoomGroup(map);
					if (roomGroup2 != null)
						neighRoomGroups.Add(roomGroup2);
				}
			}
			float energy1 = energy / (float)neighRoomGroups.Count;
			for (int index = 0; index < neighRoomGroups.Count; ++index)
				neighRoomGroups[index].PushHeat(energy1);
			int num = neighRoomGroups.Count > 0 ? 1 : 0;
			neighRoomGroups.Clear();
			__result = num != 0;
			return false;
		}

		public static bool EqualizeTemperaturesThroughBuilding(Building b, float rate, bool twoWay)
		{
			int num = 0;
			float num2 = 0f;
            if (InsulateUtility != null)
            {
                if (GetInsulationRate == null) GetInsulationRate = MethodInvoker.GetHandler(InsulateUtility);
				rate = (float)GetInsulationRate(null, new object[] { b, rate});
			}
			if (twoWay)
			{
				for (int i = 0; i < 2; i++)
				{
					IntVec3 intVec = (i == 0) ? (b.Position + b.Rotation.FacingCell) : (b.Position - b.Rotation.FacingCell);
					if (intVec.InBounds(b.Map))
					{
						RoomGroup roomGroup = intVec.GetRoomGroup(b.Map);
						if (roomGroup != null)
						{
							num2 += roomGroup.Temperature;
							beqRoomGroups[num] = roomGroup;
							num++;
						}
					}
				}
			}
			else
			{
				for (int j = 0; j < 4; j++)
				{
					IntVec3 intVec2 = b.Position + GenAdj.CardinalDirections[j];
					if (intVec2.InBounds(b.Map))
					{
						RoomGroup roomGroup2 = intVec2.GetRoomGroup(b.Map);
						if (roomGroup2 != null)
						{
							num2 += roomGroup2.Temperature;
							beqRoomGroups[num] = roomGroup2;
							num++;
						}
					}
				}
			}
			if (num == 0)
			{
				return false;
			}
			float num3 = num2 / (float)num;
			RoomGroup roomGroup3 = b.GetRoomGroup();
			if (roomGroup3 != null)
			{
				roomGroup3.Temperature = num3;
			}
			if (num == 1)
			{
				return false;
			}
			float num4 = 1f;
			for (int k = 0; k < num; k++)
			{
                RoomGroup roomGroupK = beqRoomGroups[k];
                if (null != roomGroupK)
				{
					if (!roomGroupK.UsesOutdoorTemperature)
					{
						float temperature = roomGroupK.Temperature;
						float num5 = (num3 - temperature) * rate;
						float num6 = num5 / (float)roomGroupK.CellCount;
						float num7 = roomGroupK.Temperature + num6;
						if (num5 > 0f && num7 > num3)
						{
							num7 = num3;
						}
						else if (num5 < 0f && num7 < num3)
						{
							num7 = num3;
						}
						float num8 = Mathf.Abs((num7 - temperature) * (float)roomGroupK.CellCount / num5);
						if (num8 < num4)
						{
							num4 = num8;
						}
					}
				}
			}
			for (int l = 0; l < num; l++)
			{
				RoomGroup roomGroupL = beqRoomGroups[l];
				if (null!= roomGroupL && !roomGroupL.UsesOutdoorTemperature)
				{
					float temperature2 = roomGroupL.Temperature;
					float num9 = (num3 - temperature2) * rate * num4 / (float)roomGroupL.CellCount;
					roomGroupL.Temperature += num9;
					beqRoomGroups[l] = roomGroupL;
				}
			}
			for (int m = 0; m < beqRoomGroups.Length; m++)
			{
				beqRoomGroups[m] = null;
			}
			return false;
		}
	}
}
