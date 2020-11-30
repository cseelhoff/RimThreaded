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

    public class FastPriorityQueueKeyValuePairIntFloat_Patch
	{
		public static AccessTools.FieldRef<FastPriorityQueue<KeyValuePair<int, float>>, List<KeyValuePair<int, float>>> innerList =
			AccessTools.FieldRefAccess<FastPriorityQueue<KeyValuePair<int, float>>, List<KeyValuePair<int, float>>>("innerList");
		public static AccessTools.FieldRef<FastPriorityQueue<KeyValuePair<int, float>>, IComparer<KeyValuePair<int, float>>> comparer =
			AccessTools.FieldRefAccess<FastPriorityQueue<KeyValuePair<int, float>>, IComparer<KeyValuePair<int, float>>>("comparer");
		public static bool Push(FastPriorityQueue<KeyValuePair<int, float>> __instance, KeyValuePair<int, float> item)
		{
			lock (innerList(__instance))
			{
				int num = innerList(__instance).Count;
				innerList(__instance).Add(item);
				while (num != 0)
				{
					int num2 = (num - 1) / 2;
					if (CompareElements(__instance, num, num2) >= 0)
					{
						break;
					}
					SwapElements(__instance, num, num2);
					num = num2;
				}
			}
			return false;
		}
		public static bool Pop(FastPriorityQueue<KeyValuePair<int, float>> __instance)
		{
            List<KeyValuePair<int, float>> il = innerList(__instance);
			lock (il)
			{
				KeyValuePair<int, float> result = il[0];
				int key = result.Key;
				float value = result.Value;
				int num = 0;
				int count = innerList(__instance).Count;
				il[0] = il[count - 1];
				il.RemoveAt(count - 1);
				count = il.Count;
				for (; ; )
				{
					int num2 = num;
					int num3 = 2 * num + 1;
					int num4 = num3 + 1;
					if (num3 < count && CompareElements(__instance, num, num3) > 0)
					{
						num = num3;
					}
					if (num4 < count && CompareElements(__instance, num, num4) > 0)
					{
						num = num4;
					}
					if (num == num2)
					{
						break;
					}
					SwapElements(__instance, num, num2);
				}
			}
			return false;
		}
		public static bool Clear(FastPriorityQueue<KeyValuePair<int, float>> __instance)
		{
			lock (innerList(__instance))
			{
				innerList(__instance).Clear();
			}
			return false;
		}

		private static void SwapElements(FastPriorityQueue<KeyValuePair<int, float>> __instance, int i, int j)
		{
			lock (innerList(__instance))
			{
				KeyValuePair<int, float> value = innerList(__instance)[i];
				innerList(__instance)[i] = innerList(__instance)[j];
				innerList(__instance)[j] = value;
			}
		}
		private static int CompareElements(FastPriorityQueue<KeyValuePair<int, float>> __instance, int i, int j)
		{
			lock (innerList(__instance))
			{
				return comparer(__instance).Compare(innerList(__instance)[i], innerList(__instance)[j]);
			}
		}

	}
}
