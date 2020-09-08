using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections.Concurrent;

namespace RimThreaded
{

    public class Pawn_RelationsTracker_Patch
    {
        public static AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> pawn =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");
        public static AccessTools.FieldRef<Pawn_RelationsTracker, bool> canCacheFamilyByBlood =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, bool>("canCacheFamilyByBlood");
        public static AccessTools.FieldRef<Pawn_RelationsTracker, bool> familyByBloodIsCached =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, bool>("familyByBloodIsCached");
        public static AccessTools.FieldRef<Pawn_RelationsTracker, HashSet<Pawn>> cachedFamilyByBlood =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, HashSet<Pawn>>("cachedFamilyByBlood");
        public static ConcurrentStack<HashSet<Pawn>> pawnHashsetStack = new ConcurrentStack<HashSet<Pawn>>();
        public static ConcurrentStack<List<Pawn>> pawnListStack = new ConcurrentStack<List<Pawn>>();

        public static AccessTools.FieldRef<Pawn_RelationsTracker, List<DirectPawnRelation>> directRelations =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, List<DirectPawnRelation>>("directRelations");


        public static void Notify_PawnKilled(Pawn_RelationsTracker __instance, DamageInfo? dinfo, Map mapBeforeDeath)
        {
            foreach (Pawn potentiallyRelatedPawn in __instance.PotentiallyRelatedPawns)
            {
                if (!potentiallyRelatedPawn.Dead && potentiallyRelatedPawn.needs.mood != null)
                    potentiallyRelatedPawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
            RemoveMySpouseMarriageRelatedThoughts(__instance);
            if (__instance.everSeenByPlayer && !PawnGenerator.IsBeingGenerated(pawn(__instance)) && !pawn(__instance).RaceProps.Animal)
                AffectBondedAnimalsOnMyDeath(__instance);
            __instance.Notify_FailedRescueQuest();
        }

        public static void AffectBondedAnimalsOnMyDeath(Pawn_RelationsTracker __instance)
        {
            int num1 = 0;
            Pawn pawn2 = (Pawn)null;
            for (int index = 0; index < directRelations(__instance).Count; ++index)
            {
                if (directRelations(__instance)[index].def == PawnRelationDefOf.Bond && directRelations(__instance)[index].otherPawn.Spawned)
                {
                    pawn2 = directRelations(__instance)[index].otherPawn;
                    ++num1;
                    float num2 = Rand.Value;
                    MentalStateDef stateDef = (double)num2 >= 0.25 ? ((double)num2 >= 0.5 ? ((double)num2 >= 0.75 ? MentalStateDefOf.Manhunter : MentalStateDefOf.Berserk) : MentalStateDefOf.Wander_Psychotic) : MentalStateDefOf.Wander_Sad;
                    directRelations(__instance)[index].otherPawn.mindState.mentalStateHandler.TryStartMentalState(stateDef, "MentalStateReason_BondedHumanDeath".Translate((NamedArgument)(Thing)pawn(__instance)).Resolve(), true, false, (Pawn)null, false);
                }
            }
            if (num1 == 1)
            {
                Messages.Message((string)(pawn2.Name == null || pawn2.Name.Numerical ? "MessageBondedAnimalMentalBreak".Translate((NamedArgument)pawn2.LabelIndefinite(), (NamedArgument)pawn(__instance).LabelShort, pawn2.Named("ANIMAL"), pawn(__instance).Named("HUMAN")) : "MessageNamedBondedAnimalMentalBreak".Translate((NamedArgument)pawn2.KindLabelIndefinite(), (NamedArgument)pawn2.Name.ToStringShort, (NamedArgument)pawn(__instance).LabelShort, pawn2.Named("ANIMAL"), pawn(__instance).Named("HUMAN"))).CapitalizeFirst(), (LookTargets)(Thing)pawn2, MessageTypeDefOf.ThreatSmall, true);
            }
            else
            {
                if (num1 <= 1)
                    return;
                Messages.Message((string)"MessageBondedAnimalsMentalBreak".Translate((NamedArgument)num1, (NamedArgument)pawn(__instance).LabelShort, pawn(__instance).Named("HUMAN")), (LookTargets)(Thing)pawn2, MessageTypeDefOf.ThreatSmall, true);
            }
        }
        public static void RemoveMySpouseMarriageRelatedThoughts(Pawn_RelationsTracker __instance)
        {
            Pawn spouse = pawn(__instance).GetSpouse();
            if (spouse == null || spouse.Dead || spouse.needs.mood == null)
                return;
            MemoryThoughtHandler memories = spouse.needs.mood.thoughts.memories;
            memories.RemoveMemoriesOfDef(ThoughtDefOf.GotMarried);
            memories.RemoveMemoriesOfDef(ThoughtDefOf.HoneymoonPhase);
        }
        public static bool get_FamilyByBlood(Pawn_RelationsTracker __instance, ref IEnumerable<Pawn> __result)
        {
            if (!canCacheFamilyByBlood(__instance))
            {
                __result = FamilyByBlood_Internal(__instance);
                return false;
            }
            if (!familyByBloodIsCached(__instance))
            {
                cachedFamilyByBlood(__instance).Clear();
                foreach (Pawn pawn in FamilyByBlood_Internal(__instance))
                    cachedFamilyByBlood(__instance).Add(pawn);
                familyByBloodIsCached(__instance) = true;
            }
            __result = (IEnumerable<Pawn>)cachedFamilyByBlood(__instance);
            return false;
            
        }

        private static IEnumerable<Pawn> FamilyByBlood_Internal(Pawn_RelationsTracker __instance)
        {
            if (__instance.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                List<Pawn> familyStack = (List<Pawn>)null;
                List<Pawn> familyChildrenStack = (List<Pawn>)null;
                HashSet<Pawn> familyVisited = (HashSet<Pawn>)null;
                try
                {
                    //familyStack = SimplePool<List<Pawn>>.Get();
                    if (!pawnListStack.TryPop(out familyStack))
                        familyStack = new List<Pawn>();
                    //familyChildrenStack = SimplePool<List<Pawn>>.Get();
                    if (!pawnListStack.TryPop(out familyChildrenStack))
                        familyChildrenStack = new List<Pawn>();
                    //familyVisited = SimplePool<HashSet<Pawn>>.Get();
                    if (!pawnHashsetStack.TryPop(out familyVisited))
                        familyVisited = new HashSet<Pawn>();
                    familyStack.Add(pawn(__instance));
                    familyVisited.Add(pawn(__instance));
                    while (familyStack.Any<Pawn>())
                    {
                        Pawn p = familyStack[familyStack.Count - 1];
                        familyStack.RemoveLast<Pawn>();
                        if (p != pawn(__instance))
                            yield return p;
                        Pawn father = p.GetFather();
                        if (father != null && !familyVisited.Contains(father))
                        {
                            familyStack.Add(father);
                            familyVisited.Add(father);
                        }
                        Pawn mother = p.GetMother();
                        if (mother != null && !familyVisited.Contains(mother))
                        {
                            familyStack.Add(mother);
                            familyVisited.Add(mother);
                        }
                        familyChildrenStack.Clear();
                        familyChildrenStack.Add(p);
                        while (familyChildrenStack.Any<Pawn>())
                        {
                            Pawn child = familyChildrenStack[familyChildrenStack.Count - 1];
                            familyChildrenStack.RemoveLast<Pawn>();
                            if (child != p && child != pawn(__instance))
                                yield return child;
                            foreach (Pawn child1 in child.relations.Children)
                            {
                                if (!familyVisited.Contains(child1))
                                {
                                    familyChildrenStack.Add(child1);
                                    familyVisited.Add(child1);
                                }
                            }
                            child = (Pawn)null;
                        }
                        p = (Pawn)null;
                    }
                }
                finally
                {
                    familyStack.Clear();
                    //SimplePool<List<Pawn>>.Return(familyStack);
                    pawnListStack.Push(familyStack);
                    familyChildrenStack.Clear();
                    //SimplePool<List<Pawn>>.Return(familyChildrenStack);
                    pawnListStack.Push(familyChildrenStack);
                    familyVisited.Clear();
                    //SimplePool<HashSet<Pawn>>.Return(familyVisited);
                    pawnHashsetStack.Push(familyVisited);
                }
            }
        }

    }
}
