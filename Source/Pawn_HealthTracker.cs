using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class Pawn_HealthTracker_Patch
    {

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
