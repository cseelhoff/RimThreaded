using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class WorkGiver_DoBill_Patch
    {
		public static AccessTools.FieldRef<AutoUndrafter, Pawn> pawn =
			AccessTools.FieldRefAccess<AutoUndrafter, Pawn>("pawn");

		public class DefCountList
		{
			private List<ThingDef> defs = new List<ThingDef>();

			private List<float> counts = new List<float>();

			public int Count => defs.Count;

			public float this[ThingDef def]
			{
				get
				{
					int num = defs.IndexOf(def);
					if (num < 0)
					{
						return 0f;
					}
					return counts[num];
				}
				set
				{
					int num = defs.IndexOf(def);
					if (num < 0)
					{
						defs.Add(def);
						counts.Add(value);
						num = defs.Count - 1;
					}
					else
					{
						counts[num] = value;
					}
					CheckRemove(num);
				}
			}

			public float GetCount(int index)
			{
				return counts[index];
			}

			public void SetCount(int index, float val)
			{
				counts[index] = val;
				CheckRemove(index);
			}

			public ThingDef GetDef(int index)
			{
				return defs[index];
			}

			private void CheckRemove(int index)
			{
				if (counts[index] == 0f)
				{
					counts.RemoveAt(index);
					defs.RemoveAt(index);
				}
			}

			public void Clear()
			{
				defs.Clear();
				counts.Clear();
			}

			public void GenerateFrom(List<Thing> things)
			{
				Clear();
				for (int i = 0; i < things.Count; i++)
				{
					this[things[i].def] += things[i].stackCount;
				}
			}
		}


		private static IntVec3 GetBillGiverRootCell(Thing billGiver, Pawn forPawn)
		{
			if (!(billGiver is Building building))
				return billGiver.Position;
			if (building.def.hasInteractionCell)
				return building.InteractionCell;
			Log.Error("Tried to find bill ingredients for " + (object)billGiver + " which has no interaction cell.", false);
			return forPawn.Position;
		}
		private static void MakeIngredientsListInProcessingOrder(
	  List<IngredientCount> ingredientsOrdered,
	  Bill bill)
		{
			ingredientsOrdered.Clear();
			if (bill.recipe.productHasIngredientStuff)
				ingredientsOrdered.Add(bill.recipe.ingredients[0]);
			for (int index = 0; index < bill.recipe.ingredients.Count; ++index)
			{
				if (!bill.recipe.productHasIngredientStuff || index != 0)
				{
					IngredientCount ingredient = bill.recipe.ingredients[index];
					if (ingredient.IsFixedIngredient)
						ingredientsOrdered.Add(ingredient);
				}
			}
			for (int index = 0; index < bill.recipe.ingredients.Count; ++index)
			{
				IngredientCount ingredient = bill.recipe.ingredients[index];
				if (!ingredientsOrdered.Contains(ingredient))
					ingredientsOrdered.Add(ingredient);
			}
		}

		private static MedicalCareCategory GetMedicalCareCategory(Thing billGiver)
		{
			return billGiver is Pawn pawn && pawn.playerSettings != null ? pawn.playerSettings.medCare : MedicalCareCategory.Best;
		}
		private static void AddEveryMedicineToRelevantThings(
	  Pawn pawn,
	  Thing billGiver,
	  List<Thing> relevantThings,
	  Predicate<Thing> baseValidator,
	  Map map)
		{
			MedicalCareCategory medicalCareCategory = GetMedicalCareCategory(billGiver);
			List<Thing> thingList = map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);
			//WorkGiver_DoBill.tmpMedicine.Clear();
			List<Thing> tmpMedicine = new List<Thing>();
			for (int index = 0; index < thingList.Count; ++index)
			{
				Thing thing = thingList[index];
				if (medicalCareCategory.AllowsMedicine(thing.def) && baseValidator(thing) && pawn.CanReach((LocalTargetInfo)thing, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
					tmpMedicine.Add(thing);
			}
			tmpMedicine.SortBy<Thing, float, int>((Func<Thing, float>)(x => -x.GetStatValue(StatDefOf.MedicalPotency, true)), (Func<Thing, int>)(x => x.Position.DistanceToSquared(billGiver.Position)));
			relevantThings.AddRange((IEnumerable<Thing>)tmpMedicine);
			//WorkGiver_DoBill.tmpMedicine.Clear();
		}

		public static bool TryFindBestBillIngredients(ref bool __result,
		  Bill bill,
		  Pawn pawn,
		  Thing billGiver,
		  List<ThingCount> chosen)
		{
			chosen.Clear();
			//WorkGiver_DoBill.newRelevantThings.Clear();
			List<Thing> newRelevantThings = new List<Thing>();
			if (bill.recipe.ingredients.Count == 0)
			{
				__result = true;
				return false;
			}
			IntVec3 rootCell = GetBillGiverRootCell(billGiver, pawn);
			Region rootReg = rootCell.GetRegion(pawn.Map, RegionType.Set_Passable);
			if (rootReg == null)
			{
				__result = false;
				return false;
			}
			List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();
			MakeIngredientsListInProcessingOrder(ingredientsOrdered, bill);
			//WorkGiver_DoBill.relevantThings.Clear();
			List<Thing> relevantThings = new List<Thing>();
			//WorkGiver_DoBill.processedThings.Clear();
			HashSet<Thing> processedThings = new HashSet<Thing>();
			bool foundAll = false;
			Predicate<Thing> baseValidator = (Predicate<Thing>)(t => t.Spawned && !t.IsForbidden(pawn) && ((double)(t.Position - billGiver.Position).LengthHorizontalSquared < (double)bill.ingredientSearchRadius * (double)bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any<IngredientCount>((Predicate<IngredientCount>)(ingNeed => ingNeed.filter.Allows(t)))) && pawn.CanReserve((LocalTargetInfo)t, 1, -1, (ReservationLayerDef)null, false));
			bool billGiverIsPawn = billGiver is Pawn;
			if (billGiverIsPawn)
			{
				AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
				if (TryFindBestBillIngredientsInSet2(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
				{
					relevantThings.Clear();
					ingredientsOrdered.Clear();
					{
						__result = true;
						return false;
					}
				}
			}
			TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
			RegionEntryPredicate entryCondition = (RegionEntryPredicate)null;
			if ((double)Math.Abs(999f - bill.ingredientSearchRadius) >= 1.0)
			{
				float radiusSq = bill.ingredientSearchRadius * bill.ingredientSearchRadius;
				entryCondition = (RegionEntryPredicate)((from, r) =>
				{
					if (!r.Allows(traverseParams, false))
						return false;
					CellRect extentsClose = r.extentsClose;
					int num1 = Math.Abs(billGiver.Position.x - Math.Max(extentsClose.minX, Math.Min(billGiver.Position.x, extentsClose.maxX)));
					if ((double)num1 > (double)bill.ingredientSearchRadius)
						return false;
					int num2 = Math.Abs(billGiver.Position.z - Math.Max(extentsClose.minZ, Math.Min(billGiver.Position.z, extentsClose.maxZ)));
					return (double)num2 <= (double)bill.ingredientSearchRadius && (double)(num1 * num1 + num2 * num2) <= (double)radiusSq;
				});
			}
			else
				entryCondition = (RegionEntryPredicate)((from, r) => r.Allows(traverseParams, false));
			int adjacentRegionsAvailable = rootReg.Neighbors.Count<Region>((Func<Region, bool>)(region => entryCondition(rootReg, region)));
			int regionsProcessed = 0;
			processedThings.AddRange<Thing>(relevantThings);
			RegionProcessor regionProcessor = (RegionProcessor)(r =>
			{
				List<Thing> thingList = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
				for (int index = 0; index < thingList.Count; ++index)
				{
					Thing thing = thingList[index];
					if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && (baseValidator(thing) && !(thing.def.IsMedicine & billGiverIsPawn)))
					{
						newRelevantThings.Add(thing);
						processedThings.Add(thing);
					}
				}
				++regionsProcessed;
				if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
				{
					relevantThings.AddRange((IEnumerable<Thing>)newRelevantThings);
					newRelevantThings.Clear();
					if (TryFindBestBillIngredientsInSet2(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
					{
						foundAll = true;
						return true;
					}
				}
				return false;
			});
			RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
			//WorkGiver_DoBill.relevantThings.Clear();
			//WorkGiver_DoBill.newRelevantThings.Clear();
			//WorkGiver_DoBill.processedThings.Clear();
			//WorkGiver_DoBill.ingredientsOrdered.Clear();
			__result = foundAll;
			return false;
		}
		private static bool TryFindBestBillIngredientsInSet_AllowMix(
	  List<Thing> availableThings,
	  Bill bill,
	  List<ThingCount> chosen)
		{
			chosen.Clear();
			availableThings.Sort((Comparison<Thing>)((t, t2) => bill.recipe.IngredientValueGetter.ValuePerUnitOf(t2.def).CompareTo(bill.recipe.IngredientValueGetter.ValuePerUnitOf(t.def))));
			for (int index1 = 0; index1 < bill.recipe.ingredients.Count; ++index1)
			{
				IngredientCount ingredient = bill.recipe.ingredients[index1];
				float baseCount = ingredient.GetBaseCount();
				for (int index2 = 0; index2 < availableThings.Count; ++index2)
				{
					Thing availableThing = availableThings[index2];
					if (ingredient.filter.Allows(availableThing) && (ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(availableThing)))
					{
						float num = bill.recipe.IngredientValueGetter.ValuePerUnitOf(availableThing.def);
						int countToAdd = Mathf.Min(Mathf.CeilToInt(baseCount / num), availableThing.stackCount);
						ThingCountUtility.AddToList(chosen, availableThing, countToAdd);
						baseCount -= (float)countToAdd * num;
						if ((double)baseCount <= 9.99999974737875E-05)
							break;
					}
				}
				if ((double)baseCount > 9.99999974737875E-05)
					return false;
			}
			return true;
		}

		private static bool TryFindBestBillIngredientsInSet2(
	  List<Thing> availableThings,
	  Bill bill,
	  List<ThingCount> chosen,
	  IntVec3 rootCell,
	  bool alreadySorted, List<IngredientCount> ingredientsOrdered)
		{
			return bill.recipe.allowMixingIngredients ? TryFindBestBillIngredientsInSet_AllowMix(availableThings, bill, chosen) : TryFindBestBillIngredientsInSet_NoMix2(availableThings, bill, chosen, rootCell, alreadySorted, ingredientsOrdered);
		}

		private static bool TryFindBestBillIngredientsInSet_NoMix2(
		  List<Thing> availableThings,
		  Bill bill,
		  List<ThingCount> chosen,
		  IntVec3 rootCell,
		  bool alreadySorted, List<IngredientCount> ingredientsOrdered)
		{
			if (!alreadySorted)
			{
				Comparison<Thing> comparison = (Comparison<Thing>)((t1, t2) => ((float)(t1.Position - rootCell).LengthHorizontalSquared).CompareTo((float)(t2.Position - rootCell).LengthHorizontalSquared));
				availableThings.Sort(comparison);
			}
			RecipeDef recipe = bill.recipe;
			chosen.Clear();
			//WorkGiver_DoBill.availableCounts.Clear();
			DefCountList availableCounts = new DefCountList();
			availableCounts.GenerateFrom(availableThings);
			for (int index1 = 0; index1 < ingredientsOrdered.Count; ++index1)
			{
				IngredientCount ingredient = recipe.ingredients[index1];
				bool flag = false;
				for (int index2 = 0; index2 < availableCounts.Count; ++index2)
				{
					float f = (float)ingredient.CountRequiredOfFor(availableCounts.GetDef(index2), bill.recipe);
					if ((recipe.ignoreIngredientCountTakeEntireStacks || (double)f <= (double)availableCounts.GetCount(index2)) && ingredient.filter.Allows(availableCounts.GetDef(index2)) && (ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(availableCounts.GetDef(index2))))
					{
						for (int index3 = 0; index3 < availableThings.Count; ++index3)
						{
							if (availableThings[index3].def == availableCounts.GetDef(index2))
							{
								int num = availableThings[index3].stackCount - ThingCountUtility.CountOf(chosen, availableThings[index3]);
								if (num > 0)
								{
									if (recipe.ignoreIngredientCountTakeEntireStacks)
									{
										ThingCountUtility.AddToList(chosen, availableThings[index3], num);
										return true;
									}
									int countToAdd = Mathf.Min(Mathf.FloorToInt(f), num);
									ThingCountUtility.AddToList(chosen, availableThings[index3], countToAdd);
									f -= (float)countToAdd;
									if ((double)f < 1.0 / 1000.0)
									{
										flag = true;
										float val = availableCounts.GetCount(index2) - (float)ingredient.CountRequiredOfFor(availableCounts.GetDef(index2), bill.recipe);
										availableCounts.SetCount(index2, val);
										break;
									}
								}
							}
						}
						if (flag)
							break;
					}
				}
				if (!flag)
					return false;
			}
			return true;
		}

	}
}
