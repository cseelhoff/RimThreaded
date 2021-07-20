using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class District_Patch
    {

        internal static void RunDestructivePatches()
        {
#if RW13
            Type original = typeof(District);
            Type patched = typeof(District_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveRegion));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_RoofChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_RoomShapeOrContainedBedsChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(OpenRoofCountStopAt));
#endif
        }
#if RW13
        public static bool RemoveRegion(District __instance, Region r)
        {
            lock (__instance) //added
            {
                if (!__instance.regions.Contains(r))
                {
                    Log.Error("Tried to remove region from District but __instance region is not here. region=" + (object)r + ", district=" + (object)__instance);
                }
                else
                {
                    __instance.regions.Remove(r);
                    if (r.touchesMapEdge)
                        --__instance.numRegionsTouchingMapEdge;
                    if (__instance.regions.Count != 0)
                        return false;
                    __instance.Room = null;
                    __instance.cachedOpenRoofCount = -1;
                    __instance.cachedOpenRoofState = null;
                    __instance.Map.regionGrid.allDistricts.Remove(__instance);
                }
            }
            return false;
        }

        public static bool Notify_RoofChanged(District __instance)
        {
            lock (__instance) //added
            {
                __instance.cachedOpenRoofCount = -1;
                __instance.cachedOpenRoofState = null;
                __instance.Room.Notify_RoofChanged();
            }
            return false;
        }

        public static bool Notify_RoomShapeOrContainedBedsChanged(District __instance)
        {
            lock (__instance) //added
            {
                __instance.cachedCellCount = -1;
                __instance.cachedOpenRoofCount = -1;
                __instance.cachedOpenRoofState = null;
                __instance.lastChangeTick = Find.TickManager.TicksGame;
                FacilitiesUtility.NotifyFacilitiesAboutChangedLOSBlockers(__instance.regions);
            }
            return false;
        }

        public static bool OpenRoofCountStopAt(District __instance, ref int __result, int threshold)
        {
            lock (__instance) //added
            {
                if (__instance.cachedOpenRoofCount == -1 && __instance.cachedOpenRoofState == null)
                {
                    __instance.cachedOpenRoofCount = 0;
                    __instance.cachedOpenRoofState = __instance.Cells.GetEnumerator();
                }
                if (__instance.cachedOpenRoofCount < threshold && __instance.cachedOpenRoofState != null)
                {
                    RoofGrid roofGrid = __instance.Map.roofGrid;
                    while (__instance.cachedOpenRoofCount < threshold && __instance.cachedOpenRoofState.MoveNext())
                    {
                        if (!roofGrid.Roofed(__instance.cachedOpenRoofState.Current))
                            ++__instance.cachedOpenRoofCount;
                    }
                    if (__instance.cachedOpenRoofCount < threshold)
                        __instance.cachedOpenRoofState = null;
                }
                __result = __instance.cachedOpenRoofCount;
            }
            return false;
        }

#endif
    }
}
