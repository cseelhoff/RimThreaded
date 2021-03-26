using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    
    public class DynamicDrawManager_Patch
    {

        public static AccessTools.FieldRef<DynamicDrawManager, HashSet<Thing>> drawThings =
            AccessTools.FieldRefAccess<DynamicDrawManager, HashSet<Thing>>("drawThings");
        public static AccessTools.FieldRef<DynamicDrawManager, bool> drawingNow =
            AccessTools.FieldRefAccess<DynamicDrawManager, bool>("drawingNow");

        public static void RunDestructivePatches()
        {
            Type original = typeof(DynamicDrawManager);
            Type patched = typeof(DynamicDrawManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegisterDrawable");
            RimThreadedHarmony.Prefix(original, patched, "DeRegisterDrawable");
        }

        public static bool RegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType != DrawerType.None)
            {
                if (drawingNow(__instance))
                    Log.Warning("Cannot register drawable " + t + " while drawing is in progress. Things shouldn't be spawned in Draw methods.", false);
                lock (__instance)
                {
                    drawThings(__instance).Add(t);
                }
            }
            return false;
        }

        public static bool DeRegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType != DrawerType.None)
            {
                if (drawingNow(__instance))
                    Log.Warning("Cannot deregister drawable " + t + " while drawing is in progress. Things shouldn't be despawned in Draw methods.", false);
                lock (__instance)
                {
                    HashSet<Thing> newDrawThings = new HashSet<Thing>(drawThings(__instance));
                    newDrawThings.Remove(t);
                    drawThings(__instance) = newDrawThings;
                }
            }
            return false;
        }


    }

}
