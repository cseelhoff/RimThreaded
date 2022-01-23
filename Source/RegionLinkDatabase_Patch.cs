using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class RegionLinkDatabase_Patch
    {
		public static void RunDestructivePatches()
		{
			Type original = typeof(RegionLinkDatabase);
			Type patched = typeof(RegionLinkDatabase_Patch);
			RimThreadedHarmony.Prefix(original, patched, nameof(LinkFrom));
			RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LinkHasNoRegions));
		}

		public static bool LinkFrom(RegionLinkDatabase __instance, ref RegionLink __result, EdgeSpan span)
		{
			ulong key = span.UniqueHashCode();
			RegionLink value;
			Dictionary<ulong, RegionLink> links = __instance.links;
			lock (links)
			{
				if (!links.TryGetValue(key, out value))
				{
					value = new RegionLink();
					value.span = span;
					links.Add(key, value);
				}
			}
			__result = value;
			return false;
		}
		public static bool Notify_LinkHasNoRegions(RegionLinkDatabase __instance, RegionLink link)
		{
			Dictionary<ulong, RegionLink> links = __instance.links;
			lock (links)
			{
				links.Remove(link.UniqueHashCode());
			}
			return false;
		}
	}
}
