using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class Pawn_RecordsTracker_Patch
    {
        public static AccessTools.FieldRef<Pawn_RecordsTracker, int> battleExitTick =
            AccessTools.FieldRefAccess<Pawn_RecordsTracker, int>("battleExitTick");
        public static AccessTools.FieldRef<Pawn_RecordsTracker, Battle> battleActive =
            AccessTools.FieldRefAccess<Pawn_RecordsTracker, Battle>("battleActive");
        public static bool get_BattleActive(Pawn_RecordsTracker __instance, ref Battle __result)
        {
            if (battleExitTick(__instance) < Find.TickManager.TicksGame)
            {
                __result = null;
                return false;
            }

            if (battleActive == null)
            {
                __result = null;
                return false;
            }

            while (battleActive(__instance).AbsorbedBy != null)
            {
                battleActive(__instance) = battleActive(__instance).AbsorbedBy;
            }

            __result = battleActive(__instance);
            return false;
        }
        
    }
}
