using HarmonyLib;
using System.Collections.Generic;
using RimWorld.Planet;
using System;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class WorldObjectsHolder_Patch
	{
        //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
        internal static void RunDestructivePatches()
        {
            Type original = typeof(WorldObjectsHolder);
            Type patched = typeof(WorldObjectsHolder_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldObjectsHolderTick");
        }

        public static bool WorldObjectsHolderTick(WorldObjectsHolder __instance)
        {
            worldObjectsTickList = __instance.worldObjects;
            worldObjectsTicks = __instance.worldObjects.Count;
            return false;
        }

        public static List<WorldObject> worldObjectsTickList;
        public static int worldObjectsTicks;

        public static void WorldObjectsPrepare()
        {
            try
            {
                World world = Find.World;
                world.worldObjects.WorldObjectsHolderTick();
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void WorldObjectsListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref worldObjectsTicks);
                if (index < 0) return;
                WorldObject worldObject = worldObjectsTickList[index];
                try
                {
                    worldObject.Tick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking world object: " + worldObject.ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}
