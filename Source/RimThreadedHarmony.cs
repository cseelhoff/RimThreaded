using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System.Threading;
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
			foreach (CodeInstruction ci in EnterLock(
				lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
				yield return ci;
			while (i < instructionsList.Count - 1)
			{
				yield return instructionsList[i++];
			}
			foreach (CodeInstruction ci in ExitLock(
				iLGenerator, lockObject, lockTaken, instructionsList, ref i))
				yield return ci;
			yield return instructionsList[i++];
		}

		//public static readonly Dictionary<Type, Type> threadStaticPatches = new Dictionary<Type, Type>();
		public static readonly Dictionary<FieldInfo, FieldInfo> replaceFields = new Dictionary<FieldInfo, FieldInfo>();

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
						if (replaceFields.TryGetValue(fieldInfo, out FieldInfo newFieldInfo))
						{
							//Log.Message("RimThreaded is replacing field: " + fieldInfo.DeclaringType.ToString() + "." + fieldInfo.Name + " with field: " + newFieldInfo.DeclaringType.ToString() + "." + newFieldInfo.Name);
							codeInstruction.operand = newFieldInfo;
						}
					}
				}
				yield return codeInstruction;
			}
		}


		public static readonly HarmonyMethod replaceFieldsHarmonyTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "ReplaceFieldsTranspiler"));
		public static readonly HarmonyMethod methodLockTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "WrapMethodInInstanceLock"));
		

		public static void TranspileFieldReplacements(Type original, string methodName, Type[] orig_type = null)
		{
			//Log.Message("RimThreaded is TranspilingFieldReplacements for method: " + original.Name + "." + methodName);
			harmony.Patch(Method(original, methodName, orig_type), transpiler: replaceFieldsHarmonyTranspiler);
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
		public static void TranspileMethodLock(Type original, string methodName, Type[] orig_type = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, orig_type);
			harmony.Patch(oMethod, transpiler: methodLockTranspiler);
		}


		private static void PatchNonDestructiveFixes()
		{
			ThingOwnerThing_Transpile.RunNonDestructivePatches(); //REMOVEAT WILL CAUSE RANGE ERROR. ADD transpile needs new style. 
			Thing_Patch.RunNonDestructivePatches(); //REMOVEAT WILL CAUSE RANGE ERROR. ADD transpile needs new style. 
			
			//Simple
			AttackTargetFinder_Patch.RunNonDestructivePatches();
			BFSWorker_Patch.RunNonDestructivePatches();
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
			HaulAIUtility_Patch.RunNonDestructivePatches();
			Pawn_InteractionsTracker_Transpile.RunNonDestructivePatches();
			Pawn_MeleeVerbs_Patch.RunNonDestructivePatches();
			Pawn_WorkSettings_Patch.RunNonDestructivePatches();
			PawnsFinder_Patch.RunNonDestructivePatches();
			PawnDiedOrDownedThoughtsUtility_Patch.RunNonDestructivePatches();
			RCellFinder_Patch.RunNonDestructivePatches();
			RegionListersUpdater_Patch.RunNonDestructivePatches();
			RegionTraverser_Patch.RunNonDestructivePatches();
			ThinkNode_PrioritySorter_Patch.RunNonDestructivePatches();
			TickList_Transpile.RunNonDestructivePatches();
			Verb_Patch.RunNonDestructivePatches();
			World_Patch.RunNonDestructivePatches();


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
			SituationalThoughtHandler_Patch.RunNonDestructivePatches();
			WealthWatcher_Patch.RunNonDestructivePatches();
			WorkGiver_ConstructDeliverResources_Transpile.RunNonDestructivePatches(); //reexamine complexity
			WorkGiver_DoBill_Transpile.RunNonDestructivePatches(); //better way to find bills with cache




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
			ContentFinder_Texture2D_Patch.RunDestructivePatches();
			DrugAIUtility_Patch.RunDestructivePatches();
			DynamicDrawManager_Patch.RunDestructivePatches();
			GenClosest_Patch.RunDestructivePatches();
			GenTemperature_Patch.RunDestructivePatches();
			ListerThings_Patch.RunDestructivePatches();
			JobMaker_Patch.RunDestructivePatches();
			MaterialPool_Patch.RunDestructivePatches();
			MemoryThoughtHandler_Patch.RunDestructivePatches();
			Pawn_PlayerSettings_Patch.RunDestructivePatches();
			PawnUtility_Patch.RunDestructivePatches();
			PawnDestinationReservationManager_Patch.RunDestructivePatches();
			PortraitRenderer_Patch.RunDestructivePatches();
			PhysicalInteractionReservationManager_Patch.RunDestructivePatches(); //TODO: write ExposeData and change concurrentdictionary
			Reachability_Patch.RunDestructivePatches();
			ReachabilityCache_Patch.RunDestructivePatches();
			RealtimeMoteList_Patch.RunDestructivePatches();
			RegionDirtyer_Patch.RunDestructivePatches();
			SampleSustainer_Patch.RunDestructivePatches();
			ShootLeanUtility_Patch.RunDestructivePatches(); //TODO: excessive locks, therefore RimThreadedHarmony.Prefix, conncurrent_queue could be transpiled in
			SubSustainer_Patch.RunDestructivePatches();
			SustainerManager_Patch.RunDestructivePatches();
			TaleManager_Patch.RunDestructivePatches();
			ThingGrid_Patch.RunDestructivePatches();
			TickList_Patch.RunDestructivePatches();
			TickManager_Patch.RunDestructivePatches();
			WealthWatcher_Patch.RunDestructivePatches();
			WorkGiver_GrowerSow_Patch.RunDestructivePatches();

			//check methods for unneccessary try catches
			SoundStarter_Patch.RunDestructivePatches();
			Pawn_RelationsTracker_Patch.RunDestructivePatches();
			Battle_Patch.RunDestructivePatches();
			Building_Door_Patch.RunDestructivePatches();
			ThoughtHandler_Patch.RunDestructivePatches();
			Projectile_Patch.RunDestructivePatches();
			AttackTargetReservationManager_Patch.RunDestructivePatches();
			PawnCollisionTweenerUtility_Patch.RunDestructivePatches();
			ReservationManager_Patch.RunDestructivePatches();
			FloodFiller_Patch.RunDestructivePatches();//FloodFiller - inefficient global lock
			MapPawns_Patch.RunDestructivePatches();
			MapTemperature_Patch.RunDestructivePatches();
			Region_Patch.RunDestructivePatches();
			Sample_Patch.RunDestructivePatches();
			Sustainer_Patch.RunDestructivePatches();
			ImmunityHandler_Patch.RunDestructivePatches();
			Room_Patch.RunDestructivePatches();
			LongEventHandler_Patch.RunDestructivePatches();
			SituationalThoughtHandler_Patch.RunDestructivePatches();
			LordToil_Siege_Patch.RunDestructivePatches();
			PawnCapacitiesHandler_Patch.RunDestructivePatches();
			PawnPath_Patch.RunDestructivePatches();
			GenCollection_Patch.RunDestructivePatches();
			SoundSizeAggregator_Patch.RunDestructivePatches();
			HediffSet_Patch.RunDestructivePatches();
			LanguageWordInfo_Patch.RunDestructivePatches();
			JobGiver_ConfigurableHostilityResponse_Patch.RunDestructivePatches();
			Toils_Ingest_Patch.RunDestructivePatches();
			BeautyUtility_Patch.RunDestructivePatches();
			TendUtility_Patch.RunDestructivePatches();
			WanderUtility_Patch.RunDestructivePatches();
			RegionAndRoomUpdater_Patch.RunDestructivePatches();
			Medicine_Patch.RunDestructivePatches();
			JobGiver_Work_Patch.RunDestructivePatches();
			ThingCountUtility_Patch.RunDestructivePatches();
			BiomeDef_Patch.RunDestructivePatches();
			WildPlantSpawner_Patch.RunDestructivePatches();
			TileTemperaturesComp_Patch.RunDestructivePatches();
			PawnRelationUtility_Patch.RunDestructivePatches();
			SustainerAggregatorUtility_Patch.RunDestructivePatches();
			StoryState_Patch.RunDestructivePatches();
			GrammarResolver_Patch.RunDestructivePatches();
			JobQueue_Patch.RunDestructivePatches();
			MeditationFocusTypeAvailabilityCache_Patch.RunDestructivePatches();
			LightningBoltMeshMaker_Patch.RunDestructivePatches();
			TimeControls_Patch.RunDestructivePatches();
			GlobalControlsUtility_Patch.RunDestructivePatches();
			RegionCostCalculator_Patch.RunDestructivePatches();
			RegionCostCalculatorWrapper_Patch.RunDestructivePatches();
			GUIStyle_Patch.RunDestructivePatches();
			WorldGrid_Patch.RunDestructivePatches();
			ReservationUtility_Patch.RunDestructivePatches();
			WorldFloodFiller_Patch.RunDestructivePatches();
			RecipeWorkerCounter_Patch.RunDestructivePatches();
			Pawn_RotationTracker_Patch.RunDestructivePatches();
			GrammarResolverSimpleStringExtensions_Patch.RunDestructivePatches();
			Pawn_HealthTracker_Patch.RunDestructivePatches();
			Pawn_Patch.RunDestructivePatches();
			Pawn_JobTracker_Patch.RunDestructivePatches();
			JobGiver_OptimizeApparel_Patch.RunDestructivePatches();
			HediffGiver_Heat_Patch.RunDestructivePatches();
			Pawn_MindState_Patch.RunDestructivePatches();
			WorldObjectsHolder_Patch.RunDestructivePatches();
			WorldPawns_Patch.RunDestructivePatches();
			SteadyEnvironmentEffects_Patch.RunDestructivePatches();
			WindManager_Patch.RunDestructivePatches();
			FactionManager_Patch.RunDestructivePatches();
			SeasonUtility_Patch.RunDestructivePatches();
			TradeShip_Patch.RunDestructivePatches();
			DateNotifier_Patch.RunDestructivePatches();
			WorldComponentUtility_Patch.RunDestructivePatches();
			Map_Patch.RunDestructivePatches();
			ThinkNode_SubtreesByTag_Patch.RunDestructivePatches();
			ThinkNode_QueuedJob_Patch.RunDestructivePatches();
			TemperatureCache_Patch.RunDestructivePatches();
			JobGiver_AnimalFlee_Patch.RunDestructivePatches();
			PlayLog_Patch.RunDestructivePatches();
			ResourceCounter_Patch.RunDestructivePatches();
			UniqueIDsManager_Patch.RunDestructivePatches();
			CompCauseGameCondition_Patch.RunDestructivePatches();
			MapGenerator_Patch.RunDestructivePatches();//MapGenerator (Z-levels)
			RenderTexture_Patch.RunDestructivePatches();//RenderTexture (Giddy-Up)
			Graphics_Patch.RunDestructivePatches();//Graphics (Giddy-Up and others)
			Texture2D_Patch.RunDestructivePatches();//Graphics (Giddy-Up)
			SectionLayer_Patch.RunDestructivePatches();
			GraphicDatabaseHeadRecords_Patch.RunDestructivePatches();
			MeshMakerPlanes_Patch.RunDestructivePatches();
			MeshMakerShadows_Patch.RunDestructivePatches();
			QuestUtility_Patch.RunDestructivePatches();
			Job_Patch.RunDestructivePatches();
			RestUtility_Patch.RunDestructivePatches();
			Lord_Patch.RunDestructivePatches();
			LordManager_Patch.RunDestructivePatches();
			ThingOwnerUtility_Patch.RunDestructivePatches();
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