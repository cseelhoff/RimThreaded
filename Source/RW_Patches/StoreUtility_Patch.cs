using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class StoreUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(StoreUtility);
            Type patched = typeof(StoreUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CurrentHaulDestinationOf), new Type[] { typeof(Thing) });
        }
        public static bool CurrentHaulDestinationOf(ref IHaulDestination __result, Thing t)
        {
            __result = null;
            if (t == null)
                return false;
            if (!t.Spawned)
            {
                __result = t.ParentHolder as IHaulDestination;
                return false;
            }
            Map map = t.Map;
            if (map == null)
                return false;
            HaulDestinationManager haulDestinationManager = map.haulDestinationManager;
            if (haulDestinationManager == null)
                return false;
            __result = haulDestinationManager.SlotGroupParentAt(t.Position);
            return false;
        }
    }
}
