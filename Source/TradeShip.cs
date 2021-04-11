using HarmonyLib;
using System;
using RimWorld;
using Verse;
using System.Threading;

namespace RimThreaded
{

    public class TradeShip_Patch
    {
        public static AccessTools.FieldRef<TradeShip, ThingOwner> things =
            AccessTools.FieldRefAccess<TradeShip, ThingOwner>("things");

        internal static void RunDestructivePatches()
        {
            Type original = typeof(TradeShip);
            Type patched = typeof(TradeShip_Patch);
            RimThreadedHarmony.Prefix(original, patched, "PassingShipTick");
        }

        public static bool PassingShipTick(TradeShip __instance)
        {
            --__instance.ticksUntilDeparture;
            if (__instance.Departed)
                __instance.Depart();
            int index = Interlocked.Increment(ref RimThreaded.totalTradeShipsCount) - 1;
            ThingOwner thingsOwner = things(__instance);
            RimThreaded.tradeShips[index].TradeShipThings = thingsOwner;
            Interlocked.Add(ref RimThreaded.totalTradeShipTicks, thingsOwner.Count);
            RimThreaded.tradeShips[index].TradeShipTicks = RimThreaded.totalTradeShipTicks;
            return false;
        }

    }
}
