using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{    
    public class ListerThings_Patch
    {
        public static void RunDestructivePatches()
		{
			Type original = typeof(ListerThings);
			Type patched = typeof(ListerThings_Patch);
			RimThreadedHarmony.Prefix(original, patched, "Remove");
			RimThreadedHarmony.Prefix(original, patched, "Add");
			//RimThreadedHarmony.Postfix(original, patched, nameof(ThingsMatching));
			//RimThreadedHarmony.Postfix(original, patched, nameof(get_AllThings));

		}
		public static void get_AllThings(ListerThings __instance, ref List<Thing> __result)
        {
			if (__result != null)
			{
				lock (__instance)
				{
					List<Thing> tmp = __result;
					__result = OneTickPool<List<Thing>>.Get();
					__result.Clear();
					__result.AddRange(tmp);
					//__result = new List<Thing>(__result);
				}
			}
		}
		public static void ThingsMatching(ListerThings __instance, ref List<Thing> __result, ThingRequest req)//this has to give a snapshot not just a reference -Sernior
        {
			if ( __result != null )
            {
                lock ( __instance )
                {
					List<Thing> tmp = __result;
					__result = OneTickPool<List<Thing>>.Get();
					__result.Clear();
					__result.AddRange(tmp);
					//__result = new List<Thing>(__result);
				}
            }
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
				if (!__instance.listsByDef.TryGetValue(thingDef, out List<Thing> value))
				{
					value = new List<Thing>();
					__instance.listsByDef.Add(t.def, value);
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
						List<Thing> list = __instance.listsByGroup[(uint)thingRequestGroup];
						if (list == null)
						{
							list = new List<Thing>();
							__instance.listsByGroup[(uint)thingRequestGroup] = list;
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
                List<Thing> newListsByDef = new List<Thing>(__instance.listsByDef[thingDef]);
				newListsByDef.Remove(t);
				__instance.listsByDef[thingDef] = newListsByDef;
			}
			
			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			for (int i = 0; i < allGroups.Length; i++)
			{
				ThingRequestGroup group = allGroups[i];
				if ((__instance.use != ListerThingsUse.Region || group.StoreInRegion()) && group.Includes(thingDef))
				{
					lock (__instance)
					{
                        List<Thing> newListsByGroup = new List<Thing>(__instance.listsByGroup[i]);
						newListsByGroup.Remove(t);
						__instance.listsByGroup[i] = newListsByGroup;
					}
				}
			}
			return false;
		}

    }
    
}
