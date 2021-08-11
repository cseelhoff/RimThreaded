using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimThreaded
{
    class TransportShipManager_Patch
    {
        internal static void RunNonDestructivePatches()
        {
#if RW13
            Type original = typeof(TransportShipManager);
            //Type patched = typeof(TransportShipManager_Patch);
            RimThreadedHarmony.TranspileMethodLock(original, "RegisterShipObject");
            RimThreadedHarmony.TranspileMethodLock(original, "DeregisterShipObject");
#endif
        }
#if RW13
        public static List<TransportShip> AllTransportShips;
        public static int AllTransportShipsCount;

        public static void ShipObjectsPrepare()
        {
            AllTransportShips = Current.Game.transportShipManager.ships;
            AllTransportShipsCount = AllTransportShips.Count;
        }
        public static void ShipObjectsTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref AllTransportShipsCount);
                if (index < 0) return;
                try
                {
                    AllTransportShips[index].Tick();
                }
                catch (Exception e)
                {
                    Log.Error("Exception ticking TransportShip: " + AllTransportShips[index].ToString() + ": " + e);
                }
                /*
                                int index = Interlocked.Decrement(ref allFactionsTicks);
                if (index < 0) return;
                Faction faction = allFactionsTickList[index];
                try
                {
                    faction.FactionTick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking faction: " + faction.ToStringSafe() + ": " + ex);
                }
                }*/
            }
        }
#endif
    }
}
