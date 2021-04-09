using System;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class Alert_ColonistLeftUnburied_Patch
    {
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(Alert_ColonistLeftUnburied);
            Type patched = typeof(Alert_ColonistLeftUnburied_Patch);
            RimThreadedHarmony.Prefix(original, patched, "IsCorpseOfColonist");
        }

        public static bool IsCorpseOfColonist(Corpse corpse)
        {
            return corpse?.InnerPawn?.Faction != null && corpse.InnerPawn.Faction == Faction.OfPlayer && corpse.InnerPawn.def.race.Humanlike && !corpse.InnerPawn.IsQuestLodger() && !corpse.IsInAnyStorage();
        }
    }
}