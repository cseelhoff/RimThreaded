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

    public class TradeShip_Patch
	{
		public static AccessTools.FieldRef<TradeShip, ThingOwner> things =
			AccessTools.FieldRefAccess<TradeShip, ThingOwner>("things");
        public static bool PassingShipTick(TradeShip __instance)
        {
            --__instance.ticksUntilDeparture;
            if (__instance.Departed)
                __instance.Depart();
            RimThreaded.TradeShipThings = things(__instance);
            RimThreaded.TradeShipTicks = things(__instance).Count;
            return false;
        }

    }
}
