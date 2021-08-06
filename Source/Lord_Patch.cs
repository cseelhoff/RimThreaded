using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimThreaded
{
    [StaticConstructorOnStartup]
    class Lord_Patch
    {
        public static Dictionary<Pawn, Lord> pawnsLord = new Dictionary<Pawn, Lord>();

        public static bool AddPawns(Lord __instance, IEnumerable<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                Lord_Patch.AddPawn(__instance, pawn);
            }
            return false;
        }
        public static bool AddPawn(Lord __instance, Pawn p)
        {
            if (__instance.ownedPawns.Contains(p))
            {
                Log.Error(string.Concat("Lord for ", __instance.faction.ToStringSafe(), " tried to add ", p, " whom it already controls."));
            }
            else if (p.GetLord() != null)
            {
                Log.Error(string.Concat("Tried to add pawn ", p, " to lord ", __instance, " but this pawn is already a member of lord ", p.GetLord(), ". Pawns can't be members of more than one lord at the same time."));
            }
            else
            {
                lock (__instance.ownedPawns)
                {
                    __instance.ownedPawns.Add(p);
                }
                lock (pawnsLord)
                {
                    pawnsLord[p] = __instance;
                }
                __instance.numPawnsEverGained++;
                __instance.Map.attackTargetsCache.UpdateTarget(p);
                __instance.curLordToil.UpdateAllDuties();
                __instance.curJob.Notify_PawnAdded(p);
            }
            return false;
        }

        public static bool RemovePawn(Lord __instance, Pawn p)
        {
            lock (__instance.ownedPawns)
            {
                __instance.ownedPawns.Remove(p);
            }
            lock (pawnsLord)
            {
                pawnsLord.Remove(p);
            }
            if (p.mindState != null)
            {
                p.mindState.duty = null;
            }

            __instance.Map.attackTargetsCache.UpdateTarget(p);
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Lord);
            Type patched = typeof(Lord_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AddPawn");
            RimThreadedHarmony.Prefix(original, patched, "AddPawns");
            RimThreadedHarmony.Prefix(original, patched, "RemovePawn");
        }
    }
}