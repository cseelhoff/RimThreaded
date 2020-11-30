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
        public static bool SetDead(Pawn_HealthTracker __instance)
        {
            if (__instance.Dead)
            {
                Log.Warning(string.Concat(pawn(__instance), " set dead while already dead."));
            }

            healthState(__instance) = PawnHealthState.Dead;
            return false;
        }

    }
}
