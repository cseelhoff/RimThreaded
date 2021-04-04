using HarmonyLib;
using System.Collections.Generic;
using RimWorld.Planet;
using System;

namespace RimThreaded
{

    public class WorldObjectsHolder_Patch
	{
        public static AccessTools.FieldRef<WorldObjectsHolder, List<WorldObject>> worldObjects =
            AccessTools.FieldRefAccess<WorldObjectsHolder, List<WorldObject>>("worldObjects");
        public static bool WorldObjectsHolderTick(WorldObjectsHolder __instance)
        {
            RimThreaded.worldObjects = worldObjects(__instance);
            RimThreaded.worldObjectsTicks = worldObjects(__instance).Count;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WorldObjectsHolder);
            Type patched = typeof(WorldObjectsHolder_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldObjectsHolderTick");
        }
    }
}
