using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse.Sound;
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

		static RimThreadedHarmony()
		{
			Harmony.DEBUG = true;
			Log.Message("RimThreaded " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "  is patching methods...");

			PatchDestructiveFixes();
			PatchNonDestructiveFixes();
			PatchModCompatibility();

			Log.Message("RimThreaded patching is complete.");
		}

		public static List<CodeInstruction> EnterLock(LocalBuilder lockObject, LocalBuilder lockTaken, List<CodeInstruction> loadLockObjectInstructions, CodeInstruction currentInstruction)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			loadLockObjectInstructions[0].labels = currentInstruction.labels;
			for (int i = 0; i < loadLockObjectInstructions.Count; i++)
			{
				codeInstructions.Add(loadLockObjectInstructions[i]);
			}
			currentInstruction.labels = new List<Label>();
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
		public static List<CodeInstruction> ExitLock(ILGenerator iLGenerator, LocalBuilder lockObject, LocalBuilder lockTaken, CodeInstruction currentInstruction)
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
			currentInstruction.labels.Add(endHandlerDestination);
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

		public static IEnumerable<CodeInstruction> WrapMethodInInstanceLock(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0)
			};
			LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
				yield return ci;
			while (i < instructionsList.Count - 1)
			{
				yield return instructionsList[i++];
			}
			foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
				yield return ci;
			yield return instructionsList[i++];
		}

		//public static readonly Dictionary<Type, Type> threadStaticPatches = new Dictionary<Type, Type>();
		public static readonly Dictionary<FieldInfo, object> replaceFields = new Dictionary<FieldInfo, object>();

		public static void AddAllMatchingFields(Type original, Type patched, bool matchStaticFieldsOnly = true)
		{
			IEnumerable<KeyValuePair<FieldInfo, FieldInfo>> allMatchingFields = GetAllMatchingFields(original, patched, matchStaticFieldsOnly);
			foreach (KeyValuePair<FieldInfo, FieldInfo> matchingFields in allMatchingFields)
			{
				//Log.Message("Adding field replacement for: " + matchingFields.Key.DeclaringType.Name + "." + matchingFields.Key.Name + " with: " + matchingFields.Value.DeclaringType.Name + "." + matchingFields.Value.Name);
				replaceFields.Add(matchingFields.Key, matchingFields.Value);
			}
		}

		public static IEnumerable<KeyValuePair<FieldInfo, FieldInfo>> GetAllMatchingFields(Type original, Type patched, bool matchStaticFieldsOnly = true)
		{
			foreach (FieldInfo newFieldInfo in GetDeclaredFields(patched))
			{
				if (!matchStaticFieldsOnly || newFieldInfo.IsStatic)
				{
					foreach (FieldInfo fieldInfo in GetDeclaredFields(original))
					{
						if (fieldInfo.Name.Equals(newFieldInfo.Name) && fieldInfo.FieldType == newFieldInfo.FieldType)
						{
							yield return new KeyValuePair<FieldInfo, FieldInfo>(fieldInfo, newFieldInfo);
						}
					}
				}
			}
		}

		public static IEnumerable<CodeInstruction> ReplaceFieldsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			foreach(CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.operand != null)
				{
					if (codeInstruction.operand is FieldInfo fieldInfo)
					{
						if (replaceFields.TryGetValue(fieldInfo, out object newFieldInfo))
						{
							//Log.Message("RimThreaded is replacing field: " + fieldInfo.DeclaringType.ToString() + "." + fieldInfo.Name + " with field: " + newFieldInfo.DeclaringType.ToString() + "." + newFieldInfo.Name);
							if(newFieldInfo is MethodInfo)
                            {
								codeInstruction.opcode = OpCodes.Call;
							}
							codeInstruction.operand = newFieldInfo;
						}
					}
				}
				yield return codeInstruction;
			}
		}

		public static IEnumerable<CodeInstruction> Add3Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				if (i + 3 < instructionsList.Count && instructionsList[i + 3].opcode == OpCodes.Callvirt)
				{
					if (instructionsList[i + 3].operand is MethodInfo methodInfo)
					{
						if (methodInfo.Name.Equals("Add") && methodInfo.DeclaringType.FullName.Contains("System.Collections"))
						{
							LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
							LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
							List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>()
							{
								new CodeInstruction(OpCodes.Ldarg_0)
							};
							foreach (CodeInstruction lockInstruction in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							foreach (CodeInstruction lockInstruction in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							continue;
						}
					}
				}
				yield return instructionsList[i++];
			}
		}


		public static readonly HarmonyMethod replaceFieldsHarmonyTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "ReplaceFieldsTranspiler"));
		public static readonly HarmonyMethod methodLockTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "WrapMethodInInstanceLock"));
        public static readonly HarmonyMethod add3Transpiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "Add3Transpiler"));
        public static readonly HarmonyMethod GameObjectTranspiler = new HarmonyMethod(Method(typeof(GameObject_Patch), "TranspileGameObjectConstructor"));
        public static readonly HarmonyMethod TimeFrameCountTranspiler = new HarmonyMethod(Method(typeof(Time_Patch), "TranspileTimeFrameCount"));
		public static readonly HarmonyMethod RealtimeSinceStartupTranspiler = new HarmonyMethod(Method(typeof(Time_Patch), "TranspileRealtimeSinceStartup"));
        public static readonly HarmonyMethod ComponentTransformTranspiler = new HarmonyMethod(Method(typeof(Component_Patch), "TranspileComponentTransform"));
		public static readonly HarmonyMethod GameObjectTransformTranspiler = new HarmonyMethod(Method(typeof(GameObject_Patch), "TranspileGameObjectTransform"));
        
		public static void TranspileTimeFrameCountReplacement(Type original, string methodName, Type[] origType = null)
        {
            harmony.Patch(Method(original, methodName, origType), transpiler: TimeFrameCountTranspiler);
        }
		public static void TranspileFieldReplacements(Type original, string methodName, Type[] origType = null)
		{
			//Log.Message("RimThreaded is TranspilingFieldReplacements for method: " + original.Name + "." + methodName);
			harmony.Patch(Method(original, methodName, origType), transpiler: replaceFieldsHarmonyTranspiler);
		}

		public static void TranspileLockAdd3(Type original, string methodName, Type[] origType = null)
		{
			harmony.Patch(Method(original, methodName, origType), transpiler: add3Transpiler);
		}

		public static void Prefix(Type original, Type patched, string methodName, Type[] origType = null, bool destructive = true)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			Type[] patch_type = null;
			if (origType != null)
			{
				patch_type = new Type[origType.Length];
				Array.Copy(origType, patch_type, origType.Length);

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

		public static void Transpile(Type original, Type patched, string methodName, Type[] origType = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			MethodInfo pMethod = Method(patched, methodName);
			HarmonyMethod transpilerMethod = new HarmonyMethod(pMethod)
			{
				after = harmonyAfter
			};
			try
			{
				harmony.Patch(oMethod, transpiler: transpilerMethod);
			} catch (Exception e)
            {
				Log.Error("Exception Transpiling: " + oMethod.ToString() + " " + transpilerMethod.ToString() + " " + e.ToString());
            }
		}
		public static void TranspileMethodLock(Type original, string methodName, Type[] origType = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			harmony.Patch(oMethod, transpiler: methodLockTranspiler);
		}


		private static void PatchNonDestructiveFixes()
		{

			//Simple
			Area_Patch.RunNonDestructivePatches();
			AttackTargetFinder_Patch.RunNonDestructivePatches();
			BeautyUtility_Patch.RunNonDestructivePatches();
			BuildableDef_Patch.RunNonDestructivePatches(); 
			CellFinder_Patch.RunNonDestructivePatches();
			DamageWorker_Patch.RunNonDestructivePatches();
			Fire_Patch.RunNonDestructivePatches();
			FloatMenuMakerMap_Patch.RunNonDestructivePatches();
			FoodUtility_Patch.RunNonDestructivePatches();
			GenAdj_Patch.RunNonDestructivePatches();
			GenAdjFast_Patch.RunNonDestructivePatches();
			GenLeaving_Patch.RunNonDestructivePatches();
			GenRadial_Patch.RunNonDestructivePatches();
			GenText_Patch.RunNonDestructivePatches();
			GrammarResolverSimpleStringExtensions_Patch.RunNonDestructivePatches();
			HaulAIUtility_Patch.RunNonDestructivePatches();
			SlotGroup_Patch.RunNonDestructivePatches();
			ImmunityHandler_Patch.RunNonDestructivePatches();
			JobGiver_AnimalFlee_Patch.RunNonDestructivePatches(); //may need changes to FleeLargeFireJob
			JobGiver_ConfigurableHostilityResponse_Patch.RunNonDestructivePatches();
			LanguageWordInfo_Patch.RunNonDestructivePatches();
			MapTemperature_Patch.RunNonDestructivePatches();
			Medicine_Patch.RunNonDestructivePatches();
			Pawn_InteractionsTracker_Transpile.RunNonDestructivePatches();
			Pawn_MeleeVerbs_Patch.RunNonDestructivePatches();
			Pawn_WorkSettings_Patch.RunNonDestructivePatches();
			PawnsFinder_Patch.RunNonDestructivePatches();
			PawnDiedOrDownedThoughtsUtility_Patch.RunNonDestructivePatches();
			Projectile_Patch.RunNonDestructivePatches();
			RCellFinder_Patch.RunNonDestructivePatches();
			RegionAndRoomUpdater_Patch.RunNonDestructivePatches();
			RegionCostCalculator_Patch.RunNonDestructivePatches();
			RegionListersUpdater_Patch.RunNonDestructivePatches();
			RegionMaker_Patch.RunNonDestructivePatches();
			TendUtility_Patch.RunNonDestructivePatches();
			ThinkNode_PrioritySorter_Patch.RunNonDestructivePatches();
			ThoughtHandler_Patch.RunNonDestructivePatches();
			Toils_Ingest_Patch.RunNonDestructivePatches();
			Verb_Patch.RunNonDestructivePatches();
			WanderUtility_Patch.RunNoneDestructivePatches();
			WildPlantSpawner_Patch.RunNonDestructivePatches();
			World_Patch.RunNonDestructivePatches();
			WorldGrid_Patch.RunNonDestructivePatches();
			ZoneManager_Patch.RunNonDestructivePatches();
			Zone_Patch.RunNonDestructivePatches();


			//Complex
			AttackTargetsCache_Patch.RunNonDestructivePatches();
			BattleLog_Transpile.RunNonDestructivePatches();
			CompSpawnSubplant_Transpile.RunNonDestructivePatches();//uses old transpile for lock
			GenTemperature_Patch.RunNonDestructivePatches();
			GrammarResolver_Transpile.RunNonDestructivePatches();//reexamine complexity
			GrammarResolverSimple_Transpile.RunNonDestructivePatches();//reexamine complexity
			HediffGiver_Hypothermia_Transpile.RunNonDestructivePatches();
			HediffSet_Patch.RunNonDestructivePatches();
			InfestationCellFinder_Patch.RunNonDestructivePatches(); //fix public struct
			LongEventHandler_Patch.RunNonDestructivePatches();
			Map_Transpile.RunNonDestructivePatches();
			PathFinder_Transpile.RunNonDestructivePatches(); //large method
			PawnCapacitiesHandler_Transpile.RunNonDestructivePatches(); //reexamine complexity?
			Rand_Patch.RunNonDestructivePatches(); //uses old transpile for lock
			RegionTraverser_Transpile.RunNonDestructivePatches(); 
			SituationalThoughtHandler_Patch.RunNonDestructivePatches();
			Thing_Patch.RunNonDestructivePatches();
			ThingOwnerThing_Transpile.RunNonDestructivePatches();
			TickList_Patch.RunNonDestructivePatches();
			WealthWatcher_Patch.RunNonDestructivePatches();
			WorldFloodFiller_Patch.RunNonDestructivePatches();
			WorkGiver_ConstructDeliverResources_Transpile.RunNonDestructivePatches(); //reexamine complexity
			WorkGiver_DoBill_Transpile.RunNonDestructivePatches(); //better way to find bills with cache
			QuestUtility_Patch.RunNonDestructivePatches();


			Pawn_RelationsTracker_Transpile.RunNonDestructivePatches(); //TODO - should transpile ReplacePotentiallyRelatedPawns instead
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

		}

		private static void PatchDestructiveFixes()
		{
			Alert_MinorBreakRisk_Patch.RunDestructivePatches();
			AlertsReadout_Patch.RunDestructivesPatches();
			AmbientSoundManager_Patch.RunDestructivePatches();
			AttackTargetsCache_Patch.RunDestructivesPatches(); //TODO: write ExposeData and change concurrentdictionary
			AudioSource_Patch.RunDestructivePatches();
			AudioSourceMaker_Patch.RunDestructivePatches();
			Building_Door_Patch.RunDestructivePatches(); //strange bug
			CompCauseGameCondition_Patch.RunDestructivePatches();
			DateNotifier_Patch.RunDestructivePatches(); //performance boost when playing on only 1 map
			DrugAIUtility_Patch.RunDestructivePatches();
			DynamicDrawManager_Patch.RunDestructivePatches();
			FactionManager_Patch.RunDestructivePatches();
			GenClosest_Patch.RunDestructivePatches();
			GenCollection_Patch.RunDestructivePatches();
			GenTemperature_Patch.RunDestructivePatches();
			GlobalControlsUtility_Patch.RunDestructivePatches();
			GrammarResolver_Patch.RunDestructivePatches();
			HediffGiver_Heat_Patch.RunDestructivePatches();
			HediffSet_Patch.RunDestructivePatches();
			ImmunityHandler_Patch.RunDestructivePatches();
			ListerThings_Patch.RunDestructivePatches();
			JobGiver_Work_Patch.RunDestructivePatches();
			JobMaker_Patch.RunDestructivePatches();
			LongEventHandler_Patch.RunDestructivePatches();
			Lord_Patch.RunDestructivePatches();
			LordManager_Patch.RunDestructivePatches();
			LordToil_Siege_Patch.RunDestructivePatches(); //TODO does locks around clears and adds. TRANSPILE
			Map_Patch.RunDestructivePatches(); //TODO - discover root cause
			MaterialPool_Patch.RunDestructivePatches();
			MemoryThoughtHandler_Patch.RunDestructivePatches();
			Pawn_HealthTracker_Patch.RunDestructivePatches(); //TODO re-add transpile
			Pawn_MindState_Patch.RunDestructivePatches(); //TODO - destructive hack for speed up - maybe not needed
			Pawn_PlayerSettings_Patch.RunDestructivePatches();
			Pawn_RelationsTracker_Patch.RunDestructivePatches();
			PawnPath_Patch.RunDestructivePatches();
			PawnUtility_Patch.RunDestructivePatches();
			PawnDestinationReservationManager_Patch.RunDestructivePatches();
			PlayLog_Patch.RunDestructivePatches();
			PortraitRenderer_Patch.RunDestructivePatches();
			PhysicalInteractionReservationManager_Patch.RunDestructivePatches(); //TODO: write ExposeData and change concurrent dictionary
			Reachability_Patch.RunDestructivePatches();
			ReachabilityCache_Patch.RunDestructivePatches();
			RealtimeMoteList_Patch.RunDestructivePatches();
			RecipeWorkerCounter_Patch.RunDestructivePatches(); // rexamine purpose
			RegionAndRoomUpdater_Patch.RunDestructivePatches();
			RegionDirtyer_Patch.RunDestructivePatches();
			RegionMaker_Patch.RunDestructivePatches();
			ResourceCounter_Patch.RunDestructivePatches();
			Sample_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SampleSustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SeasonUtility_Patch.RunDestructivePatches(); //performance boost
			ShootLeanUtility_Patch.RunDestructivePatches(); //TODO: excessive locks, therefore RimThreadedHarmony.Prefix, conncurrent_queue could be transpiled in
			SoundSizeAggregator_Patch.RunDestructivePatches(); //TODO: low priority, reexamine sound
			SoundStarter_Patch.RunDestructivePatches(); //TODO: low priority, reexamine sound
			SteadyEnvironmentEffects_Patch.RunDestructivePatches();
			StoryState_Patch.RunDestructivePatches(); //WrapMethodInInstanceLock
			SubSustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			Sustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SustainerAggregatorUtility_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SustainerManager_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			TaleManager_Patch.RunDestructivePatches();
			ThingGrid_Patch.RunDestructivePatches();
			ThinkNode_SubtreesByTag_Patch.RunDestructivePatches();
			TickManager_Patch.RunDestructivePatches();
			TileTemperaturesComp_Patch.RunDestructivePatches(); //TODO - good simple transpile candidate
			TimeControls_Patch.RunDestructivePatches(); //TODO TRANSPILE - should releave needing TexButton2 class
			TradeShip_Patch.RunDestructivePatches();
			UniqueIDsManager_Patch.RunDestructivePatches();
			Verb_Patch.RunDestructivePatches(); // TODO: why is this causing null?
			WealthWatcher_Patch.RunDestructivePatches();
			WildPlantSpawner_Patch.RunDestructivePatches();
			WindManager_Patch.RunDestructivePatches();
			WorkGiver_GrowerSow_Patch.RunDestructivePatches();
			WorldComponentUtility_Patch.RunDestructivePatches();
			WorldObjectsHolder_Patch.RunDestructivePatches();
			WorldPawns_Patch.RunDestructivePatches(); //todo examine GC optimization

            //complex methods that need further review for simplification
            AttackTargetReservationManager_Patch.RunDestructivePatches();
            BiomeDef_Patch.RunDestructivePatches();
            FloodFiller_Patch.RunDestructivePatches();//FloodFiller - inefficient global lock - threadstatics might help do these concurrently?
            JobGiver_OptimizeApparel_Patch.RunDestructivePatches();
            JobQueue_Patch.RunDestructivePatches();
            MapPawns_Patch.RunDestructivePatches();
            MeditationFocusTypeAvailabilityCache_Patch.RunDestructivePatches();
            Pawn_JobTracker_Patch.RunDestructivePatches();
            Pawn_Patch.RunDestructivePatches();
            PawnCapacitiesHandler_Patch.RunDestructivePatches();
            Region_Patch.RunDestructivePatches();
            ReservationManager_Patch.RunDestructivePatches();
            Room_Patch.RunDestructivePatches();
            SituationalThoughtHandler_Patch.RunDestructivePatches();
            ThingOwnerUtility_Patch.RunDestructivePatches(); //TODO fix method reference by index

			//main-thread-only
			ContentFinder_Texture2D_Patch.RunDestructivePatches();
			GraphicDatabaseHeadRecords_Patch.RunDestructivePatches();
			Graphics_Patch.RunDestructivePatches();//Graphics (Giddy-Up and others)
			GUIStyle_Patch.RunDestructivePatches();
			LightningBoltMeshMaker_Patch.RunDestructivePatches();
			MapGenerator_Patch.RunDestructivePatches();//MapGenerator (Z-levels)
			MeshMakerPlanes_Patch.RunDestructivePatches();
			MeshMakerShadows_Patch.RunDestructivePatches();
			RenderTexture_Patch.RunDestructivePatches();//RenderTexture (Giddy-Up)
			SectionLayer_Patch.RunDestructivePatches();
			Texture2D_Patch.RunDestructivePatches();//Graphics (Giddy-Up)

			//Development mode patches
			//GameObject_Patch.RunNonDestructivePatches();
            //TranspileGameObjectConstructor
			Material_Patch.RunDestructivePatches();
			Transform_Patch.RunDestructivePatches();
			UnityEngine_Object_Patch.RunDestructivePatches();

			//harmony.Patch(Constructor(typeof(Verse.Sound.Sustainer)), transpiler: GameObjectTranspiler);
            //List<ConstructorInfo> constructorInfos = GetDeclaredConstructors(typeof(Verse.Sound.Sustainer));
            ConstructorInfo constructorSustainer = Constructor(typeof(Verse.Sound.Sustainer), new Type[] {typeof(SoundDef), typeof(SoundInfo)});
			harmony.Patch(constructorSustainer, transpiler: GameObjectTranspiler);
            
			//TimeFrameCountTranspiler Fixes
			//harmony.Patch(Method(typeof(RimWorld.AlertsReadout), "AlertsReadoutUpdate"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Alert_Critical), "AlertActiveUpdate"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Building_Bed), "ToggleForPrisonersByInterface"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.CompAbilityEffect_Chunkskip), "FindClosestChunks"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.GenWorld), "MouseTile"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.InfestationCellFinder), "DebugDraw"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.LessonAutoActivator), "LessonAutoActivatorUpdate"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.ListerHaulables), "DebugString"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.ListerMergeables), "DebugString"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "DrawPowerGridOverlayThisFrame"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "get_ShouldDrawPowerGrid"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "DrawZonesThisFrame"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "get_ShouldDrawZones"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.SocialCardUtility), "CheckRecache"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Storyteller), "DebugString"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.MouseoverSounds), "SilenceForNextFrame"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.MouseoverSounds), "ResolveFrame"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "<.ctor>b__12_0"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "StartSample"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "<.ctor>b__15_0"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "SustainerUpdate"), transpiler: TimeFrameCountTranspiler);
            //harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "Maintain"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.CameraDriver), "get_CurrentViewRect"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.CellRenderer), "InitFrame"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.DebugInputLogger), "InputLogOnGUI"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.DesignationDragger), "UpdateDragCellsIfNeeded"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.Dialog_Rename), "get_AcceptsInput"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.Dialog_Rename), "WasOpenedByHotkey"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.FloatMenuWorld), "DoWindowContents"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.GenUI), "GetWidthCached"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.GizmoGridDrawer), "get_HeightDrawnRecently"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.GizmoGridDrawer), "DrawGizmoGrid"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.GUIEventFilterForOSX), "CheckRejectGUIEvent"), transpiler: TimeFrameCountTranspiler);
			harmony.Patch(Method(typeof(Verse.GUIEventFilterForOSX), "RejectEvent"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.RealTime), "Update"), transpiler: TimeFrameCountTranspiler);
            //harmony.Patch(Method(typeof(Verse.Region), "DangerFor"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.RoomGroupTempTracker), "DebugString"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.Root), "Update"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.ScreenshotTaker), "Update"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.ScreenshotTaker), "TakeNonSteamShot"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.UIHighlighter), "HighlightTag"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.UIHighlighter), "HighlightOpportunity"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.UIHighlighter), "UIHighlighterUpdate"), transpiler: TimeFrameCountTranspiler);
            harmony.Patch(Method(typeof(Verse.UnityGUIBugsFixer), "FixDelta"), transpiler: TimeFrameCountTranspiler);

			//RealtimeSinceStartupTranspiler
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "CalculateCurInputDollyVect"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldSelectionDrawer), "Notify_Selected"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Alert), "Notify_Started"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.CompProjectileInterceptor), "GetCurrentAlpha_Idle"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.CompProjectileInterceptor), "GetCurrentAlpha_Selected"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Designator_Place), "HandleRotationShortcuts"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.LearningReadout), "LearningReadoutOnGUI"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Lesson), "get_AgeSeconds"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Lesson), "OnActivated"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.OverlayDrawer), "RenderPulsingOverlayInternal"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.PlaceWorker_WatermillGenerator), "DrawGhost"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Screen_Credits), "PreOpen"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Screen_Credits), "WindowUpdate"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(RimWorld.SelectionDrawer), "Notify_Selected"), transpiler: RealtimeSinceStartupTranspiler);
            ColonistBarColonistDrawer_Patch.RunNonDestructivePatches();
            WorldSelectionDrawer_Patch.RunNonDestructivePatches();
			//harmony.Patch(Method(typeof(RimWorld.SelectionDrawerUtility), "CalculateSelectionBracketPositionsUI"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.SelectionDrawerUtility), "CalculateSelectionBracketPositionsWorld"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.Sample), "get_AgeRealTime"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Constructor(typeof(Verse.Sound.Sample)), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SampleSustainer), "get_Volume"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_Perlin), "ValueFor"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_SourceAge), "ValueFor"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SoundSlotManager), "CanPlayNow"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SoundSlotManager), "Notify_Played"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "<.ctor>b__12_0"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "StartSample"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "SubSustainerUpdate"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "get_TimeSinceEnd"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "End"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.ArenaUtility), "PerformBattleRoyale"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.CameraDriver), "CalculateCurInputDollyVect"), transpiler: RealtimeSinceStartupTranspiler);
			harmony.Patch(Method(typeof(Verse.CameraShaker), "get_ShakeOffset"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.DesignationDragger), "DraggerUpdate"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Dialog_MessageBox), "get_TimeUntilInteractive"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Dialog_NodeTree), "get_InteractiveNow"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Dialog_ResolutionConfirm), "get_TimeUntilRevert"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Constructor(typeof(Verse.Dialog_ResolutionConfirm), Type.EmptyTypes), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.GameInfo), "GameInfoOnGUI"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.GameInfo), "GameInfoUpdate"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.GameplayTipWindow), "DrawContents"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.GenText), "MarchingEllipsis"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.LongEventHandler), "UpdateCurrentEnumeratorEvent"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Mote), "get_AgeSecs"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Mote), "SpawnSetup"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Pulser), "PulseBrightness", new [] {typeof(float), typeof(float) }), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.RealTime), "Update"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Region), "DebugDrawMouseover"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.ScreenFader), "get_CurTime"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.TooltipHandler), "TipRegion", new [] {typeof(Rect), typeof(TipSignal)}), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.TooltipHandler), "DrawActiveTips"), transpiler: RealtimeSinceStartupTranspiler);
            harmony.Patch(Method(typeof(Verse.Widgets), "CheckPlayDragSliderSound"), transpiler: RealtimeSinceStartupTranspiler);

			//TranspileComponentTransform Fixes
			harmony.Patch(Method(typeof(RimWorld.Planet.DebugTile), "get_DistanceToCamera"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.GenWorldUI), "CurUITileSize"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "get_CurrentRealPosition"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "Update"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "ApplyPositionToGameObject"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraManager), "CreateWorldSkyboxCamera"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_Position"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_Rotation"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "set_Rotation"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_LocalPosition"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "set_LocalPosition"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "Init"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.MusicManagerEntry), "StartPlaying"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.MusicManagerPlay), "MusicUpdate"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.Page_SelectStartingSite), "PostOpen"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.PortraitCameraManager), "CreatePortraitCamera"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Method(typeof(RimWorld.PortraitRenderer), "RenderPortrait"), transpiler: ComponentTransformTranspiler);
            harmony.Patch(Constructor(typeof(Verse.Sound.AudioSourcePoolCamera)), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.Sample), "ToString"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_CameraAltitude), "ValueFor"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.CameraDriver), "get_CurrentRealPosition"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.CameraDriver), "ApplyPositionToGameObject"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.DamageWorker), "ExplosionStart"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.SkyOverlay), "DrawOverlay"), transpiler: ComponentTransformTranspiler);
			harmony.Patch(Method(typeof(Verse.SubcameraDriver), "Init"), transpiler: ComponentTransformTranspiler);

			//GameObjectTransformTranspiler
            harmony.Patch(Method(typeof(MusicManagerEntry), "StartPlaying"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(MusicManagerPlay), "MusicUpdate"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Constructor(typeof(Verse.Sound.AudioSourcePoolCamera)), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Constructor(typeof(AudioSourcePoolWorld)), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(SampleOneShot), "TryMakeAndPlay"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(SampleSustainer), "TryMakeAndPlay"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(Sustainer), "get_CameraDistanceSquared"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(Sustainer), "UpdateRootObjectPosition"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(Sustainer), "Cleanup"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(CameraDriver), "ApplyPositionToGameObject"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(CameraSwooper), "OffsetCameraFrom"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(GenDebug), "DebugPlaceSphere"), transpiler: GameObjectTransformTranspiler);
            harmony.Patch(Method(typeof(SubcameraDriver), "Init"), transpiler: GameObjectTransformTranspiler);


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

	}

}