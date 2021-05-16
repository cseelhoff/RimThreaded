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
            int index = Interlocked.Increment(ref totalTradeShipsCount) - 1;
            ThingOwner thingsOwner = things(__instance);
            tradeShips[index].TradeShipThings = thingsOwner;
            Interlocked.Add(ref totalTradeShipTicks, thingsOwner.Count);
            tradeShips[index].TradeShipTicks = totalTradeShipTicks;
            return false;
        }

        public struct TradeShipStructure
        {
            public int TradeShipTicks;
            public ThingOwner TradeShipThings;
        }
        public static int totalTradeShipsCount = 0;
        public static int totalTradeShipTicks = 0;
        public static int totalTradeShipTicksCompleted = 0;
        public static TradeShipStructure[] tradeShips = new TradeShipStructure[99];

        public static void PassingShipListTick()
        {
            while (true)
            {
                int ticketIndex = Interlocked.Increment(ref totalTradeShipTicksCompleted) - 1;
                if (ticketIndex >= totalTradeShipTicks) return; // false;
                int totalTradeShipIndex = 0;
                while (ticketIndex < totalTradeShipTicks)
                {
                    int index = ticketIndex;
                    while (ticketIndex >= tradeShips[totalTradeShipIndex].TradeShipTicks)
                    {
                        totalTradeShipIndex++;
                    }
                    if (totalTradeShipIndex > 0)
                        index = ticketIndex - tradeShips[totalTradeShipIndex - 1].TradeShipTicks;
                    Pawn pawn = tradeShips[totalTradeShipIndex].TradeShipThings[index] as Pawn;
                    if (pawn != null)
                    {
                        try
                        {
                            pawn.Tick();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception ticking Pawn: " + pawn.ToStringSafe() + " " + ex);
                        }
                        if (pawn.Dead)
                        {
                            lock (tradeShips[totalTradeShipIndex].TradeShipThings)
                            {
                                tradeShips[totalTradeShipIndex].TradeShipThings.Remove(pawn);
                            }
                        }
                    }
                    ticketIndex = Interlocked.Increment(ref totalTradeShipTicksCompleted) - 1;
                }
            }
        }
    }
}
