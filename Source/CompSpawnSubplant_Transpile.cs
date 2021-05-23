using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class CompSpawnSubplant_Transpile
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(CompSpawnSubplant);
            Type patched = typeof(CompSpawnSubplant_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "DoGrowSubplant");
		}
		internal static void RunDestructivePatches()
        {
            Type original = typeof(CompSpawnSubplant);
            Type patched = typeof(CompSpawnSubplant_Transpile);
            RimThreadedHarmony.Prefix(original, patched, "AddProgress_NewTmp"); //could use interlock instead
			RimThreadedHarmony.Prefix(original, patched, "TryGrowSubplants"); //could use interlock instead
		}
		
        public static bool AddProgress_NewTmp(CompSpawnSubplant __instance, float progress, bool ignoreMultiplier = false)
        {
            if (!ModLister.RoyaltyInstalled)
            {
                Log.ErrorOnce("Subplant spawners are a Royalty-specific game system. If you want to use this code please check ModLister.RoyaltyInstalled before calling it. See rules on the Ludeon forum for more info.", 43254);
            }
            else
            {
                if (!ignoreMultiplier)
                    progress *= __instance.ProgressMultiplier;
                lock (__instance)
                {
                    __instance.progressToNextSubplant += progress;
                }

                ++__instance.meditationTicksToday;
                __instance.TryGrowSubplants();
            }

            return false;
        }

		public static bool TryGrowSubplants(CompSpawnSubplant __instance)
        {
            lock (__instance)
            {
                for (; __instance.progressToNextSubplant >= 1.0; --__instance.progressToNextSubplant)
                    __instance.DoGrowSubplant();
            }

            return false;
        }
		public static IEnumerable<CodeInstruction> DoGrowSubplant(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			Type loadLockObjectType = typeof(List<Thing>);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompSpawnSubplant), "subplants"))
			};
			List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompSpawnSubplant), "get_Props")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompProperties_SpawnSubplant), "subplant")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_2));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), "parent")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Map")));
			searchInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
			searchInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenSpawn), "Spawn", new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) })));
			searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(loadLockObjectType, "Add")));

			int i = 0;
			int matchesFound = 0;

			while (i < instructionsList.Count)
			{
				if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
				{
					matchesFound++;
					foreach (CodeInstruction codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
						iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions, loadLockObjectType))
					{
						yield return codeInstruction;
					}
					i += searchInstructions.Count;
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			if (matchesFound < 1)
			{
				Log.Error("IL code instructions not found");
			}
		}

    }
}
