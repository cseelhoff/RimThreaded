using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class RegionListersUpdater_Patch
    {
		public static bool DeregisterInRegions(Thing thing, Map map)
		{
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return false;
			}
			List<Region> tmpRegions = new List<Region>();
			RegionListersUpdater.GetTouchableRegions(thing, map, tmpRegions, true);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				lock (listerThings)
				{
					if (listerThings.Contains(thing))
					{
						listerThings.Remove(thing);
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
				lock (listerThings)
				{
					if (!listerThings.Contains(thing))
					{
						listerThings.Add(thing);
					}
				}
			}
			return false;
		}
	}
}
