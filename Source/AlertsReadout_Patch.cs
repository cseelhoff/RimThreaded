using System;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class AlertsReadout_Patch
    {
        public static void RunNonDestructivesPatches()
        {
            Type original = typeof(AlertsReadout);
            Type patched = typeof(AlertsReadout_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AlertsReadoutUpdate", Type.EmptyTypes, false);
        }

        public static bool AlertsReadoutUpdate(AlertsReadout __instance)
        {
            //this will disable alert checks on ultrafast speed for an added speed boost
            return Find.TickManager.curTimeSpeed != TimeSpeed.Ultrafast || !RimThreadedMod.Settings.disablesomealerts;
        }

    }
}
