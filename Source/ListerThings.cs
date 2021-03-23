using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{    
    public class ListerThings_Patch
    {
		public static FieldRef<ListerThings, Dictionary<ThingDef, List<Thing>>> listsByDef =
			FieldRefAccess<ListerThings, Dictionary<ThingDef, List<Thing>>>("listsByDef");
		public static FieldRef<ListerThings, List<Thing>[]> listsByGroup =
					FieldRefAccess<ListerThings, List<Thing>[]>("listsByGroup");

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
			{
				return false;
			}

			List<Thing> newValue;
			lock (__instance)
			{
				if (!listsByDef(__instance).TryGetValue(thingDef, out List<Thing> value))
				{
					newValue = new List<Thing>()
					{
						t
					};
				}
				else
				{
					newValue = new List<Thing>(value)
					{
						t
					};
				}
                Dictionary<ThingDef, List<Thing>> newListsByDef = new Dictionary<ThingDef, List<Thing>>(listsByDef(__instance))
                {
                    [thingDef] = newValue
                };
                listsByDef(__instance) = newListsByDef;
			}

			
			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			foreach (ThingRequestGroup thingRequestGroup in allGroups)
			{
				if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(thingDef))
				{
					List<Thing> newThingList;
					lock (__instance)
					{
						List<Thing> list = listsByGroup(__instance)[(uint)thingRequestGroup];
						if (list == null)
						{
							newThingList = new List<Thing>() { t };
						}
						else
						{
							newThingList = new List<Thing>(list) { t };
						}
						listsByGroup(__instance)[(uint)thingRequestGroup] = newThingList;
					}
				}
			}
			return false;
		}

		public static bool Remove(ListerThings __instance, Thing t)
		{
			ThingDef thingDef = t.def;
			if (!ListerThings.EverListable(thingDef, __instance.use))
			{
				return false;
			}
			lock(__instance)
            {
                List<Thing> newListsByDef = new List<Thing>(listsByDef(__instance)[thingDef]);
				newListsByDef.Remove(t);
				listsByDef(__instance)[thingDef] = newListsByDef;
			}
			
			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			for (int i = 0; i < allGroups.Length; i++)
			{
				ThingRequestGroup group = allGroups[i];
				if ((__instance.use != ListerThingsUse.Region || group.StoreInRegion()) && group.Includes(thingDef))
				{
					lock (__instance)
					{
                        List<Thing> newListsByGroup = new List<Thing>(listsByGroup(__instance)[i]);
						newListsByGroup.Remove(t);
						listsByGroup(__instance)[i] = newListsByGroup;
					}
				}
			}
			return false;
		}

    }
    
}
