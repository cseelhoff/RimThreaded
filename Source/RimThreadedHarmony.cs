using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using RimWorld.Planet;
using System.Reflection.Emit;
using System.Threading;
using Verse.Grammar;
using Verse.AI.Group;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

	[StaticConstructorOnStartup]
	public class RimThreadedHarmony
	{
		public static Harmony harmony = new Harmony("majorhoff.rimthreaded");
		public static Type giddyUpCoreUtilitiesTextureUtility;
		public static Type giddyUpCoreStorageExtendedDataStorage;
		public static Type giddyUpCoreStorageExtendedPawnData;
		public static Type giddyUpCoreJobsJobDriver_Mounted;
		public static Type giddyUpCoreJobsGUC_JobDefOf;
		public static Type hospitalityCompUtility;
		public static Type hospitalityCompGuest;
		public static Type giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob;
		public static Type awesomeInventoryJobsJobGiver_FindItemByRadius;
		public static Type awesomeInventoryErrorMessage;
		public static Type jobGiver_AwesomeInventory_TakeArm;
		public static Type awesomeInventoryJobsJobGiver_FindItemByRadiusSub;
		public static Type pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus;
		public static Type combatExtendedCE_Utility;
		public static Type combatExtendedVerb_LaunchProjectileCE;
		public static Type combatExtendedVerb_MeleeAttackCE;
		public static Type combatExtended_ProjectileCE;
		public static Type dubsSkylight_Patch_GetRoof;
		public static Type jobsOfOpportunityJobsOfOpportunity_Hauling;
		public static Type jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob;
		public static Type childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch;
		public static Type androidTiers_GeneratePawns_Patch1;
		public static Type androidTiers_GeneratePawns_Patch;
		public static FieldInfo cachedStoreCell;
		public static HashSet<MethodInfo> nonDestructivePrefixes = new HashSet<MethodInfo>();

		public static List<CodeInstruction> EnterLock(LocalBuilder lockObject, LocalBuilder lockTaken, List<CodeInstruction> loadLockObjectInstructions, List<CodeInstruction> instructionsList, ref int currentInstructionIndex)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			loadLockObjectInstructions[0].labels = instructionsList[currentInstructionIndex].labels;
			for (int i = 0; i < loadLockObjectInstructions.Count; i++)
			{
				codeInstructions.Add(loadLockObjectInstructions[i]);
			}
			instructionsList[currentInstructionIndex].labels = new List<Label>();
			codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
			codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			codeInstructions.Add(codeInstruction);
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() })));
			return codeInstructions;
		}
		public static List<CodeInstruction> ExitLock(ILGenerator iLGenerator, LocalBuilder lockObject, LocalBuilder lockTaken, List<CodeInstruction> instructionsList, ref int currentInstructionIndex)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			Label endHandlerDestination = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
			codeInstructions.Add(codeInstruction);
			Label endFinallyDestination = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
			codeInstruction = new CodeInstruction(OpCodes.Endfinally);
			codeInstruction.labels.Add(endFinallyDestination);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			codeInstructions.Add(codeInstruction);
			instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
			return codeInstructions;
		}

		public static List<CodeInstruction> GetLockCodeInstructions(
			ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
			int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions,
			LocalBuilder lockObject, LocalBuilder lockTaken)
		{
			List<CodeInstruction> finalCodeInstructions = new List<CodeInstruction>();
			loadLockObjectInstructions[0].labels = instructionsList[currentInstructionIndex].labels;
			for (int i = 0; i < loadLockObjectInstructions.Count; i++)
			{
				finalCodeInstructions.Add(loadLockObjectInstructions[i]);
			}
			instructionsList[currentInstructionIndex].labels = new List<Label>();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			finalCodeInstructions.Add(codeInstruction);
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() })));
			for (int i = 0; i < searchInstructionsCount; i++)
			{
				finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
				currentInstructionIndex++;
			}
			Label endHandlerDestination = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
			codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
			finalCodeInstructions.Add(codeInstruction);
			Label endFinallyDestination = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
			codeInstruction = new CodeInstruction(OpCodes.Endfinally);
			codeInstruction.labels.Add(endFinallyDestination);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			finalCodeInstructions.Add(codeInstruction);
			instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
			return finalCodeInstructions;
		}
		public static List<CodeInstruction> GetLockCodeInstructions(
			ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
			int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions, Type lockObjectType)
		{
			LocalBuilder lockObject = iLGenerator.DeclareLocal(lockObjectType);
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			return GetLockCodeInstructions(iLGenerator, instructionsList, currentInstructionIndex,
				searchInstructionsCount, loadLockObjectInstructions, lockObject, lockTaken);
		}

		public static bool IsCodeInstructionsMatching(List<CodeInstruction> searchInstructions, List<CodeInstruction> instructionsList, int instructionIndex)
		{
			bool instructionsMatch = false;
			if (instructionIndex + searchInstructions.Count < instructionsList.Count)
			{
				instructionsMatch = true;
				for (int searchIndex = 0; searchIndex < searchInstructions.Count; searchIndex++)
				{
					CodeInstruction searchInstruction = searchInstructions[searchIndex];
					CodeInstruction originalInstruction = instructionsList[instructionIndex + searchIndex];
					object searchOperand = searchInstruction.operand;
					object orginalOperand = originalInstruction.operand;
					if (searchInstruction.opcode != originalInstruction.opcode)
					{
						instructionsMatch = false;
						break;
					}
					else
					{
						if (orginalOperand != null &&
							searchOperand != null &&
							orginalOperand != searchOperand)
						{
							if (orginalOperand.GetType() != typeof(LocalBuilder))
							{
								instructionsMatch = false;
								break;
							}
							else
							{
								if (((LocalBuilder)orginalOperand).LocalIndex != (int)searchOperand)
								{
									instructionsMatch = false;
									break;
								}
							}
						}
					}
				}
			}
			return instructionsMatch;
		}
		public static void AddBreakDestination(List<CodeInstruction> instructionsList, int currentInstructionIndex, Label breakDestination)
		{
			//Since we are going to break inside of some kind of loop, we need to find out where to jump/break to
			//The destination should be one line after the closing bracket of the loop when the exception/break occurs			
			HashSet<Label> labels = new HashSet<Label>();

			//gather all labels that exist at or above currentInstructionIndex. the start of our loop is going to be one of these...
			for (int i = 0; i <= currentInstructionIndex; i++)
			{
				foreach (Label label in instructionsList[i].labels)
				{
					labels.Add(label);
				}
			}

			//find first branch that jumps to label above currentInstructionIndex. the first branch opcode found is likely the closing bracket
			for (int i = currentInstructionIndex + 1; i < instructionsList.Count; i++)
			{
				if (instructionsList[i].operand is Label label)
				{
					if (labels.Contains(label))
					{
						instructionsList[i + 1].labels.Add(breakDestination);
						break;
					}
				}
			}
		}

		public static void StartTryAndAddBreakDestinationLabel(List<CodeInstruction> instructionsList, ref int currentInstructionIndex, Label breakDestination)
        {
			AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
			instructionsList[currentInstructionIndex].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
		}

		public static List<CodeInstruction> EndTryStartCatchArgumentExceptionOutOfRange(List<CodeInstruction> instructionsList, ref int currentInstructionIndex, ILGenerator iLGenerator, Label breakDestination)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			Label handlerEnd = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
			CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
			pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
			codeInstructions.Add(pop);
			CodeInstruction leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
			leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			codeInstructions.Add(leaveLoopEnd);
			instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
			return codeInstructions;
		}

		public static List<CodeInstruction> UpdateTryCatchCodeInstructions(ILGenerator iLGenerator,
			List<CodeInstruction> instructionsList, int currentInstructionIndex, int searchInstructionsCount)
		{
			Label breakDestination = iLGenerator.DefineLabel();
			AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
			List<CodeInstruction> finalCodeInstructions = new List<CodeInstruction>();
			instructionsList[currentInstructionIndex].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			for (int i = 0; i < searchInstructionsCount; i++)
			{
				finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
				currentInstructionIndex++;
			}
			Label handlerEnd = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
			CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
			pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
			finalCodeInstructions.Add(pop);
			CodeInstruction leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
			leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			finalCodeInstructions.Add(leaveLoopEnd);
			instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
			return finalCodeInstructions;
		}

		public static readonly Dictionary<Type, Type> threadStaticPatches = new Dictionary<Type, Type>();

		public static IEnumerable<CodeInstruction> ReplaceThreadStatics(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				if (instructionsList[i].operand != null) {
					if(instructionsList[i].operand is FieldInfo fieldInfo) { 
						if (threadStaticPatches.TryGetValue(fieldInfo.DeclaringType, out Type patchedType)) {
							FieldInfo[] fields = patchedType.GetFields();
							foreach (FieldInfo field in fields)
							{
								if (fieldInfo.Name.Equals(field.Name) && fieldInfo.FieldType == field.FieldType)
                                {
									//Log.Message("RimThreaded is replacing field: " + fieldInfo.DeclaringType.ToString() + "." + fieldInfo.Name + " with field: " + field.DeclaringType.ToString() + "." + field.Name);
									instructionsList[i].operand = field;
									break;
								}
							}
						}
					}
				}
				yield return instructionsList[i++];
			}
		}


		static RimThreadedHarmony()
		{
			Harmony.DEBUG = false;
			Log.Message("RimThreaded " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "  is patching methods...");

			PatchDestructiveFixes();
			PatchNonDestructiveFixes();
			PatchModCompatibility();

			Log.Message("RimThreaded patching is complete.");
		}

        private static void PatchModCompatibility()
        {
			Type patched = null;

			giddyUpCoreStorageExtendedPawnData = TypeByName("GiddyUpCore.Storage.ExtendedPawnData");
			giddyUpCoreJobsGUC_JobDefOf = TypeByName("GiddyUpCore.Jobs.GUC_JobDefOf");
			giddyUpCoreUtilitiesTextureUtility = TypeByName("GiddyUpCore.Utilities.TextureUtility");
			giddyUpCoreStorageExtendedDataStorage = TypeByName("GiddyUpCore.Storage.ExtendedDataStorage");
			giddyUpCoreJobsJobDriver_Mounted = TypeByName("GiddyUpCore.Jobs.JobDriver_Mounted");
			giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob = TypeByName("GiddyUpCore.Harmony.Pawn_JobTracker_DetermineNextJob");
			hospitalityCompUtility = TypeByName("Hospitality.CompUtility");
			hospitalityCompGuest = TypeByName("Hospitality.CompGuest");
			awesomeInventoryJobsJobGiver_FindItemByRadius = TypeByName("AwesomeInventory.Jobs.JobGiver_FindItemByRadius");
			awesomeInventoryErrorMessage = TypeByName("AwesomeInventory.ErrorMessage");
			jobGiver_AwesomeInventory_TakeArm = TypeByName("AwesomeInventory.Jobs.JobGiver_AwesomeInventory_TakeArm");
			awesomeInventoryJobsJobGiver_FindItemByRadiusSub = TypeByName("AwesomeInventory.Jobs.JobGiver_FindItemByRadius+<>c__DisplayClass17_0");
			pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus = TypeByName("PawnRules.Patch.RimWorld_Pawn_GuestTracker_SetGuestStatus");
			combatExtendedCE_Utility = TypeByName("CombatExtended.CE_Utility");
			combatExtendedVerb_LaunchProjectileCE = TypeByName("CombatExtended.Verb_LaunchProjectileCE");
			combatExtendedVerb_MeleeAttackCE = TypeByName("CombatExtended.Verb_MeleeAttackCE");
			combatExtended_ProjectileCE = TypeByName("CombatExtended.ProjectileCE");
			dubsSkylight_Patch_GetRoof = TypeByName("Dubs_Skylight.Patch_GetRoof");
			jobsOfOpportunityJobsOfOpportunity_Hauling = TypeByName("JobsOfOpportunity.JobsOfOpportunity+Hauling");
			jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob = TypeByName("JobsOfOpportunity.JobsOfOpportunity+Patch_TryOpportunisticJob");
			childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch = TypeByName("Children.ChildrenHarmony+HediffComp_Discoverable_CheckDiscovered_Patch");
			androidTiers_GeneratePawns_Patch1 = TypeByName("MOARANDROIDS.PawnGroupMakerUtility_Patch");
			if (androidTiers_GeneratePawns_Patch1 != null)
			{
				androidTiers_GeneratePawns_Patch = androidTiers_GeneratePawns_Patch1.GetNestedType("GeneratePawns_Patch");
			}

			if (giddyUpCoreUtilitiesTextureUtility != null)
			{
				string methodName = "setDrawOffset";
				Log.Message("RimThreaded is patching " + giddyUpCoreUtilitiesTextureUtility.FullName + " " + methodName);
				patched = typeof(TextureUtility_Transpile);
				Transpile(giddyUpCoreUtilitiesTextureUtility, patched, methodName);
			}

			if (giddyUpCoreStorageExtendedDataStorage != null)
			{
				string methodName = "DeleteExtendedDataFor";
				Log.Message("RimThreaded is patching " + giddyUpCoreStorageExtendedDataStorage.FullName + " " + methodName);
				patched = typeof(ExtendedDataStorage_Transpile);
				Transpile(giddyUpCoreStorageExtendedDataStorage, patched, methodName);

				methodName = "GetExtendedDataFor";
				Log.Message("RimThreaded is patching " + giddyUpCoreStorageExtendedDataStorage.FullName + " " + methodName);
				Transpile(giddyUpCoreStorageExtendedDataStorage, patched, methodName);
			}

			if (giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob != null)
			{
				string methodName = "Postfix";
				Log.Message("RimThreaded is patching " + giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob.FullName + " " + methodName);
				patched = typeof(Pawn_JobTracker_DetermineNextJob_Transpile);
				Transpile(giddyUpCoreHarmonyPawnJobTracker_DetermineNextJob, patched, methodName);
			}

			if (giddyUpCoreJobsJobDriver_Mounted != null)
			{
				string methodName = "<waitForRider>b__8_0";
				foreach (MethodInfo methodInfo in ((TypeInfo)giddyUpCoreJobsJobDriver_Mounted).DeclaredMethods)
				{
					if (methodInfo.Name.Equals(methodName))
					{
						Log.Message("RimThreaded is patching " + giddyUpCoreJobsJobDriver_Mounted.FullName + " " + methodName);
						patched = typeof(JobDriver_Mounted_Transpile);
						MethodInfo pMethod2 = patched.GetMethod("WaitForRider");
						harmony.Patch(methodInfo, transpiler: new HarmonyMethod(pMethod2));
					}
				}
			}

			if (hospitalityCompUtility != null)
			{
				string methodName = "CompGuest";
				Log.Message("RimThreaded is patching " + hospitalityCompUtility.FullName + " " + methodName);
				patched = typeof(CompUtility_Transpile);
				Transpile(hospitalityCompUtility, patched, methodName);
				methodName = "OnPawnRemoved";
				Log.Message("RimThreaded is patching " + hospitalityCompUtility.FullName + " " + methodName);
				Transpile(hospitalityCompUtility, patched, methodName);
			}

			if (awesomeInventoryJobsJobGiver_FindItemByRadius != null)
			{
				string methodName = "Reset";
				Log.Message("RimThreaded is patching " + awesomeInventoryJobsJobGiver_FindItemByRadius.FullName + " " + methodName);
				patched = typeof(JobGiver_FindItemByRadius_Transpile);
				Transpile(awesomeInventoryJobsJobGiver_FindItemByRadius, patched, methodName);
				methodName = "FindItem";
				Log.Message("RimThreaded is patching " + awesomeInventoryJobsJobGiver_FindItemByRadius.FullName + " " + methodName);
				Transpile(awesomeInventoryJobsJobGiver_FindItemByRadius, patched, methodName);
			}

			if (pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus != null)
			{
				string methodName = "Prefix";
				Log.Message("RimThreaded is patching " + pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus.FullName + " " + methodName);
				patched = typeof(RimWorld_Pawn_GuestTracker_SetGuestStatus_Transpile);
				Transpile(pawnRulesPatchRimWorld_Pawn_GuestTracker_SetGuestStatus, patched, methodName);
			}

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
			if (combatExtendedVerb_LaunchProjectileCE != null)
			{
				string methodName = "CanHitFromCellIgnoringRange";
				patched = typeof(Verb_LaunchProjectileCE_Transpile);
				Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
				Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
				methodName = "TryFindCEShootLineFromTo";
				Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
				Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
			}
			if (combatExtendedVerb_MeleeAttackCE != null)
			{
				string methodName = "TryCastShot";
				patched = typeof(Verb_MeleeAttackCE_Transpile);
				Log.Message("RimThreaded is patching " + combatExtendedVerb_MeleeAttackCE.FullName + " " + methodName);
				Transpile(combatExtendedVerb_MeleeAttackCE, patched, methodName);
			}

			if (dubsSkylight_Patch_GetRoof != null)
			{
				string methodName = "Postfix";
				patched = typeof(DubsSkylight_getPatch_Transpile);
				Log.Message("RimThreaded is patching " + dubsSkylight_Patch_GetRoof.FullName + " " + methodName);
				Transpile(dubsSkylight_Patch_GetRoof, patched, methodName);
			}

			if (jobsOfOpportunityJobsOfOpportunity_Hauling != null)
			{
				cachedStoreCell = Field(jobsOfOpportunityJobsOfOpportunity_Hauling, "cachedStoreCell");
				string methodName = "CanHaul";
				patched = typeof(Hauling_Transpile);
				Log.Message("RimThreaded is patching " + jobsOfOpportunityJobsOfOpportunity_Hauling.FullName + " " + methodName);
				Transpile(jobsOfOpportunityJobsOfOpportunity_Hauling, patched, methodName);
			}

			if (jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob != null)
			{
				string methodName = "TryOpportunisticJob";
				patched = typeof(Patch_TryOpportunisticJob_Transpile);
				Log.Message("RimThreaded is patching " + jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob.FullName + " " + methodName);
				Transpile(jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob, patched, methodName);
			}

			if (childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch != null)
			{
				string methodName = "CheckDiscovered_Pre";
				patched = typeof(childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch_Transpile);
				Log.Message("RimThreaded is patching " + childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch.FullName + " " + methodName);
				Transpile(childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch, patched, methodName);
			}

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

        private static void PatchNonDestructiveFixes()
        {
			Type original = null;
			Type patched = null;

			GenTemperature_Patch.RunNonDestructivePatches();
			HaulAIUtility_Patch.RunNonDestructivePatches();
			InfestationCellFinder_Patch.RunNonDestructivePatches();
			LongEventHandler_Patch.RunNonDestructivePatches();
			RCellFinder_Patch.RunNonDestructivePatches();
			RegionListersUpdater_Patch.RunNonDestructivePatches();
			SituationalThoughtHandler_Patch.RunNonDestructivePatches();
			World_Patch.RunNonDestructivePatches();
			FoodUtility_Patch.RunNonDestructivePatches();

			//TODO - should transpile ReplacePotentiallyRelatedPawns instead
			// Pawn_RelationsTracker.<get_PotentiallyRelatedPawns>d__28'.MoveNext()
			// // <stack>5__2 = SimplePool<List<Pawn>>.Get();
			//IL_004c: call!0 class Verse.SimplePool`1<class [mscorlib] System.Collections.Generic.List`1<class Verse.Pawn>>::Get()
			//-----------
			//CHANGE TO: new List<Pawn>();
			//-----------
			//
			// AND
			//		
			// <visited>5__3 = SimplePool<HashSet<Pawn>>.Get();
			//IL_0057: call!0 class Verse.SimplePool`1<class [System.Core] System.Collections.Generic.HashSet`1<class Verse.Pawn>>::Get()
			//--------
			//CHANGE TO: new HashSet<Pawn>();
			// ---------
			// AND
			// -----------
			// Pawn_RelationsTracker.<get_PotentiallyRelatedPawns>d__28'.'<>m__Finally1'()
			/*
			 * // <>1__state = -1;
				IL_0000: ldarg.0
				IL_0001: ldc.i4.m1
				IL_0002: stfld int32 RimWorld.Pawn_RelationsTracker/'<get_PotentiallyRelatedPawns>d__28'::'<>1__state'
				// <stack>5__2.Clear();
				IL_0007: ldarg.0
				IL_0008: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.Pawn> RimWorld.Pawn_RelationsTracker/'<get_PotentiallyRelatedPawns>d__28'::'<stack>5__2'
				IL_000d: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class Verse.Pawn>::Clear()
				// SimplePool<List<Pawn>>.Return(<stack>5__2);
				IL_0012: ldarg.0
				IL_0013: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.Pawn> RimWorld.Pawn_RelationsTracker/'<get_PotentiallyRelatedPawns>d__28'::'<stack>5__2'
				IL_0018: call void class Verse.SimplePool`1<class [mscorlib]System.Collections.Generic.List`1<class Verse.Pawn>>::Return(!0)
				// <visited>5__3.Clear();
				IL_001d: ldarg.0
				IL_001e: ldfld class [System.Core]System.Collections.Generic.HashSet`1<class Verse.Pawn> RimWorld.Pawn_RelationsTracker/'<get_PotentiallyRelatedPawns>d__28'::'<visited>5__3'
				IL_0023: callvirt instance void class [System.Core]System.Collections.Generic.HashSet`1<class Verse.Pawn>::Clear()
				// SimplePool<HashSet<Pawn>>.Return(<visited>5__3);
				IL_0028: ldarg.0
				IL_0029: ldfld class [System.Core]System.Collections.Generic.HashSet`1<class Verse.Pawn> RimWorld.Pawn_RelationsTracker/'<get_PotentiallyRelatedPawns>d__28'::'<visited>5__3'
				IL_002e: call void class Verse.SimplePool`1<class [System.Core]System.Collections.Generic.HashSet`1<class Verse.Pawn>>::Return(!0)
				// }
				IL_0033: ret
			    ---REMOVE IL_07 through IL_002e---
			*/
			// 
			//FocusStrengthOffset_GraveCorpseRelationship.CanApply
			original = typeof(FocusStrengthOffset_GraveCorpseRelationship);
			patched = typeof(Pawn_RelationsTracker_Transpile);
			MethodInfo pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
			harmony.Patch(Method(original, "CanApply"), transpiler: new HarmonyMethod(pMethod));

			//PawnDiedOrDownedThoughtsUtility.AppendThoughts_Relations
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			harmony.Patch(Method(original, "AppendThoughts_Relations"), transpiler: new HarmonyMethod(pMethod));

			//Pawn_RelationsTracker.get_RelatedPawns
			original = TypeByName("RimWorld.Pawn_RelationsTracker+<get_RelatedPawns>d__30");
			harmony.Patch(Method(original, "MoveNext"), transpiler: new HarmonyMethod(pMethod));

			//Pawn_RelationsTracker
			original = typeof(Pawn_RelationsTracker);
			//Pawn_RelationsTracker.Notify_PawnKilled
			harmony.Patch(Method(original, "Notify_PawnKilled"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker.Notify_PawnSold
			harmony.Patch(Method(original, "Notify_PawnSold"), transpiler: new HarmonyMethod(pMethod));


			//DamageWorker
			original = typeof(DamageWorker);
			patched = typeof(DamageWorker_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "ExplosionAffectCell");
			TranspileThreadStatics(original, "ExplosionCellsToHit", new Type[] { typeof(IntVec3), typeof(Map), typeof(float), typeof(IntVec3), typeof(IntVec3) });

			//GenLeaving
			original = typeof(GenLeaving);
			patched = typeof(GenLeaving_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "DropFilthDueToDamage");


			//TickList
			original = typeof(TickList);
			patched = typeof(TickList_Transpile);
			Transpile(original, patched, "RegisterThing");
			Transpile(original, patched, "DeregisterThing");

			//Rand
			original = typeof(Rand);
			patched = typeof(Rand_Transpile);
			Transpile(original, patched, "PushState", Type.EmptyTypes);
			Transpile(original, patched, "PopState");
			Transpile(original, patched, "TryRangeInclusiveWhere");

			//ThingOwner<Thing>
			original = typeof(ThingOwner<Thing>);
			patched = typeof(ThingOwnerThing_Transpile);
			Transpile(original, patched, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
			Transpile(original, patched, "Remove");

			//Thing
			original = typeof(Thing);
			patched = typeof(Thing_Transpile);
			Transpile(original, patched, "SpawnSetup");
			Transpile(original, patched, "DeSpawn");
			Transpile(original, patched, "get_FlammableNow");
			patched = typeof(Thing_Patch);
			Postfix(original, patched, "SpawnSetup");

			//RegionTraverser
			original = typeof(RegionTraverser);
			patched = typeof(RegionTraverser_Transpile);
			Transpile(original, patched, "BreadthFirstTraverse", new Type[] {
				typeof(Region),
				typeof(RegionEntryPredicate),
				typeof(RegionProcessor),
				typeof(int),
				typeof(RegionType)
			});
			Transpile(original, patched, "RecreateWorkers");

			//Verse.RegionTraverser+BFSWorker
			original = TypeByName("Verse.RegionTraverser+BFSWorker");
			patched = typeof(BFSWorker_Transpile);
			Transpile(original, patched, "QueueNewOpenRegion");
			Transpile(original, patched, "BreadthFirstTraverseWork");

			//ThinkNode_PrioritySorter
			original = typeof(ThinkNode_PrioritySorter);
			patched = typeof(ThinkNode_PrioritySorter_Transpile);
			Transpile(original, patched, "TryIssueJobPackage");

			//CellFinder
			original = typeof(CellFinder);
			patched = typeof(CellFinder_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "TryFindRandomCellNear");
			TranspileThreadStatics(original, "TryFindRandomCellInRegion");
			TranspileThreadStatics(original, "TryFindBestPawnStandCell");
			TranspileThreadStatics(original, "TryFindRandomReachableCellNear");
			TranspileThreadStatics(original, "TryFindRandomCellInsideWith");
			TranspileThreadStatics(original, "FindNoWipeSpawnLocNear");
			TranspileThreadStatics(original, "RandomRegionNear");

			//JobDriver_Wait
			original = typeof(JobDriver_Wait);
			patched = typeof(JobDriver_Wait_Transpile);
			Transpile(original, patched, "CheckForAutoAttack");

			//FloatMenuMakerMap
			original = typeof(FloatMenuMakerMap);
			patched = typeof(FloatMenuMakerMap_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "TryMakeMultiSelectFloatMenu");
			patched = typeof(FloatMenuMakerMap_Transpile);
			Transpile(original, patched, "AddHumanlikeOrders");

			//AttackTargetsCache
			original = typeof(AttackTargetsCache);
			patched = typeof(AttackTargetsCache_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "Notify_FactionHostilityChanged");
			TranspileThreadStatics(original, "Debug_AssertHostile");

			//PawnsFinder
			original = typeof(PawnsFinder);
			threadStaticPatches.Add(original, typeof(PawnsFinder_Patch));
			TranspileThreadStatics(original, "get_AllMapsWorldAndTemporary_AliveOrDead");
			TranspileThreadStatics(original, "get_AllMapsWorldAndTemporary_Alive");
			TranspileThreadStatics(original, "get_AllMapsAndWorld_Alive");
			TranspileThreadStatics(original, "get_AllMaps");
			TranspileThreadStatics(original, "get_AllMaps_Spawned");
			TranspileThreadStatics(original, "get_All_AliveOrDead");
			TranspileThreadStatics(original, "get_Temporary");
			TranspileThreadStatics(original, "get_Temporary_Alive");
			TranspileThreadStatics(original, "get_Temporary_Dead");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive");
			TranspileThreadStatics(original, "get_AllCaravansAndTravelingTransportPods_Alive");
			TranspileThreadStatics(original, "get_AllCaravansAndTravelingTransportPods_AliveOrDead");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners");
			TranspileThreadStatics(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep");
			TranspileThreadStatics(original, "get_AllMaps_PrisonersOfColonySpawned");
			TranspileThreadStatics(original, "get_AllMaps_PrisonersOfColony");
			TranspileThreadStatics(original, "get_AllMaps_FreeColonists");
			TranspileThreadStatics(original, "get_AllMaps_FreeColonistsSpawned");
			TranspileThreadStatics(original, "get_AllMaps_FreeColonistsAndPrisonersSpawned");
			TranspileThreadStatics(original, "get_AllMaps_FreeColonistsAndPrisoners");
			TranspileThreadStatics(original, "get_HomeMaps_FreeColonistsSpawned");
			TranspileThreadStatics(original, "AllMaps_SpawnedPawnsInFaction");
			TranspileThreadStatics(original, "Clear");


			//AttackTargetFinder
			original = typeof(AttackTargetFinder);
			patched = typeof(AttackTargetFinder_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "BestAttackTarget");
			TranspileThreadStatics(original, "GetAvailableShootingTargetsByScore");
			TranspileThreadStatics(original, "DebugDrawAttackTargetScores_Update");
			TranspileThreadStatics(original, "CanSee");

			//Fire			
			original = typeof(Fire);
			patched = typeof(Fire_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "DoComplexCalcs");

			//Verb
			original = typeof(Verb);
			patched = typeof(Verb_Transpile);
			Transpile(original, patched, "TryFindShootLineFromTo");
			Transpile(original, patched, "CanHitFromCellIgnoringRange");

			//Pawn_WorkSettings
			original = typeof(Pawn_WorkSettings);
			patched = typeof(Pawn_WorkSettings_Transpile);
			Transpile(original, patched, "CacheWorkGiversInOrder");

			//GenAdjFast
			original = typeof(GenAdjFast);
			patched = typeof(GenAdjFast_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "AdjacentCells8Way", new Type[] { typeof(IntVec3) });
			TranspileThreadStatics(original, "AdjacentCells8Way", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
			TranspileThreadStatics(original, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3) });
			TranspileThreadStatics(original, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });

			//GenAdj
			original = typeof(GenAdj);
			patched = typeof(GenAdj_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "TryFindRandomAdjacentCell8WayWithRoomGroup", new Type[] {
				typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(Map), typeof(IntVec3).MakeByRefType() });

			//BattleLog
			original = typeof(BattleLog);
			patched = typeof(BattleLog_Transpile);
			Transpile(original, patched, "Add");


			//WorkGiver_ConstructDeliverResources
			original = typeof(WorkGiver_ConstructDeliverResources);
			patched = typeof(WorkGiver_ConstructDeliverResources_Transpile);
			Transpile(original, patched, "ResourceDeliverJobFor", null, new string[] { "CodeOptimist.JobsOfOpportunity" });

			//PawnCapacitiesHandler
			original = typeof(PawnCapacitiesHandler);
			patched = typeof(PawnCapacitiesHandler_Transpile);
			Transpile(original, patched, "GetLevel");

			//PathFinder
			original = typeof(PathFinder);
			patched = typeof(PathFinder_Transpile);
			Transpile(original, patched, "FindPath", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) });

			//Pawn_InteractionsTracker
			original = typeof(Pawn_InteractionsTracker);
			patched = typeof(Pawn_InteractionsTracker_Transpile);
			Transpile(original, patched, "TryInteractRandomly");

			//GenRadial
			original = typeof(GenRadial);
			patched = typeof(GenRadial_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "ProcessEquidistantCells");

			//WorkGiver_DoBill
			original = typeof(WorkGiver_DoBill);
			patched = typeof(WorkGiver_DoBill_Transpile);
			Transpile(original, patched, "TryFindBestBillIngredients");
			Transpile(original, patched, "AddEveryMedicineToRelevantThings");

			//GenText
			original = typeof(GenText);
			patched = typeof(GenText_Patch);
			threadStaticPatches.Add(original, patched);
			TranspileThreadStatics(original, "CapitalizeSentences");


			//GrammarResolverSimple
			original = typeof(GrammarResolverSimple);
			patched = typeof(GrammarResolverSimple_Transpile);
			Transpile(original, patched, "Formatted");

			//GrammarResolver
			original = typeof(GrammarResolver);
			patched = typeof(GrammarResolver_Transpile);
			Transpile(original, patched, "AddRule");
			Transpile(original, patched, "RandomPossiblyResolvableEntry");
			original = TypeByName("Verse.Grammar.GrammarResolver+<>c__DisplayClass17_0");
			MethodInfo oMethod = Method(original, "<RandomPossiblyResolvableEntry>b__0");
			pMethod = Method(patched, "RandomPossiblyResolvableEntryb__0");
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));

			//Pawn_PathFollower
			original = typeof(Pawn_PathFollower);
			patched = typeof(Pawn_PathFollower_Transpile);
			Transpile(original, patched, "StartPath");

			//CompSpawnSubplant
			original = typeof(CompSpawnSubplant);
			patched = typeof(CompSpawnSubplant_Transpile);
			Transpile(original, patched, "DoGrowSubplant");

			//ColoredText
			original = typeof(ColoredText);
			patched = typeof(ColoredText_Transpile);
			Transpile(original, patched, "Resolve");

			//HediffGiver_Hypothermia
			original = typeof(HediffGiver_Hypothermia);
			patched = typeof(HediffGiver_Hypothermia_Transpile);
			Transpile(original, patched, "OnIntervalPassed");

			//Map			
			original = typeof(Map);
			patched = typeof(Map_Transpile);
			Transpile(original, patched, "MapUpdate");

		}

        private static void PatchDestructiveFixes()
        {
			Type original = null;
			Type patched = null;

			Alert_MinorBreakRisk_Patch.RunDestructivePatches();
			AlertsReadout_Patch.RunDestructivesPatches();
			ContentFinder_Texture2D_Patch.RunDestructivePatches();
			DrugAIUtility_Patch.RunDestructivePatches();
			DynamicDrawManager_Patch.RunDestructivePatches();
			GenTemperature_Patch.RunDestructivePatches();
			ListerThings_Patch.RunDestructivePatches();
			JobMaker_Patch.RunDestructivePatches();
			MaterialPool_Patch.RunDestructivePatches();
			MemoryThoughtHandler_Patch.RunDestructivePatches();
			Pawn_PlayerSettings_Patch.RunDestructivePatches();
			PawnDestinationReservationManager_Patch.RunDestructivePatches();
			PortraitRenderer_Patch.RunDestructivePatches();
			ReachabilityCache_Patch.RunDestructivePatches();
			RealtimeMoteList_Patch.RunDestructivePatches();
			RegionDirtyer_Patch.RunDestructivePatches();
			TaleManager_Patch.RunDestructivePatches();
			ThingGrid_Patch.RunDestructivePatches();
			TickList_Patch.RunDestructivePatches();
			TickManager_Patch.RunDestructivePatches();
			WorkGiver_GrowerSow_Patch.RunDestructivePatches();


			//Reachability
			original = typeof(Reachability);
			patched = typeof(Reachability_Patch);
			Prefix(original, patched, "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) });

			//PhysicalInteractionReservationManager
			original = typeof(PhysicalInteractionReservationManager);
			patched = typeof(PhysicalInteractionReservationManager_Patch);
			Prefix(original, patched, "IsReservedBy");
			Prefix(original, patched, "Reserve");
			Prefix(original, patched, "Release");
			Prefix(original, patched, "FirstReserverOf");
			Prefix(original, patched, "FirstReservationFor");
			Prefix(original, patched, "ReleaseAllForTarget");
			Prefix(original, patched, "ReleaseClaimedBy");
			Prefix(original, patched, "ReleaseAllClaimedBy");

			//SelfDefenseUtility
			original = typeof(SelfDefenseUtility);
			patched = typeof(SelfDefenseUtility_Patch);
			Prefix(original, patched, "ShouldStartFleeing");

			//GenClosest
			original = typeof(GenClosest);
			patched = typeof(GenClosest_Patch);
			Prefix(original, patched, "RegionwiseBFSWorker");

			//PawnUtility
			original = typeof(PawnUtility);
			patched = typeof(PawnUtility_Patch);
			Prefix(original, patched, "PawnBlockingPathAt");
			Prefix(original, patched, "EnemiesAreNearby");
			Prefix(original, patched, "ForceWait");

			//ThingOwnerUtility
			original = typeof(ThingOwnerUtility);
			patched = typeof(ThingOwnerUtility_Patch);
			Prefix(original, patched, "AppendThingHoldersFromThings");
			Prefix(original, patched, "GetAllThingsRecursively", new Type[] { typeof(IThingHolder), typeof(List<Thing>), typeof(bool), typeof(Predicate<IThingHolder>) });

			MethodInfo[] methods = original.GetMethods();

			//MethodInfo originalPawnGetAllThings = original.GetMethod("GetAllThingsRecursively", bf, null, new Type[] { 
			//	typeof(Map), typeof(ThingRequest), typeof(List<Pawn>), typeof(bool), typeof(Predicate<IThingHolder>), typeof(bool) }, null);
			MethodInfo originalPawnGetAllThings = methods[17];
			MethodInfo originalPawnGetAllThingsGeneric = originalPawnGetAllThings.MakeGenericMethod(new Type[] { typeof(Pawn) });
			MethodInfo patchedPawnGetAllThings = patched.GetMethod("GetAllThingsRecursively_Pawn");
			HarmonyMethod prefixPawnGetAllThings = new HarmonyMethod(patchedPawnGetAllThings);
			harmony.Patch(originalPawnGetAllThingsGeneric, prefix: prefixPawnGetAllThings);

			MethodInfo originalThingGetAllThings = methods[17];
			MethodInfo originalThingGetAllThingsGeneric = originalThingGetAllThings.MakeGenericMethod(new Type[] { typeof(Thing) });
			MethodInfo patchedThingGetAllThings = patched.GetMethod("GetAllThingsRecursively_Thing");
			HarmonyMethod prefixThingGetAllThings = new HarmonyMethod(patchedThingGetAllThings);
			harmony.Patch(originalThingGetAllThingsGeneric, prefix: prefixThingGetAllThings);

			//Pawn_MeleeVerbs
			original = typeof(Pawn_MeleeVerbs);
			patched = typeof(Pawn_MeleeVerbs_Patch);
			Prefix(original, patched, "GetUpdatedAvailableVerbsList");

			//AttackTargetsCache
			original = typeof(AttackTargetsCache);
			patched = typeof(AttackTargetsCache_Patch);
			Prefix(original, patched, "GetPotentialTargetsFor");
			Prefix(original, patched, "RegisterTarget");
			Prefix(original, patched, "DeregisterTarget");
			Prefix(original, patched, "TargetsHostileToFaction");
			Prefix(original, patched, "UpdateTarget");

			//PawnDiedOrDownedThoughtsUtility
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			patched = typeof(PawnDiedOrDownedThoughtsUtility_Patch);
			Prefix(original, patched, "RemoveLostThoughts");
			Prefix(original, patched, "RemoveDiedThoughts");
			Prefix(original, patched, "RemoveResuedRelativeThought");

			//ShootLeanUtility
			original = typeof(ShootLeanUtility);
			patched = typeof(ShootLeanUtility_Patch);
			Prefix(original, patched, "LeanShootingSourcesFromTo");

			//BuildableDef
			original = typeof(BuildableDef);
			patched = typeof(BuildableDef_Patch);
			Prefix(original, patched, "get_PlaceWorkers");

			//SustainerManager			
			original = typeof(SustainerManager);
			patched = typeof(SustainerManager_Patch);
			Prefix(original, patched, "RegisterSustainer");
			Prefix(original, patched, "DeregisterSustainer");
			Prefix(original, patched, "SustainerManagerUpdate");
			Prefix(original, patched, "UpdateAllSustainerScopes");
			Prefix(original, patched, "SustainerExists");
			Prefix(original, patched, "EndAllInMap");

			//AudioSourceMaker			
			original = typeof(AudioSourceMaker);
			patched = typeof(AudioSourceMaker_Patch);
			Prefix(original, patched, "NewAudioSourceOn");

			//AudioSource			
			original = typeof(AudioSource);
			patched = typeof(AudioSource_Patch);
			Prefix(original, patched, "Stop", Type.EmptyTypes);

			//SampleSustainer			
			original = typeof(SampleSustainer);
			patched = typeof(SampleSustainer_Patch);
			Prefix(original, patched, "TryMakeAndPlay");

			//RecreateMapSustainers
			original = typeof(AmbientSoundManager);
			patched = typeof(AmbientSoundManager_Patch);
			Prefix(original, patched, "EnsureWorldAmbientSoundCreated");

			//SubSustainer
			original = typeof(SubSustainer);
			patched = typeof(SubSustainer_Patch);
			Prefix(original, patched, "SubSustainerUpdate");

			//SoundStarter
			original = typeof(SoundStarter);
			patched = typeof(SoundStarter_Patch);
			Prefix(original, patched, "PlayOneShot");
			Prefix(original, patched, "PlayOneShotOnCamera");

			//Pawn_RelationsTracker			
			original = typeof(Pawn_RelationsTracker);
			patched = typeof(Pawn_RelationsTracker_Patch);
			Prefix(original, patched, "get_FamilyByBlood");

			//Battle			
			original = typeof(Battle);
			patched = typeof(Battle_Patch);
			Prefix(original, patched, "ExposeData");
			Prefix(original, patched, "Absorb");

			//Building_Door			
			original = typeof(Building_Door);
			patched = typeof(Building_Door_Patch);
			Prefix(original, patched, "get_DoorPowerOn");

			//ThoughtHandler						
			original = typeof(ThoughtHandler);
			patched = typeof(ThoughtHandler_Patch);
			Prefix(original, patched, "TotalOpinionOffset");
			Prefix(original, patched, "MoodOffsetOfGroup");
			Prefix(original, patched, "TotalMoodOffset");
			Prefix(original, patched, "OpinionOffsetOfGroup");

			//Projectile			
			original = typeof(Projectile);
			patched = typeof(Projectile_Patch);
			Prefix(original, patched, "ImpactSomething");
			Prefix(original, patched, "CanHit");
			Prefix(original, patched, "CheckForFreeInterceptBetween");
			Prefix(original, patched, "CheckForFreeIntercept");

			//AttackTargetReservationManager
			original = typeof(AttackTargetReservationManager);
			patched = typeof(AttackTargetReservationManager_Patch);
			Prefix(original, patched, "FirstReservationFor");
			Prefix(original, patched, "ReleaseClaimedBy");
			Prefix(original, patched, "ReleaseAllForTarget");
			Prefix(original, patched, "ReleaseAllClaimedBy");
			Prefix(original, patched, "GetReservationsCount");
			Prefix(original, patched, "Reserve");
			Prefix(original, patched, "IsReservedBy");

			//PawnCollisionTweenerUtility
			original = typeof(PawnCollisionTweenerUtility);
			patched = typeof(PawnCollisionTweenerUtility_Patch);
			Prefix(original, patched, "GetPawnsStandingAtOrAboutToStandAt");
			Prefix(original, patched, "CanGoDirectlyToNextCell");

			//ReservationManager
			original = typeof(ReservationManager);
			patched = typeof(ReservationManager_Patch);
			Prefix(original, patched, "CanReserve");
			Prefix(original, patched, "CanReserveStack");
			Prefix(original, patched, "Reserve");
			Prefix(original, patched, "Release");
			Prefix(original, patched, "ReleaseAllForTarget");
			Prefix(original, patched, "ReleaseClaimedBy");
			Prefix(original, patched, "ReleaseAllClaimedBy");
			Prefix(original, patched, "FirstReservationFor");
			Prefix(original, patched, "IsReservedByAnyoneOf");
			Prefix(original, patched, "FirstRespectedReserver");
			Prefix(original, patched, "ReservedBy", new Type[] { typeof(LocalTargetInfo), typeof(Pawn), typeof(Job) });
			//Prefix(original, patched, "ReservedByJobDriver_TakeToBed"); //TODO FIX!
			Prefix(original, patched, "AllReservedThings");
			Prefix(original, patched, "DebugString");
			Prefix(original, patched, "DebugDrawReservations");
			Prefix(original, patched, "ExposeData");

			//FloodFiller - inefficient global lock			
			original = typeof(FloodFiller);
			patched = typeof(FloodFiller_Patch);
			Prefix(original, patched, "FloodFill", new Type[] { typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, int, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>) });

			//MapPawns
			original = typeof(MapPawns);
			patched = typeof(MapPawns_Patch);
			Prefix(original, patched, "get_AllPawns");
			Prefix(original, patched, "get_AllPawnsUnspawned");
			Prefix(original, patched, "get_PrisonersOfColony");
			Prefix(original, patched, "get_FreeColonistsAndPrisoners");
			Prefix(original, patched, "get_AnyPawnBlockingMapRemoval");
			Prefix(original, patched, "get_FreeColonistsAndPrisonersSpawned");
			Prefix(original, patched, "get_SpawnedPawnsWithAnyHediff");
			Prefix(original, patched, "get_SpawnedHungryPawns");
			Prefix(original, patched, "get_SpawnedDownedPawns");
			Prefix(original, patched, "get_SpawnedPawnsWhoShouldHaveSurgeryDoneNow");
			Prefix(original, patched, "get_SpawnedPawnsWhoShouldHaveInventoryUnloaded");
			Prefix(original, patched, "get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount");
			Prefix(original, patched, "EnsureFactionsListsInit");
			Prefix(original, patched, "PawnsInFaction");
			Prefix(original, patched, "FreeHumanlikesOfFaction");
			Prefix(original, patched, "FreeHumanlikesSpawnedOfFaction");
			Prefix(original, patched, "RegisterPawn");
			Prefix(original, patched, "DeRegisterPawn");

			//MapTemperatures
			original = typeof(MapTemperature);
			patched = typeof(MapTemperature_Patch);
			Prefix(original, patched, "MapTemperatureTick");

			//Region
			original = typeof(Region);
			patched = typeof(Region_Patch);
			Prefix(original, patched, "DangerFor");
			Prefix(original, patched, "get_AnyCell");
			Prefix(original, patched, "OverlapWith");

			//Sample
			original = typeof(Sample);
			patched = typeof(Sample_Patch);
			Prefix(original, patched, "Update");

			//Sustainer
			original = typeof(Sustainer);
			patched = typeof(Sustainer_Patch);
			Prefix(original, patched, "Cleanup");
			Prefix(original, patched, "Maintain");

			//ImmunityHandler
			original = typeof(ImmunityHandler);
			patched = typeof(ImmunityHandler_Patch);
			Prefix(original, patched, "ImmunityHandlerTick");
			Prefix(original, patched, "NeededImmunitiesNow");

			//Room
			original = typeof(Room);
			patched = typeof(Room_Patch);
			Prefix(original, patched, "OpenRoofCountStopAt");
			Prefix(original, patched, "get_PsychologicallyOutdoors");
			Prefix(original, patched, "RemoveRegion");
			Prefix(original, patched, "Notify_RoofChanged");
			Prefix(original, patched, "Notify_RoomShapeOrContainedBedsChanged");
			Prefix(original, patched, "get_ContainedAndAdjacentThings");
			Prefix(original, patched, "get_Neighbors");

			//LongEventHandler
			original = typeof(LongEventHandler);
			patched = typeof(LongEventHandler_Patch);
			Prefix(original, patched, "ExecuteToExecuteWhenFinished");
			Prefix(original, patched, "ExecuteWhenFinished");

			//SituationalThoughtHandler
			original = typeof(SituationalThoughtHandler);
			patched = typeof(SituationalThoughtHandler_Patch);
			Prefix(original, patched, "AppendSocialThoughts");
			Prefix(original, patched, "Notify_SituationalThoughtsDirty");
			Prefix(original, patched, "RemoveExpiredThoughtsFromCache");

			//LordToil_Siege
			original = typeof(LordToil_Siege);
			patched = typeof(LordToil_Siege_Patch);
			Prefix(original, patched, "UpdateAllDuties");

			//PawnCapacitiesHandler
			original = typeof(PawnCapacitiesHandler);
			patched = typeof(PawnCapacitiesHandler_Patch);
			Prefix(original, patched, "Notify_CapacityLevelsDirty");
			Prefix(original, patched, "Clear");
			Prefix(original, patched, "CapableOf");
			ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
			MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
			harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));

			//PawnPath
			original = typeof(PawnPath);
			patched = typeof(PawnPath_Patch);
			Prefix(original, patched, "AddNode");
			Prefix(original, patched, "ReleaseToPool");

			//GenCollection
			original = typeof(GenCollection);
			patched = typeof(GenCollection_Patch);
			MethodInfo[] genCollectionMethods = original.GetMethods();
			MethodInfo originalRemoveAll = null;
			foreach (MethodInfo mi in genCollectionMethods)
			{
				if (mi.Name.Equals("RemoveAll") && mi.GetGenericArguments().Length == 2)
				{
					originalRemoveAll = mi;
					break;
				}
			}

			MethodInfo originalRemoveAllGeneric = originalRemoveAll.MakeGenericMethod(new Type[] { typeof(object), typeof(object) });
			MethodInfo patchedRemoveAll = patched.GetMethod("RemoveAll_Object_Object_Patch");
			HarmonyMethod prefixRemoveAll = new HarmonyMethod(patchedRemoveAll);
			harmony.Patch(originalRemoveAllGeneric, prefix: prefixRemoveAll);

			//SoundSizeAggregator
			original = typeof(SoundSizeAggregator);
			patched = typeof(SoundSizeAggregator_Patch);
			Prefix(original, patched, "RegisterReporter");
			Prefix(original, patched, "RemoveReporter");
			Prefix(original, patched, "get_AggregateSize");

			original = typeof(HediffSet);
			patched = typeof(HediffSet_Patch);
			Prefix(original, patched, "AddDirect");
			Postfix(original, patched, "DirtyCache", "DirtyCacheSetInvisbility");

			//LanguageWordInfo
			original = typeof(LanguageWordInfo);
			patched = typeof(LanguageWordInfo_Patch);
			Prefix(original, patched, "TryResolveGender");

			//JobGiver_ConfigurableHostilityResponse
			original = typeof(JobGiver_ConfigurableHostilityResponse);
			patched = typeof(JobGiver_ConfigurableHostilityResponse_Patch);
			Prefix(original, patched, "TryGetFleeJob");

			//Toils_Ingest
			original = typeof(Toils_Ingest);
			patched = typeof(Toils_Ingest_Patch);
			Prefix(original, patched, "TryFindAdjacentIngestionPlaceSpot");

			//BeautyUtility
			original = typeof(BeautyUtility);
			patched = typeof(BeautyUtility_Patch);
			Prefix(original, patched, "AverageBeautyPerceptible");

			//TendUtility
			original = typeof(TendUtility);
			patched = typeof(TendUtility_Patch);
			Prefix(original, patched, "GetOptimalHediffsToTendWithSingleTreatment");

			//WanderUtility
			original = typeof(WanderUtility);
			patched = typeof(WanderUtility_Patch);
			Prefix(original, patched, "GetColonyWanderRoot");

			//RegionAndRoomUpdater
			original = typeof(RegionAndRoomUpdater);
			patched = typeof(RegionAndRoomUpdater_Patch);
			Prefix(original, patched, "FloodAndSetRoomGroups");
			Prefix(original, patched, "TryRebuildDirtyRegionsAndRooms");

			//WealthWatcher
			original = typeof(WealthWatcher);
			patched = typeof(WealthWatcher_Patch);
			Prefix(original, patched, "ForceRecount");

			//Medicine
			original = typeof(Medicine);
			patched = typeof(Medicine_Patch);
			Prefix(original, patched, "GetMedicineCountToFullyHeal");

			//JobGiver_Work
			original = typeof(JobGiver_Work);
			patched = typeof(JobGiver_Work_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThingCountUtility
			original = typeof(ThingCountUtility);
			patched = typeof(ThingCountUtility_Patch);
			Prefix(original, patched, "AddToList");

			//BiomeDef
			original = typeof(BiomeDef);
			patched = typeof(BiomeDef_Patch);
			Prefix(original, patched, "CachePlantCommonalitiesIfShould");

			//WildPlantSpawner
			original = typeof(WildPlantSpawner);
			patched = typeof(WildPlantSpawner_Patch);
			Prefix(original, patched, "CheckSpawnWildPlantAt");
			Prefix(original, patched, "WildPlantSpawnerTickInternal");

			//TileTemperaturesComp
			original = typeof(TileTemperaturesComp);
			patched = typeof(TileTemperaturesComp_Patch);
			Prefix(original, patched, "WorldComponentTick");
			Prefix(original, patched, "ClearCaches");
			Prefix(original, patched, "GetOutdoorTemp");
			Prefix(original, patched, "GetSeasonalTemp");
			Prefix(original, patched, "OutdoorTemperatureAt");
			Prefix(original, patched, "OffsetFromDailyRandomVariation");
			Prefix(original, patched, "AverageTemperatureForTwelfth");
			Prefix(original, patched, "SeasonAcceptableFor");
			Prefix(original, patched, "OutdoorTemperatureAcceptableFor");
			Prefix(original, patched, "SeasonAndOutdoorTemperatureAcceptableFor");

			//PawnRelationUtility
			original = typeof(PawnRelationUtility);
			patched = typeof(PawnRelationUtility_Patch);
			Prefix(original, patched, "GetMostImportantColonyRelative");

			//SustainerAggregatorUtility
			original = typeof(SustainerAggregatorUtility);
			patched = typeof(SustainerAggregatorUtility_Patch);
			Prefix(original, patched, "AggregateOrSpawnSustainerFor");

			//StoryState
			original = typeof(StoryState);
			patched = typeof(StoryState_Patch);
			Prefix(original, patched, "RecordPopulationIncrease");

			//GrammarResolver
			original = typeof(GrammarResolver);
			patched = typeof(GrammarResolver_Patch);
			Prefix(original, patched, "ResolveUnsafe", new Type[] { typeof(string), typeof(GrammarRequest), typeof(bool).MakeByRefType(), typeof(string), typeof(bool), typeof(bool), typeof(List<string>), typeof(List<string>), typeof(bool) });

			//JobQueue
			original = typeof(JobQueue);
			patched = typeof(JobQueue_Patch);
			Prefix(original, patched, "AnyCanBeginNow");
			Prefix(original, patched, "EnqueueFirst");
			Prefix(original, patched, "EnqueueLast");
			Prefix(original, patched, "Contains");
			Prefix(original, patched, "Extract");
			Prefix(original, patched, "Dequeue");

			//MeditationFocusTypeAvailabilityCache
			original = typeof(MeditationFocusTypeAvailabilityCache);
			patched = typeof(MeditationFocusTypeAvailabilityCache_Patch);
			Prefix(original, patched, "PawnCanUse");
			Prefix(original, patched, "ClearFor");

			//LightningBoltMeshMaker
			original = typeof(LightningBoltMeshMaker);
			patched = typeof(LightningBoltMeshMaker_Patch);
			Prefix(original, patched, "NewBoltMesh");

			//Unforce Normal Speed			
			original = typeof(TimeControls);
			patched = typeof(TimeControls_Patch);
			Prefix(original, patched, "DoTimeControlsGUI");

			//GlobalControlsUtility
			original = typeof(GlobalControlsUtility);
			patched = typeof(GlobalControlsUtility_Patch);
			Postfix(original, patched, "DoTimespeedControls");

			//RegionCostCalculator
			original = typeof(RegionCostCalculator);
			patched = typeof(RegionCostCalculator_Patch);
			Prefix(original, patched, "GetPreciseRegionLinkDistances");
			Prefix(original, patched, "PathableNeighborIndices");
			//Prefix(original, patched, "GetRegionDistance");
			//Prefix(original, patched, "Init");

			//RegionCostCalculatorWrapper
			original = typeof(RegionCostCalculatorWrapper);
			patched = typeof(RegionCostCalculatorWrapper_Patch);
			Prefix(original, patched, "Init");

			//GUIStyle
			original = typeof(GUIStyle);
			patched = typeof(GUIStyle_Patch);
			Prefix(original, patched, "CalcHeight");
			Prefix(original, patched, "CalcSize");

			//WorldGrid
			original = typeof(WorldGrid);
			patched = typeof(WorldGrid_Patch);
			Prefix(original, patched, "IsNeighbor");
			Prefix(original, patched, "GetNeighborId");
			Prefix(original, patched, "GetTileNeighbor");
			Prefix(original, patched, "FindMostReasonableAdjacentTileForDisplayedPathCost");

			//ReservationUtility
			original = typeof(ReservationUtility);
			patched = typeof(ReservationUtility_Patch);
			Prefix(original, patched, "CanReserve");

			//WorldFloodFiller
			original = typeof(WorldFloodFiller);
			patched = typeof(WorldFloodFiller_Patch);
			Prefix(original, patched, "FloodFill", new Type[] { typeof(int), typeof(Predicate<int>), typeof(Func<int, int, bool>), typeof(int), typeof(IEnumerable<int>) });

			//RecipeWorkerCounter
			original = typeof(RecipeWorkerCounter);
			patched = typeof(RecipeWorkerCounter_Patch);
			Prefix(original, patched, "GetCarriedCount");

			//Pawn_RotationTracker
			original = typeof(Pawn_RotationTracker);
			patched = typeof(Pawn_RotationTracker_Patch);
			Prefix(original, patched, "UpdateRotation");

			//GrammarResolverSimpleStringExtensions
			original = typeof(GrammarResolverSimpleStringExtensions);
			patched = typeof(GrammarResolverSimpleStringExtensions_Patch);
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			Prefix(original, patched, "Formatted", new Type[] { typeof(string), typeof(NamedArgument[]) });


			//Pawn_HealthTracker
			original = typeof(Pawn_HealthTracker);
			patched = typeof(Pawn_HealthTracker_Patch);
			Prefix(original, patched, "RemoveHediff");
			//patched = typeof(Pawn_HealthTracker_Transpile);			
			//Transpile(original, patched, "RemoveHediff"); TODO re-add transpile

			//Pawn
			original = typeof(Pawn);
			patched = typeof(Pawn_Patch);
			Prefix(original, patched, "Destroy"); //causes strange crash to desktop without error log
			Prefix(original, patched, "VerifyReservations");

			//Pawn_JobTracker_Patch
			original = typeof(Pawn_JobTracker);
			patched = typeof(Pawn_JobTracker_Patch);
			Prefix(original, patched, "TryFindAndStartJob");
			//Prefix(original, patched, "StartJob"); conflict with giddyupcore calling MakeDriver

			//JobGiver_OptimizeApparel
			original = typeof(JobGiver_OptimizeApparel);
			patched = typeof(JobGiver_OptimizeApparel_Patch);
			Prefix(original, patched, "ApparelScoreGain");
			Prefix(original, patched, "ApparelScoreGain_NewTmp");
			Prefix(original, patched, "TryGiveJob");

			//HediffGiver_Heat
			original = typeof(HediffGiver_Heat);
			patched = typeof(HediffGiver_Heat_Patch);
			Prefix(original, patched, "OnIntervalPassed");

			//Pawn_MindState - hack for speedup. replaced (GenLocalDate.DayTick((Thing)__instance.pawn) interactions today with always 0
			original = typeof(Pawn_MindState);
			patched = typeof(Pawn_MindState_Patch);
			Prefix(original, patched, "MindStateTick");

			//WorldObjectsHolder
			original = typeof(WorldObjectsHolder);
			patched = typeof(WorldObjectsHolder_Patch);
			Prefix(original, patched, "WorldObjectsHolderTick");

			//WorldPawns
			original = typeof(WorldPawns);
			patched = typeof(WorldPawns_Patch);
			Prefix(original, patched, "WorldPawnsTick");
			Prefix(original, patched, "get_AllPawnsAlive");

			//SteadyEnvironmentEffects
			original = typeof(SteadyEnvironmentEffects);
			patched = typeof(SteadyEnvironmentEffects_Patch);
			Prefix(original, patched, "SteadyEnvironmentEffectsTick");

			//WindManager
			original = typeof(WindManager);
			patched = typeof(WindManager_Patch);
			Prefix(original, patched, "WindManagerTick");

			//FactionManager
			original = typeof(FactionManager);
			patched = typeof(FactionManager_Patch);
			Prefix(original, patched, "FactionManagerTick");

			//SeasonUtility
			original = typeof(SeasonUtility);
			patched = typeof(SeasonUtility_Patch);
			Prefix(original, patched, "GetReportedSeason");

			//TradeShip
			original = typeof(TradeShip);
			patched = typeof(TradeShip_Patch);
			Prefix(original, patched, "PassingShipTick");

			//DateNotifier
			original = typeof(DateNotifier);
			patched = typeof(DateNotifier_Patch);
			Prefix(original, patched, "FindPlayerHomeWithMinTimezone");

			//WorldComponentUtility			
			original = typeof(WorldComponentUtility);
			patched = typeof(WorldComponentUtility_Patch);
			Prefix(original, patched, "WorldComponentTick");

			//Map			
			original = typeof(Map);
			patched = typeof(Map_Patch);
			//Prefix(original, patched, "MapUpdate");
			Prefix(original, patched, "get_IsPlayerHome");

			//ThinkNode_SubtreesByTag			
			original = typeof(ThinkNode_SubtreesByTag);
			patched = typeof(ThinkNode_SubtreesByTag_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThinkNode_QueuedJob			
			original = typeof(ThinkNode_QueuedJob);
			patched = typeof(ThinkNode_QueuedJob_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//TemperatureCache			
			original = typeof(TemperatureCache);
			patched = typeof(TemperatureCache_Patch);
			Prefix(original, patched, "TryCacheRegionTempInfo");

			//JobGiver_AnimalFlee			
			original = typeof(JobGiver_AnimalFlee);
			patched = typeof(JobGiver_AnimalFlee_Patch);
			Prefix(original, patched, "FleeLargeFireJob");

			//PlayLog			
			original = typeof(PlayLog);
			patched = typeof(PlayLog_Patch);
			Prefix(original, patched, "RemoveEntry");

			//ResourceCounter			
			original = typeof(ResourceCounter);
			patched = typeof(ResourceCounter_Patch);
			Prefix(original, patched, "get_TotalHumanEdibleNutrition");
			Prefix(original, patched, "ResetDefs");
			Prefix(original, patched, "ResetResourceCounts");
			Prefix(original, patched, "GetCount");
			Prefix(original, patched, "GetCountIn", new Type[] { typeof(ThingRequestGroup) });
			Prefix(original, patched, "UpdateResourceCounts");

			//UniqueIDsManager	
			original = typeof(UniqueIDsManager);
			patched = typeof(UniqueIDsManager_Patch);
			Prefix(original, patched, "GetNextID");

			//CompCauseGameCondition	
			original = typeof(CompCauseGameCondition);
			patched = typeof(CompCauseGameCondition_Patch);
			Prefix(original, patched, "GetConditionInstance");
			Prefix(original, patched, "CreateConditionOn");
			Prefix(original, patched, "CompTick");

			//MapGenerator (Z-levels)
			original = typeof(MapGenerator);
			patched = typeof(MapGenerator_Patch);
			Prefix(original, patched, "GenerateMap");

			//RenderTexture (Giddy-Up)
			original = typeof(RenderTexture);
			patched = typeof(RenderTexture_Patch);
			Prefix(original, patched, "GetTemporaryImpl");

			//Graphics (Giddy-Up and others)
			original = typeof(Graphics);
			patched = typeof(Graphics_Patch);
			Prefix(original, patched, "Blit", new Type[] { typeof(Texture), typeof(RenderTexture) });
			Prefix(original, patched, "DrawMesh", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });

			//Graphics (Giddy-Up)
			original = typeof(Texture2D);
			patched = typeof(Texture2D_Patch);
			Prefix(original, patched, "Internal_Create");
			Prefix(original, patched, "ReadPixels", new Type[] { typeof(Rect), typeof(int), typeof(int), typeof(bool) });
			Prefix(original, patched, "Apply", new Type[] { typeof(bool), typeof(bool) });

			//SectionLayer
			original = typeof(SectionLayer);
			patched = typeof(SectionLayer_Patch);
			Prefix(original, patched, "GetSubMesh");

			//GraphicDatabaseHeadRecords
			original = typeof(GraphicDatabaseHeadRecords);
			patched = typeof(GraphicDatabaseHeadRecords_Patch);
			Prefix(original, patched, "BuildDatabaseIfNecessary");

			//MeshMakerPlanes
			original = typeof(MeshMakerPlanes);
			patched = typeof(MeshMakerPlanes_Patch);
			Prefix(original, patched, "NewPlaneMesh", new Type[] { typeof(Vector2), typeof(bool), typeof(bool), typeof(bool) });

			//MeshMakerShadows
			original = typeof(MeshMakerShadows);
			patched = typeof(MeshMakerShadows_Patch);
			Prefix(original, patched, "NewShadowMesh", new Type[] { typeof(float), typeof(float), typeof(float) });

			//QuestUtility
			original = typeof(QuestUtility);
			patched = typeof(QuestUtility_Patch);
			Prefix(original, patched, "GetExtraFaction");

			//Job
			original = typeof(Job);
			patched = typeof(Job_Patch);
			Prefix(original, patched, "MakeDriver");
			Prefix(original, patched, "ToString", Type.EmptyTypes);

			//RestUtility
			original = typeof(RestUtility);
			patched = typeof(RestUtility_Patch);
			Prefix(original, patched, "GetBedSleepingSlotPosFor");

			//Lord
			original = typeof(Lord);
			patched = typeof(Lord_Patch);
			Prefix(original, patched, "AddPawn");
			Prefix(original, patched, "RemovePawn");

			//LordManager
			original = typeof(LordManager);
			patched = typeof(LordManager_Patch);
			Prefix(original, patched, "LordOf", new Type[] { typeof(Pawn) });
			Prefix(original, patched, "RemoveLord");

		}

		private static readonly HarmonyMethod ThreadStaticsTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "ReplaceThreadStatics"));

        public static void TranspileThreadStatics(Type original, string methodName, Type[] orig_type = null)
        {
			MethodInfo methodInfo;
			if (orig_type == null)
			{
				methodInfo = Method(original, methodName);
			} else
            {
				methodInfo = Method(original, methodName, orig_type);
			}
			harmony.Patch(methodInfo, transpiler: ThreadStaticsTranspiler);
		}

		public static void Prefix(Type original, Type patched, string methodName, Type[] orig_type = null, bool destructive = true)
		{
			MethodInfo oMethod = Method(original, methodName, orig_type);
			Type[] patch_type = null;
			if (orig_type != null)
			{
				patch_type = new Type[orig_type.Length];
				Array.Copy(orig_type, patch_type, orig_type.Length);

				if (!oMethod.ReturnType.Name.Equals("Void"))
				{
					Type[] temp_type = patch_type;
					patch_type = new Type[temp_type.Length + 1];
					patch_type[0] = oMethod.ReturnType.MakeByRefType();
					Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
				}
				if (!oMethod.IsStatic)
				{
					Type[] temp_type = patch_type;
					patch_type = new Type[temp_type.Length + 1];
					patch_type[0] = original;
					Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
				}
			}
			MethodInfo pMethod = Method(patched, methodName, patch_type);
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod));
			if (!destructive)
			{
				nonDestructivePrefixes.Add(pMethod);
			}
		}

		public static void Postfix(Type original, Type patched, string originalMethodName, string patchedMethodName = null)
		{
			MethodInfo oMethod = Method(original, originalMethodName);
			if (patchedMethodName == null)
				patchedMethodName = originalMethodName;
			MethodInfo pMethod = Method(patched, patchedMethodName);
			harmony.Patch(oMethod, postfix: new HarmonyMethod(pMethod));
		}

		public static void Transpile(Type original, Type patched, string methodName, Type[] orig_type = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, orig_type);
			MethodInfo pMethod = Method(patched, methodName);
			HarmonyMethod transpilerMethod = new HarmonyMethod(pMethod)
			{
				after = harmonyAfter
			};
			harmony.Patch(oMethod, transpiler: transpilerMethod);
		}

	}

}