using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    
    public class Thing_Patch
	{
#pragma warning disable IDE0060 // Remove unused parameter
        public static void SpawnSetup(Thing __instance, Map map, bool respawningAfterLoad)
#pragma warning restore IDE0060 // Remove unused parameter
        {
			ThingDef thingDef = __instance.def;
			if (!RimThreaded.recipeThingDefs.Contains(thingDef))
			{
				lock (RimThreaded.recipeThingDefs)
				{
					//Log.Message("RimThreaded is building new recipe caches for: " + thingDef.ToString());
					RimThreaded.recipeThingDefs.Add(thingDef);
					foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
					{
						if (!RimThreaded.sortedRecipeValues.TryGetValue(recipe, out List<float> valuesPerUnitOf))
						{
							valuesPerUnitOf = new List<float>();
							RimThreaded.sortedRecipeValues[recipe] = valuesPerUnitOf;
						}
						if (!RimThreaded.recipeThingDefValues.TryGetValue(recipe, out Dictionary<float, List<ThingDef>> thingDefValues))
						{
							thingDefValues = new Dictionary<float, List<ThingDef>>();
							RimThreaded.recipeThingDefValues[recipe] = thingDefValues;
						}
						float valuePerUnitOf = recipe.IngredientValueGetter.ValuePerUnitOf(thingDef);
						if (!thingDefValues.TryGetValue(valuePerUnitOf, out List<ThingDef> thingDefs))
						{
							thingDefs = new List<ThingDef>();
							thingDefValues[valuePerUnitOf] = thingDefs;
							valuesPerUnitOf.Add(valuePerUnitOf);
							valuesPerUnitOf.Sort();
						}
						thingDefs.Add(thingDef);
					}
				}
			}
		}

	}


}
