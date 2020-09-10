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

    public class MapPawns_Patch
	{
        public static AccessTools.FieldRef<MapPawns, List<Pawn>> pawnsSpawned =
            AccessTools.FieldRefAccess<MapPawns, List<Pawn>>("pawnsSpawned");

        public static bool get_AllPawns(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> allPawnsUnspawned = __instance.AllPawnsUnspawned;
            if (allPawnsUnspawned.Count == 0)
            {
                __result = pawnsSpawned(__instance);
                return false;
            }
            List<Pawn> allPawnsResult = new List<Pawn>();
            allPawnsResult.AddRange((IEnumerable<Pawn>)pawnsSpawned(__instance));
            allPawnsResult.AddRange((IEnumerable<Pawn>)allPawnsUnspawned);
            __result = allPawnsResult;
            return false;
        }

    }
}
