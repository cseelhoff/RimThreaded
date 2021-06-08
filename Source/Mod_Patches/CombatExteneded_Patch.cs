using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class CombatExteneded_Patch
	{
		public static Type combatExtendedCE_Utility;
		public static Type combatExtendedVerb_LaunchProjectileCE;
		public static Type combatExtendedVerb_MeleeAttackCE;
		public static Type combatExtended_ProjectileCE;

		public static void Patch()
        {
			combatExtendedCE_Utility = TypeByName("CombatExtended.CE_Utility");
			combatExtendedVerb_LaunchProjectileCE = TypeByName("CombatExtended.Verb_LaunchProjectileCE");
			combatExtendedVerb_MeleeAttackCE = TypeByName("CombatExtended.Verb_MeleeAttackCE");
			combatExtended_ProjectileCE = TypeByName("CombatExtended.ProjectileCE");

			Type patched;
			if (combatExtendedCE_Utility != null)
			{
				string methodName = "BlitCrop";
				Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
				patched = typeof(CE_Utility_Transpile);
				Transpile(combatExtendedCE_Utility, patched, methodName);
				methodName = "GetColorSafe";
				Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
				Transpile(combatExtendedCE_Utility, patched, methodName);
			}

			//if (combatExtendedVerb_LaunchProjectileCE != null)
			//{
			//	string methodName = "CanHitFromCellIgnoringRange";
			//	patched = typeof(Verb_LaunchProjectileCE_Transpile);
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
			//	methodName = "TryFindCEShootLineFromTo";
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
			//}
			//if (combatExtendedVerb_MeleeAttackCE != null)
			//{
			//	string methodName = "TryCastShot";
			//	patched = typeof(Verb_MeleeAttackCE_Transpile);
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_MeleeAttackCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_MeleeAttackCE, patched, methodName);
			//}

		}
	}
}
