using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{    
    public class ListerThings_Patch
    {
		public static AccessTools.FieldRef<ListerThings, Dictionary<ThingDef, List<Thing>>> listsByDef =
			AccessTools.FieldRefAccess<ListerThings, Dictionary<ThingDef, List<Thing>>>("listsByDef");
		public static AccessTools.FieldRef<ListerThings, List<Thing>[]> listsByGroup =
					AccessTools.FieldRefAccess<ListerThings, List<Thing>[]>("listsByGroup");

		private static readonly List<Thing> EmptyList = new List<Thing>();

		public static bool ThingsOfDef(ListerThings __instance, ref List<Thing> __result, ThingDef def)
		{
			__result = EmptyList;
			if(def != null)
				__result = __instance.ThingsMatching(ThingRequest.ForDef(def));
			return false;
		}

		public static bool Add(ListerThings __instance, Thing t)
		{
			ThingDef thingDef = t.def;
			if (!ListerThings.EverListable(thingDef, __instance.use))
				return false;
			List<Thing> thingList1;
			lock (__instance)
			{
				if (!listsByDef(__instance).TryGetValue(thingDef, out thingList1))
				{
					thingList1 = new List<Thing>();
					listsByDef(__instance).Add(thingDef, thingList1);
					if (!RimThreaded.recipeThingDefs.Contains(thingDef))
					{
						lock (RimThreaded.recipeThingDefs)
						{
							Log.Message("RimThreaded is building new recipe caches for: " + thingDef.ToString());
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
				thingList1.Add(t);
			}
			

			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			foreach (ThingRequestGroup thingRequestGroup in allGroups)
			{
				if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(thingDef))
				{
					List<Thing> list = listsByGroup(__instance)[(uint)thingRequestGroup];
					if (list == null)
					{
						list = new List<Thing>();
						listsByGroup(__instance)[(uint)thingRequestGroup] = list;
					}

					list.Add(t);
				}
			}
			return false;
		}

		public static bool Remove(ListerThings __instance, Thing t)
        {
			if (!ListerThings.EverListable(t.def, __instance.use))
				return false;
            List<Thing> ld = listsByDef(__instance)[t.def];
			lock (ld) //ADDED
			{
				ld.Remove(t);
			}

			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			for (int index = 0; index < allGroups.Length; ++index)
			{
				ThingRequestGroup group = allGroups[index];
				if ((__instance.use != ListerThingsUse.Region || group.StoreInRegion()) && group.Includes(t.def))
                {
                    List<Thing> tl = listsByGroup(__instance)[index];
					lock (tl) //ADDED
					{
						int li = tl.LastIndexOf(t);
						if (li > -1)
							tl.RemoveAt(li);
					}
				}
						
			}			
			return false;
		}

    }
    
}
