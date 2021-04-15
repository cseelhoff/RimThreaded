using RimWorld;
using System;

namespace RimThreaded
{
    class SlotGroup_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(SlotGroup);
            Type patched = typeof(HaulingCache);
            RimThreadedHarmony.Postfix(original, patched, "Notify_AddedCell", "NewStockpileCreatedOrMadeUnfull");
        }
    }
}
