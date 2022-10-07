using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static WorkGiver_DoBill_Patch;

namespace RimThreaded.RW_Patches
{
    class WorkGiver_Scanner_Patch
    {

        public static bool HasJobOnThing(WorkGiver_DoBill __instance, Pawn pawn, Thing thing, bool forced = false)
        {
            IBillGiver billGiver = thing as IBillGiver;
            if (billGiver == null || !__instance.ThingIsUsableBillGiver(thing) || !billGiver.BillStack.AnyShouldDoNow || !billGiver.UsableForBillsAfterFueling() || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning() || thing.IsForbidden(pawn))
            {
                return false;
            }

            CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
            if (compRefuelable != null && !compRefuelable.HasFuel)
            {
                if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced))
                {
                    return false;
                }

                return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced) != null;
            }

            billGiver.BillStack.RemoveIncompletableBills();
            return CanStartOrResumeBillJob(__instance, pawn, billGiver);
        }
        private static bool CanStartOrResumeBillJob(WorkGiver_DoBill __instance, Pawn pawn, IBillGiver giver)
        {
            for (int i = 0; i < giver.BillStack.Count; i++)
            {
                Bill bill = giver.BillStack[i];
                if (bill.recipe.requiredGiverWorkType != null && bill.recipe.requiredGiverWorkType != __instance.def.workType ||
                    Find.TickManager.TicksGame < bill.lastIngredientSearchFailTicks + WorkGiver_DoBill.ReCheckFailedBillTicksRange.RandomInRange && FloatMenuMakerMap.makingFor != pawn)
                {
                    continue;
                }

                bill.lastIngredientSearchFailTicks = 0;
                if (!bill.ShouldDoNow() || !bill.PawnAllowedToStartAnew(pawn))
                {
                    continue;
                }

                SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
                if (skillRequirement != null)
                {
                    JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                    continue;
                }

                if (bill is Bill_Medical && ((Bill_Medical)bill).IsSurgeryViolationOnExtraFactionMember(pawn))
                {
                    JobFailReason.Is("SurgeryViolationFellowFactionMember".Translate());
                    continue;
                }

                Bill_ProductionWithUft bill_ProductionWithUft = bill as Bill_ProductionWithUft;
                if (bill_ProductionWithUft != null)
                {
                    if (bill_ProductionWithUft.BoundUft != null)
                    {
                        if (bill_ProductionWithUft.BoundWorker == pawn && pawn.CanReserveAndReach(bill_ProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly) && !bill_ProductionWithUft.BoundUft.IsForbidden(pawn))
                        {
                            return true;
                        }

                        continue;
                    }

                    if (AnyUnfinishedThingForBill(__instance, pawn, bill_ProductionWithUft))
                    {
                        return true;
                    }
                }
                List<ThingCount> chosenIngThings = new List<ThingCount>();
                if (!TryFindAnyBillIngredients(__instance, bill, pawn, (Thing)giver, chosenIngThings))
                {
                    if (FloatMenuMakerMap.makingFor != pawn)
                    {
                        bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                    }

                    continue;
                }

                return true;
            }

            return false;
        }


        private static bool AnyUnfinishedThingForBill(WorkGiver_DoBill __instance, Pawn pawn, Bill_ProductionWithUft bill)
        {
            Predicate<Thing> validator = (t) => !t.IsForbidden(pawn) && ((UnfinishedThing)t).Recipe == bill.recipe && ((UnfinishedThing)t).Creator == pawn && ((UnfinishedThing)t).ingredients.TrueForAll((x) => bill.IsFixedOrAllowedIngredient(x.def)) && pawn.CanReserve(t);
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(bill.recipe.unfinishedThingDef), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, validator) != null;
        }
        private static bool TryFindAnyBillIngredients(WorkGiver_DoBill __instance, Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen)
        {
            chosen.Clear();
            List<Thing> relevantThings = new List<Thing>();
            if (bill.recipe.ingredients.Count == 0)
            {
                return true;
            }

            IntVec3 rootCell = WorkGiver_DoBill.GetBillGiverRootCell(billGiver, pawn);
            Region rootReg = rootCell.GetRegion(pawn.Map);
            if (rootReg == null)
            {
                return false;
            }
            List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();
            //WorkGiver_DoBill.MakeIngredientsListInProcessingOrder(ingredientsOrdered, bill); this methods doesn't exist anymore
            relevantThings.Clear();
            HashSet<Thing> processedThings = new HashSet<Thing>();
            bool foundAll = false;
            Predicate<Thing> baseValidator = (t) => t.Spawned && !t.IsForbidden(pawn) && (t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any((ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t);
            bool billGiverIsPawn = billGiver is Pawn;
            if (billGiverIsPawn)
            {
                WorkGiver_DoBill.AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
                if (TryFindAnyBillIngredientsInSet(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
                {
                    relevantThings.Clear();
                    ingredientsOrdered.Clear();
                    return true;
                }
            }

            TraverseParms traverseParams = TraverseParms.For(pawn);
            RegionEntryPredicate entryCondition = null;
            if (Math.Abs(999f - bill.ingredientSearchRadius) >= 1f)
            {
                float radiusSq = bill.ingredientSearchRadius * bill.ingredientSearchRadius;
                entryCondition = delegate (Region from, Region r)
                {
                    if (!r.Allows(traverseParams, isDestination: false))
                    {
                        return false;
                    }

                    CellRect extentsClose = r.extentsClose;
                    int num = Math.Abs(billGiver.Position.x - Math.Max(extentsClose.minX, Math.Min(billGiver.Position.x, extentsClose.maxX)));
                    if (num > bill.ingredientSearchRadius)
                    {
                        return false;
                    }

                    int num2 = Math.Abs(billGiver.Position.z - Math.Max(extentsClose.minZ, Math.Min(billGiver.Position.z, extentsClose.maxZ)));
                    return !(num2 > bill.ingredientSearchRadius) && num * num + num2 * num2 <= radiusSq;
                };
            }
            else
            {
                entryCondition = (from, r) => r.Allows(traverseParams, isDestination: false);
            }

            int adjacentRegionsAvailable = rootReg.Neighbors.Count((region) => entryCondition(rootReg, region));
            int regionsProcessed = 0;
            processedThings.AddRange(relevantThings);
            List<Thing> newRelevantThings = new List<Thing>();
            RegionProcessor regionProcessor = delegate (Region r)
            {
                List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing = list[i];
                    if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing) && !(thing.def.IsMedicine && billGiverIsPawn))
                    {
                        newRelevantThings.Add(thing);
                        processedThings.Add(thing);
                    }
                }

                regionsProcessed++;
                if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                {
                    relevantThings.AddRange(newRelevantThings);
                    newRelevantThings.Clear();
                    if (TryFindAnyBillIngredientsInSet(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
                    {
                        foundAll = true;
                        return true;
                    }
                }

                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999);
            relevantThings.Clear();
            newRelevantThings.Clear();
            processedThings.Clear();
            ingredientsOrdered.Clear();
            return foundAll;
        }
        private static bool TryFindAnyBillIngredientsInSet(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, List<IngredientCount> ingredientsOrdered)
        {
            if (bill.recipe.allowMixingIngredients)
            {
                return TryFindAnyBillIngredientsInSet_AllowMix(availableThings, bill, chosen);
            }

            return TryFindAnyBillIngredientsInSet_NoMix(availableThings, bill, chosen, rootCell, alreadySorted, ingredientsOrdered);
        }
        private static bool TryFindAnyBillIngredientsInSet_NoMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, List<IngredientCount> ingredientsOrdered)
        {
            /*
            if (!alreadySorted)
            {
                Comparison<Thing> comparison = delegate (Thing t1, Thing t2)
                {
                    float num4 = (t1.Position - rootCell).LengthHorizontalSquared;
                    float value = (t2.Position - rootCell).LengthHorizontalSquared;
                    return num4.CompareTo(value);
                };
                availableThings.Sort(comparison);
            }
            */
            RecipeDef recipe = bill.recipe;
            chosen.Clear();
            DefCountList availableCounts = new DefCountList();
            //availableCounts.Clear();
            availableCounts.GenerateFrom(availableThings);
            for (int i = 0; i < ingredientsOrdered.Count; i++)
            {
                IngredientCount ingredientCount = recipe.ingredients[i];
                bool flag = false;
                for (int j = 0; j < availableCounts.Count; j++)
                {
                    float num = ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                    if (!recipe.ignoreIngredientCountTakeEntireStacks && num > availableCounts.GetCount(j) || !ingredientCount.filter.Allows(availableCounts.GetDef(j)) || !ingredientCount.IsFixedIngredient && !bill.ingredientFilter.Allows(availableCounts.GetDef(j)))
                    {
                        continue;
                    }

                    for (int k = 0; k < availableThings.Count; k++)
                    {
                        if (availableThings[k].def != availableCounts.GetDef(j))
                        {
                            continue;
                        }

                        int num2 = availableThings[k].stackCount - ThingCountUtility.CountOf(chosen, availableThings[k]);
                        if (num2 > 0)
                        {
                            if (recipe.ignoreIngredientCountTakeEntireStacks)
                            {
                                ThingCountUtility.AddToList(chosen, availableThings[k], num2);
                                return true;
                            }

                            int num3 = Mathf.Min(Mathf.FloorToInt(num), num2);
                            ThingCountUtility.AddToList(chosen, availableThings[k], num3);
                            num -= num3;
                            if (num < 0.001f)
                            {
                                flag = true;
                                float count = availableCounts.GetCount(j);
                                count -= ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                                availableCounts.SetCount(j, count);
                                break;
                            }
                        }
                    }

                    if (flag)
                    {
                        break;
                    }
                }

                if (!flag)
                {
                    return false;
                }
            }

            return true;
        }


        private static bool TryFindAnyBillIngredientsInSet_AllowMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen)
        {
            chosen.Clear();
            //availableThings.Sort((Thing t, Thing t2) => bill.recipe.IngredientValueGetter.ValuePerUnitOf(t2.def).CompareTo(bill.recipe.IngredientValueGetter.ValuePerUnitOf(t.def)));
            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                IngredientCount ingredientCount = bill.recipe.ingredients[i];
                float num = ingredientCount.GetBaseCount();
                for (int j = 0; j < availableThings.Count; j++)
                {
                    Thing thing = availableThings[j];
                    if (ingredientCount.filter.Allows(thing) && (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(thing)))
                    {
                        float num2 = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                        int num3 = Mathf.Min(Mathf.CeilToInt(num / num2), thing.stackCount);
                        ThingCountUtility.AddToList(chosen, thing, num3);
                        num -= num3 * num2;
                        if (num <= 0.0001f)
                        {
                            break;
                        }
                    }
                }

                if (num > 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }


    }
}
