using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class PawnRules_Patch
	{
		public static Type pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus;
		public static void Patch()
		{

			pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus = TypeByName("PawnRules.Patch.RimWorld_Pawn_GuestTracker_SetGuestStatus");
			Type patched;
			if (pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus != null)
			{
				string methodName = "Prefix";
				Log.Message("RimThreaded is patching " + pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus.FullName + " " + methodName);
				patched = typeof(RimWorld_Pawn_GuestTracker_SetGuestStatus_Transpile);
				Transpile(pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus, patched, methodName);
			}
		}
    }
}
