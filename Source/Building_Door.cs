using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Building_Door_Patch
    {
		public static FieldRef<CompPowerTrader, bool> powerOnInt =
            FieldRefAccess<CompPowerTrader, bool>("powerOnInt");
		public static FieldRef<Building_Door, bool> openInt =
            FieldRefAccess<Building_Door, bool>("openInt");
		public static FieldRef<Building_Door, bool> holdOpenInt =
            FieldRefAccess<Building_Door, bool>("holdOpenInt");
		public static FieldRef<Building_Door, int> ticksUntilClose =
            FieldRefAccess<Building_Door, int>("ticksUntilClose");
		public static PropertyInfo canTryCloseAutomatically = DeclaredProperty(typeof(Building_Door), "CanTryCloseAutomatically");

		public static FieldRef<CompPowerTrader, bool> powerOnIntFieldRef = FieldRefAccess<CompPowerTrader, bool>("powerOnInt");

		public static bool get_DoorPowerOn(Building_Door __instance, ref bool __result)
		{
			CompPowerTrader pc = __instance.powerComp;
			bool poweron = false;
			if (pc != null)
			{
				try
				{
					//poweron = pc.PowerOn;
					poweron = powerOnIntFieldRef(pc);
				}
				catch (NullReferenceException) { }
			}
			__result = poweron;
			return false;
		}

		public static bool get_FreePassage(Building_Door __instance, out bool __result)
		{
            bool r = false;
            get_WillCloseSoon(__instance, ref r);
			__result = openInt(__instance) && (holdOpenInt(__instance) || !r);
			return false;
		}

		public static bool get_WillCloseSoon(Building_Door __instance, ref bool __result)
		{
			if (!__instance.Spawned)
			{
				__result = true;
				return false;
			}
			if (!openInt(__instance))
			{
				__result = true;
				return false;
			}
			if (holdOpenInt(__instance))
			{
				__result = false;
				return false;
			}
			if (ticksUntilClose(__instance) > 0 && ticksUntilClose(__instance) <= 111 && !__instance.BlockedOpenMomentary)
			{
				__result = true;
				return false;

			}
			if ((bool)canTryCloseAutomatically.GetValue(__instance, null) && !__instance.BlockedOpenMomentary)
			{
				__result = true;
				return false;
			}
			for (int i = 0; i < 5; i++)
			{
                IntVec3 position = (__instance).Position;
                IntVec3 c = position + GenAdj.CardinalDirectionsAndInside[i];
				Map map = (__instance).Map;
				if (c.InBounds(map))
				{
					List<Thing> thingList = c.GetThingList(map);
					for (int j = 0; j < thingList.Count; j++)
					{
                        if (thingList[j] is Pawn pawn)
                        {
                            Pawn_PathFollower pather1 = pawn.pather;
                            if (null != pather1)
                            {
                                if (pawn != null && !pawn.HostileTo(__instance) && !pawn.Downed && (pawn.Position == position || (pather1.Moving && pather1.nextCell == position)))
                                {
                                    return true;
                                }
                            }
                        }
                    }
				}
			}
			return false;
		}
		

	}
}
