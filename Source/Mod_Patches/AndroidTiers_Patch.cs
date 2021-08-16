using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
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
#if RW13
			Type androidTiers_Utils = TypeByName("MOARANDROIDS.Utils");
			if (androidTiers_Utils != null)
			{
				string methodName = nameof(getCachedCSM);
				Log.Message("RimThreaded is patching " + androidTiers_Utils.FullName + " " + methodName);
				Transpile(androidTiers_Utils, typeof(AndroidTiers_Patch), methodName);
			}
#endif
		}
		public static void set_Item(Dictionary<Thing, object> CSM, Thing t, object j)
		{
			lock (CSM)
			{
				CSM[t] = j;
			}
		}
		public static IEnumerable<CodeInstruction> getCachedCSM(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			Type ThingToCompSkyMind = typeof(Dictionary<,>).MakeGenericType(new[] { typeof(Thing), TypeByName("MOARANDROIDS.CompSkyMind") });
			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Callvirt)
				{//CompSkyMind
					if ((MethodInfo)i.operand == Method(ThingToCompSkyMind, "set_Item"))
					{
						i.operand = Method(typeof(AndroidTiers_Patch), nameof(set_Item));
					}
				}
				yield return i;
			}
		}

	}
}
