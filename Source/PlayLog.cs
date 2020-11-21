using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
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

	}
}
