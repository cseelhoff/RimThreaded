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
    public class WorkGiver_DoBill_RegionProcessor
    {
		public List<Thing> newRelevantThings = new List<Thing>();
		public List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();
		public List<Thing> relevantThings = new List<Thing>();
		public HashSet<Thing> processedThings = new HashSet<Thing>();
		public Bill bill;
		public Pawn pawn;
		public Predicate<Thing> baseValidator;
		public bool billGiverIsPawn;
		public int adjacentRegionsAvailable;
		public IntVec3 rootCell;
		public List<ThingCount> chosen;
		public int regionsProcessed = 0;
		public bool foundAll = false;

        public WorkGiver_DoBill_RegionProcessor()
        {
        }

        public bool Get_RegionProcessor(Region r)
        {
			RecipeDef recipe = bill.recipe;
            Dictionary<float, List<ThingDef>> thingDefValues = RimThreaded.recipeThingDefValues[recipe];
			foreach (float value in RimThreaded.sortedRecipeValues[recipe])
			{
				foreach (ThingDef thingDef in thingDefValues[value])
				{
					List<Thing> thingList = r.ListerThings.ThingsOfDef(thingDef);
					for (int index = 0; index < thingList.Count; ++index)
					{
						Thing thing = thingList[index];
						if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(
							thing, r, PathEndMode.ClosestTouch, pawn) && (baseValidator(thing) && !(thing.def.IsMedicine & billGiverIsPawn)))
						{
							newRelevantThings.Add(thing);
							processedThings.Add(thing);
						}
					}
				}
			}
			++regionsProcessed;
			if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
			{
				relevantThings.AddRange(newRelevantThings);
				newRelevantThings.Clear();
				if (WorkGiver_DoBill_Patch.TryFindBestBillIngredientsInSet2(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
				{
					foundAll = true;
					return true;
				}
			}
			return false;
		}
    }
    public class WorkGiver_DoBill_Patch
    {
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
			Log.Error("Tried to find bill ingredients for " + billGiver + " which has no interaction cell.", false);
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
		public static void AddEveryMedicineToRelevantThings2(
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
				if (medicalCareCategory.AllowsMedicine(thing.def) && baseValidator(thing) && pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
					tmpMedicine.Add(thing);
			}
			tmpMedicine.SortBy(x => -x.GetStatValue(StatDefOf.MedicalPotency, true), x => x.Position.DistanceToSquared(billGiver.Position));
			relevantThings.AddRange(tmpMedicine);
			//WorkGiver_DoBill.tmpMedicine.Clear();
		}

		public static bool TryFindBestBillIngredients(ref bool __result,
		  Bill bill,
		  Pawn pawn,
		  Thing billGiver,
		  List<ThingCount> chosen)
		{
			WorkGiver_DoBill_RegionProcessor workGiver_DoBill_RegionProcessor = new WorkGiver_DoBill_RegionProcessor(); //ADD
			chosen.Clear(); //COULD REMOVE?
			//newRelevantThings.Clear(); //REMOVE
			if (bill.recipe.ingredients.Count == 0)
			{
				__result = true;
				return false;
			}

			IntVec3 rootCell = GetBillGiverRootCell(billGiver, pawn);
			Region rootReg = rootCell.GetRegion(pawn.Map);
			if (rootReg == null)
			{
				__result = false;
				return false;
			}

			MakeIngredientsListInProcessingOrder(workGiver_DoBill_RegionProcessor.ingredientsOrdered, bill); //CHANGE
			//WorkGiver_DoBill.relevantThings.Clear(); REMOVE
			//WorkGiver_DoBill.processedThings.Clear(); REMOVE
			//bool foundAll = false; REMOVE
			Predicate<Thing> baseValidator = t => t.Spawned && !t.IsForbidden(pawn) && 
				((t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && 
				bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any(
				ingNeed => ingNeed.filter.Allows(t))) && pawn.CanReserve(t, 1, -1, null, false);
			bool billGiverIsPawn = billGiver is Pawn;
			if (billGiverIsPawn)
			{
				AddEveryMedicineToRelevantThings2(pawn, billGiver, workGiver_DoBill_RegionProcessor.relevantThings, baseValidator, pawn.Map); //CHANGE
				if (TryFindBestBillIngredientsInSet2(workGiver_DoBill_RegionProcessor.relevantThings, bill, chosen, rootCell, billGiverIsPawn, workGiver_DoBill_RegionProcessor.ingredientsOrdered)) //CHANGE x2
				{
					workGiver_DoBill_RegionProcessor.relevantThings.Clear(); //CHANGE
					workGiver_DoBill_RegionProcessor.ingredientsOrdered.Clear(); //CHANGE
					{
						__result = true;
						return false;
					}
				}
			}

			TraverseParms traverseParams = TraverseParms.For(pawn);
			RegionEntryPredicate entryCondition = null;
			if (Math.Abs(999f - bill.ingredientSearchRadius) >= 1.0)
			{
				float radiusSq = bill.ingredientSearchRadius * bill.ingredientSearchRadius;
				entryCondition = delegate (Region from, Region r)
				{
					if (!r.Allows(traverseParams, false))
						return false;
					CellRect extentsClose = r.extentsClose;
					int num1 = Math.Abs(billGiver.Position.x - Math.Max(extentsClose.minX, Math.Min(billGiver.Position.x, extentsClose.maxX)));
					if (num1 > bill.ingredientSearchRadius)
						return false;
					int num2 = Math.Abs(billGiver.Position.z - Math.Max(extentsClose.minZ, Math.Min(billGiver.Position.z, extentsClose.maxZ)));
					return num2 <= bill.ingredientSearchRadius && (num1 * num1 + num2 * num2) <= radiusSq;
				};
			}
			else
				entryCondition = ((Region from, Region r) => r.Allows(traverseParams, isDestination: false));

			int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
			//int regionsProcessed = 0; REMOVE
			workGiver_DoBill_RegionProcessor.processedThings.AddRange(workGiver_DoBill_RegionProcessor.relevantThings); //CHANGE x2
																														//processedThings, pawn, baseValidator, billGiverIsPawn, newRelevantThings, adjacentRegionsAvailable, relevantThings, bill, chosen, rootCell, ingredientsOrdered

			workGiver_DoBill_RegionProcessor.bill = bill;//ADD
			workGiver_DoBill_RegionProcessor.pawn = pawn; //ADD
			workGiver_DoBill_RegionProcessor.baseValidator = baseValidator;//ADD
			workGiver_DoBill_RegionProcessor.billGiverIsPawn = billGiverIsPawn;//ADD
			workGiver_DoBill_RegionProcessor.adjacentRegionsAvailable = adjacentRegionsAvailable;//ADD
			workGiver_DoBill_RegionProcessor.rootCell = rootCell;//ADD
			workGiver_DoBill_RegionProcessor.chosen = chosen;//ADD

			RegionProcessor regionProcessor = workGiver_DoBill_RegionProcessor.Get_RegionProcessor; //CHANGE
			RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999);

			//WorkGiver_DoBill.relevantThings.Clear(); REMOVE
			//WorkGiver_DoBill.newRelevantThings.Clear(); REMOVE
			//WorkGiver_DoBill.processedThings.Clear(); REMOVE
			//WorkGiver_DoBill.ingredientsOrdered.Clear(); REMOVE
			__result = workGiver_DoBill_RegionProcessor.foundAll; //CHANGE
			return false;
		}


		private static bool TryFindBestBillIngredientsInSet_AllowMix(
	  List<Thing> availableThings,
	  Bill bill,
	  List<ThingCount> chosen)
		{
			RecipeDef recipe = bill.recipe;
			chosen.Clear();
			availableThings.Sort((t, t2) => recipe.IngredientValueGetter.ValuePerUnitOf(t2.def).CompareTo(recipe.IngredientValueGetter.ValuePerUnitOf(t.def)));
			for (int index1 = 0; index1 < recipe.ingredients.Count; ++index1)
			{
				IngredientCount ingredient = recipe.ingredients[index1];
				float baseCount = ingredient.GetBaseCount();
				for (int index2 = 0; index2 < availableThings.Count; ++index2)
				{
					Thing availableThing = availableThings[index2];
					if (ingredient.filter.Allows(availableThing) && (ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(availableThing)))
					{
						float num = recipe.IngredientValueGetter.ValuePerUnitOf(availableThing.def);
						int countToAdd = Mathf.Min(Mathf.CeilToInt(baseCount / num), availableThing.stackCount);
						ThingCountUtility.AddToList(chosen, availableThing, countToAdd);
						baseCount -= countToAdd * num;
						if (baseCount <= 9.99999974737875E-05)
							break;
					}
				}
				if (baseCount > 9.99999974737875E-05)
					return false;
			}
			return true;
		}

		public static bool TryFindBestBillIngredientsInSet2(
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
				Comparison<Thing> comparison = (t1, t2) => ((float)(t1.Position - rootCell).LengthHorizontalSquared).CompareTo((t2.Position - rootCell).LengthHorizontalSquared);
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
					float f = ingredient.CountRequiredOfFor(availableCounts.GetDef(index2), bill.recipe);
					if ((recipe.ignoreIngredientCountTakeEntireStacks || f <= availableCounts.GetCount(index2)) && ingredient.filter.Allows(availableCounts.GetDef(index2)) && (ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(availableCounts.GetDef(index2))))
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
									f -= countToAdd;
									if (f < 1.0 / 1000.0)
									{
										flag = true;
										float val = availableCounts.GetCount(index2) - ingredient.CountRequiredOfFor(availableCounts.GetDef(index2), bill.recipe);
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
