using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{

    public class WorldPawns_Patch
    {
        public static AccessTools.FieldRef<WorldPawns, List<Pawn>> allPawnsAliveResult =
            AccessTools.FieldRefAccess<WorldPawns, List<Pawn>>("allPawnsAliveResult");
        public static AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsAlive =
            AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsAlive");
        public static AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsMothballed =
            AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsMothballed");
        public static bool get_AllPawnsAlive(WorldPawns __instance, ref List<Pawn> __result)
        {
            lock (allPawnsAliveResult(__instance))
            {
                allPawnsAliveResult(__instance).Clear();
                allPawnsAliveResult(__instance).AddRange(pawnsAlive(__instance));
                allPawnsAliveResult(__instance).AddRange(pawnsMothballed(__instance));
            }
            __result = allPawnsAliveResult(__instance);
            return false;
        }

    }
}
