using HarmonyLib;
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

		public static bool Add(ListerThings __instance, Thing t)
		{
			if (!ListerThings.EverListable(t.def, __instance.use))
				return false;
			List<Thing> thingList1;
			if (!listsByDef(__instance).TryGetValue(t.def, out thingList1))
			{
				thingList1 = new List<Thing>();
				lock (listsByDef(__instance)) //ADDED
				{
					listsByDef(__instance).Add(t.def, thingList1);
				}
			}
			lock (thingList1) //ADDED
			{
				thingList1.Add(t);
			}
			foreach (ThingRequestGroup allGroup in ThingListGroupHelper.AllGroups)
			{
				if ((__instance.use != ListerThingsUse.Region || allGroup.StoreInRegion()) && allGroup.Includes(t.def))
				{
					List<Thing> thingList2 = listsByGroup(__instance)[(int)allGroup];
					if (thingList2 == null)
					{
						thingList2 = new List<Thing>();
						listsByGroup(__instance)[(int)allGroup] = thingList2;
					}
					lock (thingList2) //ADDED
					{
						thingList2.Add(t);
					}
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
