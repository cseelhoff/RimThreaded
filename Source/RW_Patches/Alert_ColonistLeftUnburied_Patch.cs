using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class Alert_ColonistLeftUnburied_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Alert_ColonistLeftUnburied);
            Type patched = typeof(Alert_ColonistLeftUnburied_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(IsCorpseOfColonist));
        }

        public static bool IsCorpseOfColonist(ref bool __result, Corpse corpse)
        {
            if (corpse == null)
            {
                __result = false;
                return false;
            }
            Pawn InnerPawn = corpse.InnerPawn;
            if (InnerPawn == null)
            {
                __result = false;
                return false;
            }
            ThingDef def = InnerPawn.def;
            if (def == null)
            {
                __result = false;
                return false;
            }
            RaceProperties race = def.race;
            if (race == null)
            {
                __result = false;
                return false;
            }
            __result = InnerPawn.Faction == Faction.OfPlayer && race.Humanlike && !InnerPawn.IsQuestLodger() && !InnerPawn.IsSlave && !corpse.IsInAnyStorage();
            return false;
        }
    }
}
