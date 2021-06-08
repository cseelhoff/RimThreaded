using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class AndroidTiers_Patch
    {

        public static Type androidTiers_GeneratePawns_Patch1;
        public static Type androidTiers_GeneratePawns_Patch;
        public static void Patch()
        {

			androidTiers_GeneratePawns_Patch1 = TypeByName("MOARANDROIDS.PawnGroupMakerUtility_Patch");
			if (androidTiers_GeneratePawns_Patch1 != null)
			{
				androidTiers_GeneratePawns_Patch = androidTiers_GeneratePawns_Patch1.GetNestedType("GeneratePawns_Patch");
			}
			Type patched;
			if (androidTiers_GeneratePawns_Patch != null)
			{
				string methodName = "Listener";
				patched = typeof(GeneratePawns_Patch_Transpile);
				Log.Message("RimThreaded is patching " + androidTiers_GeneratePawns_Patch.FullName + " " + methodName);
				Log.Message("Utility_Patch::Listener != null: " + (Method(androidTiers_GeneratePawns_Patch, "Listener") != null));
				Log.Message("Utility_Patch_Transpile::Listener != null: " + (Method(patched, "Listener") != null));
				Transpile(androidTiers_GeneratePawns_Patch, patched, methodName);
			}
		}

    }
}
