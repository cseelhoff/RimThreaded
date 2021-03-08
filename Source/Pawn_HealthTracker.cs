using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Pawn_HealthTracker_Patch
    {

        public static FieldRef<Pawn_HealthTracker, PawnHealthState> healthState =
            FieldRefAccess<Pawn_HealthTracker, PawnHealthState>("healthState");
        public static FieldRef<Pawn_HealthTracker, Pawn> pawn =
            FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");
        public static bool RemoveHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (__instance.hediffSet == null || __instance.hediffSet.hediffs == null)
                return false;

            //__instance.hediffSet.hediffs.Remove(hediff);
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
