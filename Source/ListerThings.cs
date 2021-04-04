using RimWorld;
using System;
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

		public static void RunDestructivePatches()
		{
			Type original = typeof(ListerThings);
			Type patched = typeof(ListerThings_Patch);
			RimThreadedHarmony.Prefix(original, patched, "Remove");
			RimThreadedHarmony.Prefix(original, patched, "Add");
		}


		public static bool Add(ListerThings __instance, Thing t)		
		{
			ThingDef thingDef = t.def;
			if (!ListerThings.EverListable(thingDef, __instance.use))
			{
				return false;
			}

			lock (__instance)
			{
				if (!listsByDef(__instance).TryGetValue(thingDef, out List<Thing> value))
				{
					value = new List<Thing>();
					listsByDef(__instance).Add(t.def, value);
				} 
				value.Add(t);
			}

			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			foreach (ThingRequestGroup thingRequestGroup in allGroups)
			{
				if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(thingDef))
				{
					lock (__instance)
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
