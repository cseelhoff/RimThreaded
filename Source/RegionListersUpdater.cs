using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using static Verse.RegionListersUpdater;

namespace RimThreaded
{
    public class RegionListersUpdater_Patch
    {
		//public static Dictionary<int, List<Region>> tmpRegionsLists = new Dictionary<int, List<Region>>();
		[ThreadStatic]
		public static List<Region> tmpRegions;
		public static bool DeregisterInRegions(Thing thing, Map map)
		{
			//FIND and REPLACE ALL (RegionListersUpdater.tmpRegions with RegionListersUpdater_Patch.tmpRegions)
			//---START ADD---
			if (tmpRegions==null)
            {
				tmpRegions = new List<Region>();
			}
			//---END ADD---
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return false;
			}

			GetTouchableRegions(thing, map, tmpRegions, true);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if (listerThings.Contains(thing))
				{
					//---START REMOVE---
					//listerThings.Remove(thing);
					//---END REMOVE---

					//---START ADD---
					lockAndRemove(listerThings, thing);
					//---END ADD---
				}
			}
			tmpRegions.Clear();
			return false;
		}
		public static void lockAndRemove(ListerThings listerThings, Thing thing)
		{
			lock (listerThings)
			{
				if (listerThings.Contains(thing))
				{
					listerThings.Remove(thing);
				}
			}
		}

		public static void lockAndAdd(ListerThings listerThings, Thing thing)
		{
			lock (listerThings)
			{
				if (listerThings.Contains(thing))
				{
					listerThings.Add(thing);
				}
			}
		}

		public static bool RegisterInRegions(Thing thing, Map map)
		{
			//FIND and REPLACE ALL (RegionListersUpdater.tmpRegions with RegionListersUpdater_Patch.tmpRegions)
			//---START ADD---
			if (tmpRegions == null)
			{
				tmpRegions = new List<Region>();
			}
			//---END ADD---
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return false;
			}

			//FIND and REPLACE ALL (RegionListersUpdater.tmpRegions with RegionListersUpdater_Patch.tmpRegions)
			GetTouchableRegions(thing, map, tmpRegions, false);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if (!listerThings.Contains(thing))
				{
					//---START REMOVE---
					//listerThings.Add(thing);
					//---END REMOVE---

					//---START ADD---
					lockAndAdd(listerThings, thing);
					//---END ADD---
				}
			}
			tmpRegions.Clear();
			return false;
		}
		public static bool RegisterAllAt(IntVec3 c, Map map, HashSet<Thing> processedThings = null)
		{
			List<Thing> thingList = c.GetThingList(map);
			int count = thingList.Count;
			for (int i = 0; i < count; i++)
			{
				Thing thing;
				//ADD TRY CATCH around "thing = thingList[i];"
				try
				{
					thing = thingList[i];
				} catch(ArgumentOutOfRangeException)
                {
					break;
                }
				if (processedThings == null || processedThings.Add(thing))
				{
					RegisterInRegions(thing, map);
				}
			}
			return false;
		}
	}
}