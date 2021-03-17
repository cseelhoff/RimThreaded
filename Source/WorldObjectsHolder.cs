using HarmonyLib;
using System.Collections.Generic;
using RimWorld.Planet;

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

    }
}
