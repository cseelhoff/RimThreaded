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
            Type original = typeof(TransportShipManager);
            //Type patched = typeof(TransportShipManager_Patch);
            RimThreadedHarmony.TranspileMethodLock(original, "RegisterShipObject");
            RimThreadedHarmony.TranspileMethodLock(original, "DeregisterShipObject");
        }
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
            }
        }
    }
}