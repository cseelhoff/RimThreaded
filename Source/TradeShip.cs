using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;

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
            int index = Interlocked.Increment(ref RimThreaded.totalTradeShipsCount) - 1;
            ThingOwner thingsOwner = things(__instance);
            RimThreaded.tradeShips[index].TradeShipThings = thingsOwner;
            int totalTradeShipTicks = Interlocked.Add(ref RimThreaded.totalTradeShipTicks, thingsOwner.Count);
            RimThreaded.tradeShips[index].TradeShipTicks = totalTradeShipTicks;
            return false;
        }

    }
}
