using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;
using static RimThreaded.RimThreadedHarmony;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    
    public class Thing_Patch
	{
		internal static void RunNonDestructivePatches()
		{
			Type original = typeof(Thing);
			Type patched = typeof(Thing_Patch);
			RimThreadedHarmony.Postfix(original, patched, "SpawnSetup", "SpawnSetupPostFix");
			RimThreadedHarmony.Transpile(original, patched, "SpawnSetup");
			RimThreadedHarmony.Transpile(original, patched, "DeSpawn");
		}
		public static IEnumerable<CodeInstruction> SpawnSetup(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			//---START EDIT---
			int[] matchesFound = new int[1];
			Type listerThings = typeof(ListerThings);
			//---END EDIT---
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
						//---START EDIT---
						i + 3 < instructionsList.Count &&
						instructionsList[i + 3].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i + 3].operand == Method(listerThings, "Add")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, Field(typeof(Map), "listerThings"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(listerThings);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
								//---START EDIT---
								instructionsList[i - 1].opcode == OpCodes.Callvirt &&
								(MethodInfo)instructionsList[i - 1].operand == Method(listerThings, "Add")
						//---END EDIT---
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList, ref i))
						yield return ci;
					matchesFound[matchIndex]++;
					continue;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}
		public static IEnumerable<CodeInstruction> DeSpawn(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			//---START EDIT---
			int[] matchesFound = new int[2];
			Type listerThings = typeof(ListerThings);
			//---END EDIT---
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
						//---START EDIT---
						i + 3 < instructionsList.Count &&
						instructionsList[i + 3].opcode == OpCodes.Callvirt &&
						(MethodInfo)instructionsList[i + 3].operand == Method(listerThings, "Remove")
					//---END EDIT---
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
					//---START EDIT---
						new CodeInstruction(OpCodes.Ldloc_0),
						new CodeInstruction(OpCodes.Ldfld, Field(typeof(Map), "listerThings"))
					//---END EDIT---
					};

					//---START EDIT---
					LocalBuilder lockObject = iLGenerator.DeclareLocal(listerThings);
					//---END EDIT---

					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
							//---START EDIT---
							instructionsList[i - 1].opcode == OpCodes.Callvirt &&
							(MethodInfo)instructionsList[i - 1].operand == Method(listerThings, "Remove")
						//---END EDIT---
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList, ref i))
						yield return ci;
					matchesFound[matchIndex]++;
					continue;
				}
				matchIndex++;
				if (
					//---START EDIT---
					i - 3 > 0 &&
					instructionsList[i - 3].opcode == OpCodes.Ldstr &&
					instructionsList[i - 3].operand.ToString().Equals(" which is already destroyed.")
					//---END EDIT---
					)
				{
					instructionsList[i].operand = Method(typeof(Log), "Warning");
					yield return instructionsList[i++];
					matchesFound[matchIndex]++;
					continue;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}



#pragma warning disable IDE0060 // Remove unused parameter
		public static void SpawnSetupPostFix(Thing __instance, Map map, bool respawningAfterLoad)
#pragma warning restore IDE0060 // Remove unused parameter
        {
			ThingDef thingDef = __instance.def;
			if (!RimThreaded.recipeThingDefs.Contains(thingDef))
			{
				lock (RimThreaded.recipeThingDefs)
				{
					//Log.Message("RimThreaded is building new recipe caches for: " + thingDef.ToString());
					RimThreaded.recipeThingDefs.Add(thingDef);
					foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
					{
						if (!RimThreaded.sortedRecipeValues.TryGetValue(recipe, out List<float> valuesPerUnitOf))
						{
							valuesPerUnitOf = new List<float>();
							RimThreaded.sortedRecipeValues[recipe] = valuesPerUnitOf;
						}
						if (!RimThreaded.recipeThingDefValues.TryGetValue(recipe, out Dictionary<float, List<ThingDef>> thingDefValues))
						{
							thingDefValues = new Dictionary<float, List<ThingDef>>();
							RimThreaded.recipeThingDefValues[recipe] = thingDefValues;
						}
						float valuePerUnitOf = recipe.IngredientValueGetter.ValuePerUnitOf(thingDef);
						if (!thingDefValues.TryGetValue(valuePerUnitOf, out List<ThingDef> thingDefs))
						{
							thingDefs = new List<ThingDef>();
							thingDefValues[valuePerUnitOf] = thingDefs;
							valuesPerUnitOf.Add(valuePerUnitOf);
							valuesPerUnitOf.Sort();
						}
						thingDefs.Add(thingDef);
					}
				}
			}
		}

	}


}
