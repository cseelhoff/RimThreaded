using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class PlayLog_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(PlayLog);
            Type patched = typeof(PlayLog_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Add");
            RimThreadedHarmony.Prefix(original, patched, "RemoveEntry");
            //RimThreadedHarmony.Prefix(original, patched, "AnyEntryConcerns");
        }

        public static bool Add(PlayLog __instance, LogEntry entry)
        {
            lock (__instance)
            {
                List<LogEntry> newEntries = new List<LogEntry>(__instance.entries);
                newEntries.Insert(0, entry);
                while (newEntries.Count > 150)
                {
                    newEntries.RemoveAt(newEntries.Count - 1);
                }
                __instance.entries = newEntries;
            }
            return false;
        }

        public static bool RemoveEntry(PlayLog __instance, LogEntry entry)
        {
            lock (__instance)
            {
                List<LogEntry> newEntries = new List<LogEntry>(__instance.entries);
                newEntries.Remove(entry);
                __instance.entries = newEntries;
            }
            return false;
        }
        /*
		public static bool AnyEntryConcerns(PlayLog __instance, Pawn p, ref bool __result)
		{
            List<LogEntry> snapshotEntries = __instance.entries;
			for (int i = 0; i < snapshotEntries.Count; i++)
			{
				if (snapshotEntries[i].Concerns(p))
				{
					__result = true;
					return false;
				}
			}
			__result = false;
			return false;
		}
		*/
    }
}
