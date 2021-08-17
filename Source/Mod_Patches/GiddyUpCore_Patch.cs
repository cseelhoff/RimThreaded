using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class GiddyUpCore_Patch
    {
		//public static Type giddyUpCoreUtilitiesTextureUtility;
		public static Type giddyUpCoreStorageExtendedDataStorage;
		public static Type giddyUpCoreStorageExtendedPawnData;
		//public static Type giddyUpCoreJobsJobDriver_Mounted;
		//public static Type giddyUpCoreJobsGUC_JobDefOf;
		//public static Type giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob;
		public static void Patch()
		{
			giddyUpCoreStorageExtendedPawnData = TypeByName("GiddyUpCore.Storage.ExtendedPawnData");
			//giddyUpCoreJobsGUC_JobDefOf = TypeByName("GiddyUpCore.Jobs.GUC_JobDefOf");
			//giddyUpCoreUtilitiesTextureUtility = TypeByName("GiddyUpCore.Utilities.TextureUtility");
			giddyUpCoreStorageExtendedDataStorage = TypeByName("GiddyUpCore.Storage.ExtendedDataStorage");
            //giddyUpCoreJobsJobDriver_Mounted = TypeByName("GiddyUpCore.Jobs.JobDriver_Mounted");
            //giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob = TypeByName("GiddyUpCore.Harmony.Pawn_JobTracker_DetermineNextJob");

            Type patched = typeof(GiddyUpCore_Patch);
            //if (giddyUpCoreUtilitiesTextureUtility != null)
            //{
            //    string methodName = "setDrawOffset";
            //    Log.Message("RimThreaded is patching " + giddyUpCoreUtilitiesTextureUtility.FullName + " " + methodName);
            //    patched = typeof(TextureUtility_Transpile);
            //    Transpile(giddyUpCoreUtilitiesTextureUtility, patched, methodName);
            //}

            if (giddyUpCoreStorageExtendedDataStorage != null)
            {
                string methodName = "DeleteExtendedDataFor";
                Log.Message("RimThreaded is patching " + giddyUpCoreStorageExtendedDataStorage.FullName + " " + methodName);
                Transpile(giddyUpCoreStorageExtendedDataStorage, patched, methodName);

                methodName = "GetExtendedDataFor";
                Log.Message("RimThreaded is patching " + giddyUpCoreStorageExtendedDataStorage.FullName + " " + methodName);
                Transpile(giddyUpCoreStorageExtendedDataStorage, patched, methodName);
            }

            //if (giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob != null)
            //{
            //	string methodName = "Postfix";
            //	Log.Message("RimThreaded is patching " + giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob.FullName + " " + methodName);
            //	patched = typeof(Pawn_JobTracker_DetermineNextJob_Transpile);
            //	Transpile(giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob, patched, methodName);
            //}

            //if (giddyUpCoreJobsJobDriver_Mounted != null)
            //{
            //	string methodName = "<waitForRider>b__8_0";
            //	foreach (MethodInfo methodInfo in ((TypeInfo)giddyUpCoreJobsJobDriver_Mounted).DeclaredMethods)
            //	{
            //		if (methodInfo.Name.Equals(methodName))
            //		{
            //			Log.Message("RimThreaded is patching " + giddyUpCoreJobsJobDriver_Mounted.FullName + " " + methodName);
            //			patched = typeof(JobDriver_Mounted_Transpile);
            //			MethodInfo pMethod2 = patched.GetMethod("WaitForRider");
            //			harmony.Patch(methodInfo, transpiler: new HarmonyMethod(pMethod2));
            //		}
            //	}
            //}

        }
        public static IEnumerable<CodeInstruction> DeleteExtendedDataFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(int), giddyUpCoreStorageExtendedPawnData });
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(giddyUpCoreStorageExtendedDataStorage, "_store")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, Field(typeof(Thing), "thingIDNumber")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeDictionaryIntData, "Remove", new Type[] { typeof(int) })));
            searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in GetLockCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count, loadLockObjectInstructions, typeDictionaryIntData))
                    {
                        yield return codeInstruction;
                    }
                    currentInstructionIndex += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> GetExtendedDataFor(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type typeDictionaryIntData = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(int), giddyUpCoreStorageExtendedPawnData });
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(giddyUpCoreStorageExtendedDataStorage, "_store")),
            };
            List<CodeInstruction> searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_0));
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_2));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeDictionaryIntData, "set_Item", new Type[] { typeof(int) })));
            //searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
                {
                    matchFound = true;
                    foreach (CodeInstruction codeInstruction in GetLockCodeInstructions(
                        iLGenerator, instructionsList, currentInstructionIndex, searchInstructions.Count, loadLockObjectInstructions, typeDictionaryIntData))
                    {
                        yield return codeInstruction;
                    }
                    currentInstructionIndex += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
    }
}
