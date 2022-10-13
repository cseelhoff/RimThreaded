using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class PawnComponentsUtility_Patch
    {
        private static readonly Type Original = typeof(PawnComponentsUtility);
        private static readonly Type Patched = typeof(PawnComponentsUtility_Patch);
        internal static void RunDestructivePatches()
        {
            //for Children Mod
            RimThreadedHarmony.Prefix(Original, Patched, "CreateInitialComponents");
        }

        public static bool CreateInitialComponents(Pawn pawn)
        {
            if (pawn.ageTracker == null)
                pawn.ageTracker = new Pawn_AgeTracker(pawn);
            if (pawn.health == null)
                pawn.health = new Pawn_HealthTracker(pawn);
            if (pawn.records == null)
                pawn.records = new Pawn_RecordsTracker(pawn);
            if (pawn.inventory == null)
                pawn.inventory = new Pawn_InventoryTracker(pawn);
            if (pawn.meleeVerbs == null)
                pawn.meleeVerbs = new Pawn_MeleeVerbs(pawn);
            if (pawn.verbTracker == null)
                pawn.verbTracker = new VerbTracker((IVerbOwner)pawn);
            if (pawn.carryTracker == null)
                pawn.carryTracker = new Pawn_CarryTracker(pawn);

            //BEGIN INSERT for Children Mod
            if (pawn.mindState == null)
                pawn.mindState = new Pawn_MindState(pawn);
            //END INSERT for Children Mod

            if (pawn.needs == null)
                pawn.needs = new Pawn_NeedsTracker(pawn);
            //if (pawn.mindState == null)
                //pawn.mindState = new Pawn_MindState(pawn);
            if (pawn.RaceProps.ToolUser)
            {
                if (pawn.equipment == null)
                    pawn.equipment = new Pawn_EquipmentTracker(pawn);
                if (pawn.apparel == null)
                    pawn.apparel = new Pawn_ApparelTracker(pawn);
            }
            if (pawn.RaceProps.Humanlike)
            {
                if (pawn.ownership == null)
                    pawn.ownership = new Pawn_Ownership(pawn);
                if (pawn.skills == null)
                    pawn.skills = new Pawn_SkillTracker(pawn);
                if (pawn.story == null)
                    pawn.story = new Pawn_StoryTracker(pawn);
                if (pawn.guest == null)
                    pawn.guest = new Pawn_GuestTracker(pawn);
                if (pawn.guilt == null)
                    pawn.guilt = new Pawn_GuiltTracker(pawn);
                if (pawn.workSettings == null)
                    pawn.workSettings = new Pawn_WorkSettings(pawn);
                if (pawn.royalty == null)
                    pawn.royalty = new Pawn_RoyaltyTracker(pawn);
                if (pawn.abilities == null)
                    pawn.abilities = new Pawn_AbilityTracker(pawn);
            }
            if (pawn.RaceProps.IsFlesh)
            {
                if (pawn.relations == null)
                    pawn.relations = new Pawn_RelationsTracker(pawn);
                if (pawn.psychicEntropy == null)
                    pawn.psychicEntropy = new Pawn_PsychicEntropyTracker(pawn);
            }
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
            return false;
        }
    }
}
