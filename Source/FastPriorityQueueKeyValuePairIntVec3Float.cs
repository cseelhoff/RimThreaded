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

    public class FastPriorityQueueKeyValuePairIntVec3Float_Patch
	{
		public static AccessTools.FieldRef<FastPriorityQueue<KeyValuePair<IntVec3, float>>, List<KeyValuePair<IntVec3, float>>> innerList =
			AccessTools.FieldRefAccess<FastPriorityQueue<KeyValuePair<IntVec3, float>>, List<KeyValuePair<IntVec3, float>>>("innerList");
		public static AccessTools.FieldRef<FastPriorityQueue<KeyValuePair<IntVec3, float>>, IComparer<KeyValuePair<IntVec3, float>>> comparer =
			AccessTools.FieldRefAccess<FastPriorityQueue<KeyValuePair<IntVec3, float>>, IComparer<KeyValuePair<IntVec3, float>>>("comparer");
		public static bool Push(FastPriorityQueue<KeyValuePair<IntVec3, float>> __instance, KeyValuePair<IntVec3, float> item)
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
		public static bool Pop(FastPriorityQueue<KeyValuePair<IntVec3, float>> __instance)
		{
			lock (innerList(__instance))
			{
				KeyValuePair<IntVec3, float> result = innerList(__instance)[0];
				int num = 0;
				int count = innerList(__instance).Count;
				innerList(__instance)[0] = innerList(__instance)[count - 1];
				innerList(__instance).RemoveAt(count - 1);
				count = innerList(__instance).Count;
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
		public static bool Clear(FastPriorityQueue<KeyValuePair<IntVec3, float>> __instance)
		{
			lock (innerList(__instance))
			{
				innerList(__instance).Clear();
			}
			return false;
		}

		private static void SwapElements(FastPriorityQueue<KeyValuePair<IntVec3, float>> __instance, int i, int j)
		{
			lock (innerList(__instance))
			{
				KeyValuePair<IntVec3, float> value = innerList(__instance)[i];
				innerList(__instance)[i] = innerList(__instance)[j];
				innerList(__instance)[j] = value;
			}
		}
		private static int CompareElements(FastPriorityQueue<KeyValuePair<IntVec3, float>> __instance, int i, int j)
		{
			lock (innerList(__instance))
			{
				return comparer(__instance).Compare(innerList(__instance)[i], innerList(__instance)[j]);
			}
		}
	}
}
