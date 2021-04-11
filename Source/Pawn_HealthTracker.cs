using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Pawn_HealthTracker_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_HealthTracker);
            Type patched = typeof(Pawn_HealthTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RemoveHediff");
            //Type patched = typeof(Pawn_HealthTracker_Transpile);			
            //Transpile(original, patched, "RemoveHediff"); TODO re-add transpile
        }

        public static bool RemoveHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (__instance.hediffSet == null || __instance.hediffSet.hediffs == null)
                return false;

            lock (__instance.hediffSet)
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs);
                newHediffs.Remove(hediff);
                __instance.hediffSet.hediffs = newHediffs;
            }
            hediff.PostRemoved();
            __instance.Notify_HediffChanged(null);
            
            return false;
        }

    }
}
