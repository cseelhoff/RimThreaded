using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class OverlayDrawer_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(OverlayDrawer);
            Type patched = typeof(OverlayDrawer_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetOverlaysHandle));
            RimThreadedHarmony.Prefix(original, patched, nameof(DisposeHandle));
        }

        public static bool GetOverlaysHandle(OverlayDrawer __instance, ref ThingOverlaysHandle __result, Thing thing)
        {
            if (!thing.Spawned)
            {
                __result = null;
                return false;
            }
            ThingOverlaysHandle thingOverlaysHandle;
            lock (__instance) //added
            {
                if (!__instance.overlayHandles.TryGetValue(thing, out thingOverlaysHandle))
                {
                    thingOverlaysHandle = new ThingOverlaysHandle(__instance, thing);
                    __instance.overlayHandles.Add(thing, thingOverlaysHandle);
                }
            }
            __result = thingOverlaysHandle;
            return false;
        }
        public static bool DisposeHandle(OverlayDrawer __instance, Thing thing)
        {
            ThingOverlaysHandle thingOverlaysHandle;
            lock (__instance) //added
            {
                if (__instance.overlayHandles.TryGetValue(thing, out thingOverlaysHandle))
                {
                    thingOverlaysHandle.Dispose();
                    __instance.overlayHandles.Remove(thing);
                }
            }
            return false;
        }
    }
}
