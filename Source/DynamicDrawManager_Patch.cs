using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace RimThreaded
{
    
    public class DynamicDrawManager_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(DynamicDrawManager);
            Type patched = typeof(DynamicDrawManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegisterDrawable");
            RimThreadedHarmony.Prefix(original, patched, "DeRegisterDrawable");
        }

        public static bool RegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType == DrawerType.None) return false;
            if (__instance.drawingNow)
                Log.Warning("Cannot register drawable " + t + " while drawing is in progress. Things shouldn't be spawned in Draw methods.", false);
            lock (__instance)
            {
                __instance.drawThings.Add(t);
            }
            return false;
        }

        public static bool DeRegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType == DrawerType.None) return false;
            if (__instance.drawingNow)
                Log.Warning("Cannot deregister drawable " + t + " while drawing is in progress. Things shouldn't be despawned in Draw methods.", false);
            lock (__instance)
            {
                HashSet<Thing> newDrawThings = new HashSet<Thing>(__instance.drawThings);
                newDrawThings.Remove(t);
                __instance.drawThings = newDrawThings;
            }
            return false;
        }


    }

}
