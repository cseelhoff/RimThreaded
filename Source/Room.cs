using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Room_Patch
	{
		public static AccessTools.FieldRef<Room, int> cachedOpenRoofCount =
			AccessTools.FieldRefAccess<Room, int>("cachedOpenRoofCount");
		public static AccessTools.FieldRef<Room, IEnumerator<IntVec3>> cachedOpenRoofState =
			AccessTools.FieldRefAccess<Room, IEnumerator<IntVec3>>("cachedOpenRoofState");
		public static bool OpenRoofCountStopAt(Room __instance, ref int __result, int threshold)
		{
			if (cachedOpenRoofCount(__instance) == -1 && cachedOpenRoofState(__instance) == null)
			{
				cachedOpenRoofCount(__instance) = 0;
				cachedOpenRoofState(__instance) = __instance.Cells.GetEnumerator();
			}
			if (cachedOpenRoofCount(__instance) < threshold && cachedOpenRoofState(__instance) != null)
			{
				RoofGrid roofGrid = __instance.Map.roofGrid;
				if (null != roofGrid)
				{
					while (cachedOpenRoofCount(__instance) < threshold && cachedOpenRoofState(__instance).MoveNext())
					{
                        IntVec3 currentRoofState = cachedOpenRoofState(__instance).Current;
						if (null != currentRoofState)
						{
							if (!roofGrid.Roofed(currentRoofState))
							{
								cachedOpenRoofCount(__instance)++;
							}
						}
					}
					if (cachedOpenRoofCount(__instance) < threshold)
					{
						cachedOpenRoofState(__instance) = null;
					}
				}
			}
			__result = cachedOpenRoofCount(__instance);
			return false;
		}


	}
}
