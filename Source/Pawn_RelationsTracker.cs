using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
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
        public static AccessTools.FieldRef<Pawn_RelationsTracker, HashSet<Pawn>> cachedFamilyByBloodFieldRef =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, HashSet<Pawn>>("cachedFamilyByBlood");
        public static AccessTools.FieldRef<Pawn_RelationsTracker, HashSet<Pawn>> pawnsWithDirectRelationsWithMe =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, HashSet<Pawn>>("pawnsWithDirectRelationsWithMe");

        public static ConcurrentStack<HashSet<Pawn>> pawnHashsetStack = new ConcurrentStack<HashSet<Pawn>>();
        public static ConcurrentStack<List<Pawn>> pawnListStack = new ConcurrentStack<List<Pawn>>();

        public static AccessTools.FieldRef<Pawn_RelationsTracker, List<DirectPawnRelation>> directRelations =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, List<DirectPawnRelation>>("directRelations");

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_RelationsTracker);
            Type patched = typeof(Pawn_RelationsTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_FamilyByBlood");
            RimThreadedHarmony.Prefix(original, patched, "get_RelatedPawns");
        }

        public static IEnumerable<Pawn> get_PotentiallyRelatedPawns2(Pawn_RelationsTracker __instance)
        {
            if (!__instance.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                yield break;
            }

            List<Pawn> stack = new List<Pawn>();
            HashSet<Pawn> visited = new HashSet<Pawn>();
            try
            {
                //stack = SimplePool<List<Pawn>>.Get();
                //visited = SimplePool<HashSet<Pawn>>.Get();
                stack.Add(pawn(__instance));
                visited.Add(pawn(__instance));
                while (stack.Any())
                {
                    Pawn p = stack[stack.Count - 1];
                    stack.RemoveLast();
                    if (p != pawn(__instance))
                    {
                        yield return p;
                    }

                    for (int i = 0; i < directRelations(p.relations).Count; i++)
                    {
                        Pawn otherPawn = directRelations(p.relations)[i].otherPawn;
                        if (!visited.Contains(otherPawn))
                        {
                            stack.Add(otherPawn);
                            visited.Add(otherPawn);
                        }
                    }

                    foreach (Pawn item in pawnsWithDirectRelationsWithMe(p.relations))
                    {
                        if (!visited.Contains(item))
                        {
                            stack.Add(item);
                            visited.Add(item);
                        }
                    }
                }
            }
            finally
            {
                //stack.Clear();
                //SimplePool<List<Pawn>>.Return(stack);
                //visited.Clear();
                //SimplePool<HashSet<Pawn>>.Return(visited);
            }
            
        }



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
            Pawn pawn2 = null;
            for (int index = 0; index < directRelations(__instance).Count; ++index)
            {
                if (directRelations(__instance)[index].def == PawnRelationDefOf.Bond && directRelations(__instance)[index].otherPawn.Spawned)
                {
                    pawn2 = directRelations(__instance)[index].otherPawn;
                    ++num1;
                    float num2 = Rand.Value;
                    MentalStateDef stateDef = num2 >= 0.25 ? (num2 >= 0.5 ? (num2 >= 0.75 ? MentalStateDefOf.Manhunter : MentalStateDefOf.Berserk) : MentalStateDefOf.Wander_Psychotic) : MentalStateDefOf.Wander_Sad;
                    directRelations(__instance)[index].otherPawn.mindState.mentalStateHandler.TryStartMentalState(stateDef, "MentalStateReason_BondedHumanDeath".Translate(pawn(__instance)).Resolve(), true, false, null, false);
                }
            }
            if (num1 == 1)
            {
                Messages.Message((pawn2.Name == null || pawn2.Name.Numerical ? "MessageBondedAnimalMentalBreak".Translate(pawn2.LabelIndefinite(), pawn(__instance).LabelShort, pawn2.Named("ANIMAL"), pawn(__instance).Named("HUMAN")) : "MessageNamedBondedAnimalMentalBreak".Translate(pawn2.KindLabelIndefinite(), pawn2.Name.ToStringShort, pawn(__instance).LabelShort, pawn2.Named("ANIMAL"), pawn(__instance).Named("HUMAN"))).CapitalizeFirst(), pawn2, MessageTypeDefOf.ThreatSmall, true);
            }
            else
            {
                if (num1 <= 1)
                    return;
                Messages.Message("MessageBondedAnimalsMentalBreak".Translate(num1, pawn(__instance).LabelShort, pawn(__instance).Named("HUMAN")), pawn2, MessageTypeDefOf.ThreatSmall, true);
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
                HashSet<Pawn> cachedFamilyByBlood = new HashSet<Pawn>();
                foreach (Pawn pawn in FamilyByBlood_Internal(__instance))
                    cachedFamilyByBlood.Add(pawn);
                familyByBloodIsCached(__instance) = true;
                cachedFamilyByBloodFieldRef(__instance) = cachedFamilyByBlood;
            }            
            __result = cachedFamilyByBloodFieldRef(__instance);
            return false;
            
        }

        public static bool get_RelatedPawns(Pawn_RelationsTracker __instance, ref IEnumerable<Pawn> __result)
        {
            __result = RelatedPawns2(__instance);
            return false;
        }

        private static IEnumerable<Pawn> RelatedPawns2(Pawn_RelationsTracker __instance)
        {
            canCacheFamilyByBlood(__instance) = true;
            familyByBloodIsCached(__instance) = false;
            cachedFamilyByBloodFieldRef(__instance) = new HashSet<Pawn>();
            try
            {
                foreach (Pawn potentiallyRelatedPawn in __instance.PotentiallyRelatedPawns)
                {
                    if ((familyByBloodIsCached(__instance) && cachedFamilyByBloodFieldRef(__instance).Contains(potentiallyRelatedPawn)) || pawn(__instance).GetRelations(potentiallyRelatedPawn).Any())
                    {
                        yield return potentiallyRelatedPawn;
                    }
                }
            }
            finally
            {
                canCacheFamilyByBlood(__instance) = false;
                familyByBloodIsCached(__instance) = false;
                cachedFamilyByBloodFieldRef(__instance) = new HashSet<Pawn>();
            }
        }

        private static IEnumerable<Pawn> FamilyByBlood_Internal(Pawn_RelationsTracker __instance)
        {
            if (__instance.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                List<Pawn> familyStack = null;
                List<Pawn> familyChildrenStack = null;
                HashSet<Pawn> familyVisited = null;
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
                    while (familyStack.Any())
                    {
                        Pawn p = familyStack[familyStack.Count - 1];
                        familyStack.RemoveLast();
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
                        while (familyChildrenStack.Any())
                        {
                            Pawn child = familyChildrenStack[familyChildrenStack.Count - 1];
                            familyChildrenStack.RemoveLast();
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
                            child = null;
                        }
                        p = null;
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
