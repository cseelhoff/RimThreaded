using System;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class RegionListersUpdater_Patch
    {

		public static Dictionary<int, List<Region>> tmpRegionsLists = new Dictionary<int, List<Region>>();

		public static bool DeregisterInRegions(Thing thing, Map map)
		{
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return false;
			}
			//List<Region> tmpRegions = new List<Region>();
			List<Region> tmpRegions = tmpRegionsLists[Thread.CurrentThread.ManagedThreadId];
			tmpRegions.Clear();
			RegionListersUpdater.GetTouchableRegions(thing, map, tmpRegions, true);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				List<Thing> allThings = listerThings.AllThings;
				for (int j = allThings.Count - 1; j >= 0; j--)
				{
					Thing thing2;
					try
					{
						thing2 = allThings[j];
					}
					catch(ArgumentOutOfRangeException)
					{
						break;
					}
					if (thing == thing2)
					{
						lock (listerThings)
						{
							//if (j < allThings.Count && allThings[j] == thing)
							//{
								//allThings.RemoveAt(j);
								listerThings.Remove(thing);
							//} else
                            //{
								//Log.Warning("Thing " + thing.ToString() + " was not at expected list index when attempting to remove.");
                            //}
						}
					}				
				}
			}
			return false;
		}

		public static bool RegisterInRegions(Thing thing, Map map)
		{
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return false;
			}
			List<Region> tmpRegions = new List<Region>();
			RegionListersUpdater.GetTouchableRegions(thing, map, tmpRegions, false);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				List<Thing> allThings = listerThings.AllThings;
				bool matchFound = false;
				for (int j = allThings.Count - 1; j >= 0; j--)
				{
					Thing thing2;
					try
					{
						thing2 = allThings[j];
					}
					catch (ArgumentOutOfRangeException)
					{
						break;
					}
					if (thing == thing2)
					{
						matchFound = true;
						break;
					}
				}
				if (!matchFound)
				{
					lock (listerThings)
					{
						listerThings.Add(thing);
					}
				}				
			}
			return false;
		}
	}
}
