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
        public static AccessTools.FieldRef<WorldObjectsHolder, List<WorldObject>> worldObjects =
            AccessTools.FieldRefAccess<WorldObjectsHolder, List<WorldObject>>("worldObjects");

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WorldObjectsHolder);
            Type patched = typeof(WorldObjectsHolder_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldObjectsHolderTick");
        }

        public static bool WorldObjectsHolderTick(WorldObjectsHolder __instance)
        {
            RimThreaded.worldObjects = worldObjects(__instance);
            RimThreaded.worldObjectsTicks = worldObjects(__instance).Count;
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

        public static bool WorldObjectsListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref worldObjectsTicks);
                if (index < -1) return false;
                if (index == -1) return true; //causes method to return "true" only once upon completion
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
