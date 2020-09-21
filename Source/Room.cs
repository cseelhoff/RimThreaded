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
            IEnumerator<IntVec3> cachedOpenRoofState2 = __instance.Cells.GetEnumerator();
			int cachedOpenRoofCount2 = -1;

			if (cachedOpenRoofCount2 == -1 && cachedOpenRoofState2 == null)
			{
				cachedOpenRoofCount2 = 0;
				cachedOpenRoofState2 = __instance.Cells.GetEnumerator();
			}
			if (cachedOpenRoofCount2 < threshold && cachedOpenRoofState2 != null)
			{
				RoofGrid roofGrid = __instance.Map.roofGrid;
				if (null != roofGrid)
				{
					while (cachedOpenRoofCount2 < threshold && cachedOpenRoofState2.MoveNext())
					{
                        IntVec3 currentRoofState = cachedOpenRoofState2.Current;
						if (null != currentRoofState)
						{
							if (!roofGrid.Roofed(currentRoofState))
							{
								cachedOpenRoofCount2++;
							}
						}
					}
					if (cachedOpenRoofCount2 < threshold)
					{
						cachedOpenRoofState2 = null;
					}
				}
			}
			__result = cachedOpenRoofCount2;
			return false;
		}


	}
}
