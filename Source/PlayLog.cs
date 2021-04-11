using System;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class PlayLog_Patch
    {
		public static FieldRef<PlayLog, List<LogEntry>> entries = 
			FieldRefAccess<PlayLog, List<LogEntry>>("entries");

		internal static void RunDestructivePatches()
		{
			Type original = typeof(PlayLog);
			Type patched = typeof(PlayLog_Patch);
			RimThreadedHarmony.Prefix(original, patched, "Add");
			RimThreadedHarmony.Prefix(original, patched, "RemoveEntry");
		}

		public static bool Add(PlayLog __instance, LogEntry entry)
		{
			lock (__instance)
			{
				List<LogEntry> newEntries = new List<LogEntry>(entries(__instance));
				newEntries.Insert(0, entry);
				while (newEntries.Count > 150)
				{
					RemoveEntry(__instance, newEntries[newEntries.Count - 1]);
				}
				entries(__instance) = newEntries;
			}
			return false;
		}

		public static bool RemoveEntry(PlayLog __instance, LogEntry entry)
		{
			lock (__instance)
			{
                List<LogEntry> newEntries = new List<LogEntry>(entries(__instance));
				newEntries.Remove(entry);
				entries(__instance) = newEntries;
			}
			return false;
		}

	}
}
