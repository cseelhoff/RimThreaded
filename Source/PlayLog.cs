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
		public static bool RemoveEntry(PlayLog __instance, LogEntry entry)
		{
			lock (entries(__instance))
			{
				entries(__instance).Remove(entry);
			}
			return false;
		}

        internal static void RunDestructivePatches()
		{
			Type original = typeof(PlayLog);
			Type patched = typeof(PlayLog_Patch);
			RimThreadedHarmony.Prefix(original, patched, "RemoveEntry");
		}
    }
}
