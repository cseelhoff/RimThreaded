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
			//finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
			return finalCodeInstructions;
		}

		static RimThreadedHarmony()
		{
			Harmony.DEBUG = false;
			Log.Message("RimThreaded " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "  is patching methods...");
			Type original = null;
			Type patched = null;

			//PlayDataLoader
			//original = typeof(PlayDataLoader);
			//patched = typeof(PlayDataLoader_Patch);
			//Prefix(original, patched, "DoPlayLoad");

			//TickManager			
			original = typeof(TickManager);
			patched = typeof(TickManager_Patch);
			Prefix(original, patched, "DoSingleTick");
			Prefix(original, patched, "get_TickRateMultiplier");

			//ContentFinderTexture2D			
			original = typeof(ContentFinder<Texture2D>);
			patched = typeof(ContentFinder_Texture2D_Patch);
			Prefix(original, patched, "Get");

			//ContentFinderTexture2D
			original = typeof(MaterialPool);
			patched = typeof(MaterialPool_Patch);
			Prefix(original, patched, "MatFrom", new Type[] { typeof(MaterialRequest) });

			//TickList			
			original = typeof(TickList);
			patched = typeof(TickList_Patch);
			Prefix(original, patched, "Tick");
			patched = typeof(TickList_Transpile);
			Transpile(original, patched, "RegisterThing");
			Transpile(original, patched, "DeregisterThing");

			//Rand
			original = typeof(Rand);
			//patched = typeof(Rand_Patch);
			//Prefix(original, patched, "set_Seed");
			//Prefix(original, patched, "get_Value");
			//Prefix(original, patched, "EnsureStateStackEmpty");
			//Prefix(original, patched, "get_Int");
			//Prefix(original, patched, "PopState");
			//Prefix(original, patched, "TryRangeInclusiveWhere");
			//Prefix(original, patched, "PushState", Type.EmptyTypes);
			patched = typeof(Rand_Transpile);
			Transpile(original, patched, "PushState", Type.EmptyTypes);
			Transpile(original, patched, "PopState");
			Transpile(original, patched, "TryRangeInclusiveWhere");

			//ThingOwner<Thing>
			original = typeof(ThingOwner<Thing>);
			patched = typeof(ThingOwnerThing_Transpile);
			Transpile(original, patched, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
			Transpile(original, patched, "Remove");
			//patched = typeof(ThingOwnerThing_Patch);
			//Prefix(original, patched, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
			//Prefix(original, patched, "Remove");

			//RegionListersUpdater
			original = typeof(RegionListersUpdater);
			patched = typeof(RegionListersUpdater_Patch);
			Prefix(original, patched, "DeregisterInRegions");
			Prefix(original, patched, "RegisterInRegions");
			Prefix(original, patched, "RegisterAllAt");
			//patched = typeof(RegionListersUpdater_Transpile);
			//Transpile(original, patched, "DeregisterInRegions");
			//Transpile(original, patched, "RegisterInRegions");
			//Transpile(original, patched, "RegisterAllAt");


			//ListerThings
			original = typeof(ListerThings);
			patched = typeof(ListerThings_Patch);
			Prefix(original, patched, "ThingsOfDef"); //maybe modify instead: JoyGiver_TakeDrug.BestIngestItem...  List<Thing> list = pawn.Map.listerThings.ThingsOfDef(JoyGiver_TakeDrug.takeableDrugs[k]);
													  //Prefix(original, patched, "Remove");
													  //Prefix(original, patched, "Add");
													  //patched = typeof(ListerThings_Transpile);
													  //Transpile(original, patched, "Remove");
													  //Transpile(original, patched, "Add");

			//Thing
			original = typeof(Thing);
			patched = typeof(Thing_Transpile);
			Transpile(original, patched, "SpawnSetup");
			Transpile(original, patched, "DeSpawn");
			Transpile(original, patched, "get_FlammableNow");
			patched = typeof(Thing_Patch);
			Postfix(original, patched, "SpawnSetup");

			//JobMaker
			original = typeof(JobMaker);
			patched = typeof(JobMaker_Patch);
			Prefix(original, patched, "MakeJob", new Type[] { });
			Prefix(original, patched, "ReturnToPool");

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
			

			original = TypeByName("Verse.RegionTraverser+BFSWorker");
			patched = typeof(BFSWorker_Transpile);
			Transpile(original, patched, "QueueNewOpenRegion");
			Transpile(original, patched, "BreadthFirstTraverseWork");

			//ThinkNode_Priority
			original = typeof(ThinkNode_Priority);
			patched = typeof(ThinkNode_Priority_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThinkNode_PrioritySorter
			original = typeof(ThinkNode_PrioritySorter);
			//patched = typeof(ThinkNode_PrioritySorter_Patch);
			//Prefix(original, patched, "TryIssueJobPackage");
			patched = typeof(ThinkNode_PrioritySorter_Transpile);
			Transpile(original, patched, "TryIssueJobPackage");

			//ThingGrid
			original = typeof(ThingGrid);

			//ThingGrid_Transpile
			//patched = typeof(ThingGrid_Transpile);
			//harmony.Patch(Method(original, "ThingAt", new Type[] { typeof(IntVec3) }), transpiler: new HarmonyMethod(Method(patched, "ThingAt")));
			

			patched = typeof(ThingGrid_Patch);
			Prefix(original, patched, "RegisterInCell");
			Prefix(original, patched, "DeregisterInCell");
			Prefix(original, patched, "ThingsAt");
			Prefix(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingCategory) });
			Prefix(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingDef) });

			Type original2 = typeof(ThingGrid);
			Type patched2 = typeof(ThingGrid_Patch);
			MethodInfo originalBuilding_DoorThingAt = Method(original2, "ThingAt", new Type[] { typeof(IntVec3) }, null);
			MethodInfo originalBuilding_DoorThingAtGeneric = originalBuilding_DoorThingAt.MakeGenericMethod(typeof(Building_Door));
			MethodInfo patchedBuilding_DoorThingAt = Method(patched2, "ThingAt_Building_Door");
			HarmonyMethod prefixBuilding_Door = new HarmonyMethod(patchedBuilding_DoorThingAt);
			harmony.Patch(originalBuilding_DoorThingAtGeneric, prefix: prefixBuilding_Door);

			//FloatMenuMakerMap
			original = typeof(FloatMenuMakerMap);
			//patched = typeof(FloatMenuMakerMap_Patch);
			//Prefix(original, patched, "AddHumanlikeOrders");
			patched = typeof(FloatMenuMakerMap_Transpile);
			Transpile(original, patched, "AddHumanlikeOrders");

			//RealtimeMoteList			
			original = typeof(RealtimeMoteList);
			patched = typeof(RealtimeMoteList_Patch);
			Prefix(original, patched, "Clear");
			Prefix(original, patched, "MoteSpawned");
			Prefix(original, patched, "MoteDespawned");
			Prefix(original, patched, "MoteListUpdate");

			//RCellFinder			
			original = typeof(RCellFinder);
			patched = typeof(RCellFinder_Patch); //TODO add threadstatics and transpile in
			Prefix(original, patched, "RandomWanderDestFor");

			//GenSpawn
			original = typeof(GenSpawn);
			patched = typeof(GenSpawn_Patch);
			Prefix(original, patched, "WipeExistingThings");
			Prefix(original, patched, "CheckMoveItemsAside");

			//PawnDestinationReservationManager
			original = typeof(PawnDestinationReservationManager);
			patched = typeof(PawnDestinationReservationManager_Patch);
			Prefix(original, patched, "GetPawnDestinationSetFor");
			Prefix(original, patched, "IsReserved", new Type[] { typeof(IntVec3), typeof(Pawn).MakeByRefType() });
			Prefix(original, patched, "Notify_FactionRemoved");
			Prefix(original, patched, "DebugDrawReservations");
			Prefix(original, patched, "Reserve");
			Prefix(original, patched, "ObsoleteAllClaimedBy");
			Prefix(original, patched, "ReleaseAllObsoleteClaimedBy");
			Prefix(original, patched, "ReleaseAllClaimedBy");
			Prefix(original, patched, "ReleaseClaimedBy");
			Prefix(original, patched, "CanReserve");
			Prefix(original, patched, "FirstObsoleteReservationFor");
			Prefix(original, patched, "MostRecentReservationFor");

			//DynamicDrawManager
			original = typeof(DynamicDrawManager);
			patched = typeof(Verse_DynamicDrawManager_Patch);
			Prefix(original, patched, "RegisterDrawable");
			Prefix(original, patched, "DeRegisterDrawable");
			Prefix(original, patched, "DrawDynamicThings");
			Prefix(original, patched, "LogDynamicDrawThings");

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

			//JobDriver_Wait
			original = typeof(JobDriver_Wait);
			patched = typeof(JobDriver_Wait_Transpile);
			Transpile(original, patched, "CheckForAutoAttack");

			//SelfDefenseUtility
			original = typeof(SelfDefenseUtility);
			patched = typeof(SelfDefenseUtility_Patch);
			Prefix(original, patched, "ShouldStartFleeing");

			//GenClosest
			original = typeof(GenClosest);
			patched = typeof(GenClosest_Patch);
			Prefix(original, patched, "RegionwiseBFSWorker");
			//Prefix(original, patched, "ClosestThingReachable"); //Debugging
			//Prefix(original, patched, "ClosestThing_Global"); //Debugging

			//PawnUtility
			original = typeof(PawnUtility);
			patched = typeof(PawnUtility_Patch);
			Prefix(original, patched, "PawnBlockingPathAt");
			Prefix(original, patched, "EnemiesAreNearby");
			Prefix(original, patched, "ForceWait");

			//CellFinder
			original = typeof(CellFinder);
			patched = typeof(CellFinder_Patch);
			Prefix(original, patched, "TryFindRandomCellInRegion");
			Prefix(original, patched, "TryFindRandomReachableCellNear");
			Prefix(original, patched, "TryFindBestPawnStandCell");
			Prefix(original, patched, "TryFindRandomCellNear");
			Prefix(original, patched, "TryFindRandomCellInsideWith");
			Prefix(original, patched, "TryFindRandomEdgeCellWith", new Type[] { typeof(Predicate<IntVec3>), typeof(Map), typeof(float), typeof(IntVec3).MakeByRefType() });
			Prefix(original, patched, "TryFindRandomEdgeCellWith", new Type[] { typeof(Predicate<IntVec3>), typeof(Map), typeof(Rot4), typeof(float), typeof(IntVec3).MakeByRefType() });
			Prefix(original, patched, "FindNoWipeSpawnLocNear");
			Prefix(original, patched, "RandomRegionNear");

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
			Prefix(original, patched, "Notify_FactionHostilityChanged");
			Prefix(original, patched, "TargetsHostileToFaction");
			Prefix(original, patched, "UpdateTarget");

			//PawnsFinder
			original = typeof(PawnsFinder);
			patched = typeof(PawnsFinder_Patch);
			Prefix(original, patched, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists");
			Prefix(original, patched, "get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists");
			Prefix(original, patched, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners");
			Prefix(original, patched, "get_AllMapsWorldAndTemporary_Alive");

			//PawnDiedOrDownedThoughtsUtility
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			patched = typeof(PawnDiedOrDownedThoughtsUtility_Patch);
			Prefix(original, patched, "RemoveLostThoughts");
			Prefix(original, patched, "RemoveDiedThoughts");
			Prefix(original, patched, "RemoveResuedRelativeThought");

			//AttackTargetFinder
			original = typeof(AttackTargetFinder);
			patched = typeof(AttackTargetFinder_Transpile);
			Transpile(original, patched, "BestAttackTarget");
			Transpile(original, patched, "CanSee");

			//patched = typeof(AttackTargetFinder_Patch);
			//Prefix(original, patched, "GetRandomShootingTargetByScore");
			//Prefix(original, patched, "BestAttackTarget");
			//Prefix(original, patched, "CanSee");

			//ShootLeanUtility
			original = typeof(ShootLeanUtility);
			patched = typeof(ShootLeanUtility_Patch);
			Prefix(original, patched, "LeanShootingSourcesFromTo");

			//BuildableDef
			original = typeof(BuildableDef);
			patched = typeof(BuildableDef_Transpile);
			Transpile(original, patched, "ForceAllowPlaceOver");
			patched = typeof(BuildableDef_Patch);
			Prefix(original, patched, "get_PlaceWorkers");


			//SustainerManager			
			original = typeof(SustainerManager);
			patched = typeof(SustainerManager_Patch);
			//Prefix(original, patched, "get_AllSustainers");
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
			//Prefix(original, patched, "RecreateMapSustainers");
			Prefix(original, patched, "EnsureWorldAmbientSoundCreated");

			//SubSustainer
			original = typeof(SubSustainer);
			patched = typeof(SubSustainer_Patch);
			//Prefix(original, patched, "StartSample");
			Prefix(original, patched, "SubSustainerUpdate");

			//SoundStarter
			original = typeof(SoundStarter);
			patched = typeof(SoundStarter_Patch);
			Prefix(original, patched, "PlayOneShot");
			Prefix(original, patched, "PlayOneShotOnCamera");
			//Prefix(original, patched, "TrySpawnSustainer");

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
			patched = typeof(Building_Door_Transpile);
			Transpile(original, patched, "get_BlockedOpenMomentary");

			//ThoughtHandler						
			original = typeof(ThoughtHandler);
			patched = typeof(ThoughtHandler_Patch);
			Prefix(original, patched, "TotalOpinionOffset");
			Prefix(original, patched, "MoodOffsetOfGroup");
			Prefix(original, patched, "TotalMoodOffset");
			Prefix(original, patched, "OpinionOffsetOfGroup");
			
			//Fire			
			original = typeof(Fire);
			patched = typeof(Fire_Transpile);
			Transpile(original, patched, "DoComplexCalcs");

			//Projectile			
			original = typeof(Projectile);
			patched = typeof(Projectile_Patch);
			Prefix(original, patched, "ImpactSomething");
			Prefix(original, patched, "CanHit");
			Prefix(original, patched, "CheckForFreeInterceptBetween");
			Prefix(original, patched, "CheckForFreeIntercept");

			//GenGrid_Patch			
			original = typeof(GenGrid);
			patched = typeof(GenGrid_Patch);
			Prefix(original, patched, "InBounds", new Type[] { typeof(IntVec3), typeof(Map) });
			Prefix(original, patched, "Standable");
			Prefix(original, patched, "Walkable");

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
			//patched = typeof(AttackTargetReservationManager_Transpile);
			//Transpile(original, patched, "IsReservedBy"); changed back to prefix for now TODO
			//Transpile(original, patched, "Reserve"); changed back to prefix for now TODO


			//PawnCollisionTweenerUtility
			original = typeof(PawnCollisionTweenerUtility);
			patched = typeof(PawnCollisionTweenerUtility_Patch);
			Prefix(original, patched, "GetPawnsStandingAtOrAboutToStandAt");
			Prefix(original, patched, "CanGoDirectlyToNextCell");

			//GridsUtility			
			original = typeof(GridsUtility);
			patched = typeof(GridsUtility_Patch);
			Prefix(original, patched, "GetTerrain");
			Prefix(original, patched, "IsInPrisonCell");
			Prefix(original, patched, "GetThingList");
			patched = typeof(GridsUtility_Transpile);
			Transpile(original, patched, "GetGas");

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

			patched = typeof(ReservationManager_Transpile);
			//Transpile(original, patched, "CanReserve");

			//FloodFiller - inefficient global lock			
			original = typeof(FloodFiller);
			patched = typeof(FloodFiller_Patch);
			Prefix(original, patched, "FloodFill", new Type[] { typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, int, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>) });

			//Verb
			original = typeof(Verb);
			patched = typeof(Verb_Patch);
			Prefix(original, patched, "get_DirectOwner");
			patched = typeof(Verb_Transpile);
			Transpile(original, patched, "TryFindShootLineFromTo");
			Transpile(original, patched, "CanHitFromCellIgnoringRange");

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

			//MethodInfo oMethodRegisterPawn = Method(original, "RegisterPawn");
			//MethodInfo pMethodRegisterPawn_FreeHumanlikesSpawnedOfFaction = Method(patched, "RegisterPawn_FreeHumanlikesSpawnedOfFaction");
			//harmony.Patch(oMethodRegisterPawn, postfix: new HarmonyMethod(pMethodRegisterPawn_FreeHumanlikesSpawnedOfFaction));

			//MethodInfo oMethodDeRegisterPawn = Method(original, "DeRegisterPawn");
			//MethodInfo pMethodDeRegisterPawn_FreeHumanlikesSpawnedOfFaction = Method(patched, "DeRegisterPawn_FreeHumanlikesSpawnedOfFaction");
			//harmony.Patch(oMethodDeRegisterPawn, postfix: new HarmonyMethod(pMethodDeRegisterPawn_FreeHumanlikesSpawnedOfFaction));

			//patched = typeof(MapPawns_Transpile);
			//Transpile(original, patched, "RegisterPawn");
			//Transpile(original, patched, "DeRegisterPawn");

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

			//Pawn_WorkSettings
			original = typeof(Pawn_WorkSettings);
			patched = typeof(Pawn_WorkSettings_Transpile);
			Transpile(original, patched, "CacheWorkGiversInOrder");

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
			ConstructorInfo constructorMethod3 = original.GetConstructor(new Type[] { typeof(Pawn) });
			MethodInfo cpMethod3 = patched.GetMethod("Postfix_Constructor");
			harmony.Patch(constructorMethod3, postfix: new HarmonyMethod(cpMethod3));

			//GenAdjFast
			original = typeof(GenAdjFast);
			patched = typeof(GenAdjFast_Patch);
			Prefix(original, patched, "AdjacentCells8Way", new Type[] { typeof(IntVec3) });
			Prefix(original, patched, "AdjacentCells8Way", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
			Prefix(original, patched, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3) });
			Prefix(original, patched, "AdjacentCellsCardinal", new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });

			//GenAdj
			original = typeof(GenAdj);
			patched = typeof(GenAdj_Patch);
			Prefix(original, patched, "TryFindRandomAdjacentCell8WayWithRoomGroup", new Type[] {
				typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(Map), typeof(IntVec3).MakeByRefType() });

			//LordToil_Siege
			original = typeof(LordToil_Siege);
			patched = typeof(LordToil_Siege_Patch);
			Prefix(original, patched, "UpdateAllDuties");

			//BattleLog
			original = typeof(BattleLog);
			patched = typeof(BattleLog_Transpile);
			Transpile(original, patched, "Add");

			//PawnCapacitiesHandler
			original = typeof(PawnCapacitiesHandler);
			patched = typeof(PawnCapacitiesHandler_Patch);
			Prefix(original, patched, "Notify_CapacityLevelsDirty");
			Prefix(original, patched, "Clear");
			Prefix(original, patched, "CapableOf");
			ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
			MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
			harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));
			patched = typeof(PawnCapacitiesHandler_Transpile);
			Transpile(original, patched, "GetLevel");

			//PathFinder
			original = typeof(PathFinder);
			patched = typeof(PathFinder_Transpile);
			Transpile(original, patched, "FindPath", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) });

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

			//HediffSet
			//patched = typeof(HediffSet_Transpile);
			//original = TypeByName("Verse.HediffSet+<GetNotMissingParts>d__40");
			//Transpile(original, patched, "MoveNext");
			original = typeof(HediffSet);
			//Transpile(original, patched, "GetPartHealth");
			//Transpile(original, patched, "AddDirect"); TODO re-add transpile
			//Transpile(original, patched, "CacheMissingPartsCommonAncestors");
			patched = typeof(HediffSet_Patch);
			Prefix(original, patched, "AddDirect");
			//Prefix(original, patched, "PartIsMissing");
			//Prefix(original, patched, "HasDirectlyAddedPartFor");
			//Prefix(original, patched, "GetFirstHediffOfDef");
			//Prefix(original, patched, "HasTendableHediff");
			//Prefix(original, patched, "HasImmunizableNotImmuneHediff");
			Postfix(original, patched, "DirtyCache", "DirtyCacheSetInvisbility");

			//LanguageWordInfo
			original = typeof(LanguageWordInfo);
			patched = typeof(LanguageWordInfo_Patch);
			Prefix(original, patched, "TryResolveGender");

			//JobGiver_ConfigurableHostilityResponse
			original = typeof(JobGiver_ConfigurableHostilityResponse);
			patched = typeof(JobGiver_ConfigurableHostilityResponse_Patch);
			Prefix(original, patched, "TryGetFleeJob");


			//Pawn_InteractionsTracker
			original = typeof(Pawn_InteractionsTracker);
			patched = typeof(Pawn_InteractionsTracker_Transpile);
			Transpile(original, patched, "TryInteractRandomly");

			//Toils_Ingest
			original = typeof(Toils_Ingest);
			patched = typeof(Toils_Ingest_Patch);
			Prefix(original, patched, "TryFindAdjacentIngestionPlaceSpot");

			//BeautyUtility
			original = typeof(BeautyUtility);
			patched = typeof(BeautyUtility_Patch);
			Prefix(original, patched, "AverageBeautyPerceptible");

			//FoodUtility
			original = typeof(FoodUtility);
			patched = typeof(FoodUtility_Transpile);
			Transpile(original, patched, "FoodOptimality");

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

			//GenRadial
			original = typeof(GenRadial);
			patched = typeof(GenRadial_Transpile);
			Transpile(original, patched, "ProcessEquidistantCells");

			//WealthWatcher
			original = typeof(WealthWatcher);
			patched = typeof(WealthWatcher_Patch);
			Prefix(original, patched, "ForceRecount");

			//Medicine
			original = typeof(Medicine);
			patched = typeof(Medicine_Patch);
			Prefix(original, patched, "GetMedicineCountToFullyHeal");

			//WorkGiver_DoBill
			original = typeof(WorkGiver_DoBill);
			patched = typeof(WorkGiver_DoBill_Transpile);
			Transpile(original, patched, "TryFindBestBillIngredients");
			Transpile(original, patched, "AddEveryMedicineToRelevantThings");

			//JobGiver_Work
			original = typeof(JobGiver_Work);
			patched = typeof(JobGiver_Work_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThingCountUtility
			original = typeof(ThingCountUtility);
			patched = typeof(ThingCountUtility_Patch);
			Prefix(original, patched, "AddToList");

			//WorkGiver_ConstructDeliverResources
			original = typeof(WorkGiver_ConstructDeliverResources);
			//patched = typeof(WorkGiver_ConstructDeliverResources_Patch);
			patched = typeof(WorkGiver_ConstructDeliverResources_Transpile);
			Transpile(original, patched, "ResourceDeliverJobFor", new string[] { "CodeOptimist.JobsOfOpportunity" });

			//GenText
			original = typeof(GenText);
			patched = typeof(GenText_Patch);
			Prefix(original, patched, "CapitalizeSentences");

			//BiomeDef
			original = typeof(BiomeDef);
			patched = typeof(BiomeDef_Patch);
			Prefix(original, patched, "CachePlantCommonalitiesIfShould");
			Prefix(original, patched, "get_LowestWildAndCavePlantOrder");

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
			//patched = typeof(TileTemperaturesComp_Transpile);
			//Transpile(original, patched, "WorldComponentTick");

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

			//GrammarResolverSimple
			original = typeof(GrammarResolverSimple);
			patched = typeof(GrammarResolverSimple_Transpile);
			Transpile(original, patched, "Formatted");

			//GrammarResolver
			original = typeof(GrammarResolver);
			patched = typeof(GrammarResolver_Patch);
			Prefix(original, patched, "ResolveUnsafe", new Type[] { typeof(string), typeof(GrammarRequest), typeof(bool).MakeByRefType(), typeof(string), typeof(bool), typeof(bool), typeof(List<string>), typeof(List<string>), typeof(bool) });
			patched = typeof(GrammarResolver_Transpile);
			Transpile(original, patched, "AddRule");
			Transpile(original, patched, "RandomPossiblyResolvableEntry");
			original = TypeByName("Verse.Grammar.GrammarResolver+<>c__DisplayClass17_0");
			MethodInfo oMethod = Method(original, "<RandomPossiblyResolvableEntry>b__0");
			MethodInfo pMethod = Method(patched, "RandomPossiblyResolvableEntryb__0");
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));

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

			//PathGrid
			original = typeof(PathGrid);
			patched = typeof(PathGrid_Patch);
			Prefix(original, patched, "CalculatedCostAt");

			//GlobalControlsUtility
			original = typeof(GlobalControlsUtility);
			patched = typeof(GlobalControlsUtility_Patch);
			Postfix(original, patched, "DoTimespeedControls");

			//InfestationCellFinder
			original = typeof(InfestationCellFinder);
			patched = typeof(InfestationCellFinder_Patch);
			Prefix(original, patched, "CalculateDistanceToColonyBuildingGrid");
			Prefix(original, patched, "GetScoreAt");

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

			//GenLabel
			original = typeof(GenLabel);
			patched = typeof(GenLabel_Transpile);
			//Transpile(original, patched, "ThingLabel", new Type[] { typeof(BuildableDef), typeof(ThingDef), typeof(int) }); causes Threadlock... JobDriver.TryActuallyStartNextToil? ThingOwnerTryAddOrTransfer? ThingOwner.TryAdd? GrammarResolverSimple.Formatted? GrammarResolverSimpleStringExtentions_Patch.Formatted? 
			//Transpile(original, patched, "ThingLabel", new Type[] { typeof(Thing), typeof(int), typeof(bool) });

			//Pawn_PathFollower
			original = typeof(Pawn_PathFollower);
			patched = typeof(Pawn_PathFollower_Transpile);
			Transpile(original, patched, "StartPath");

			//CompSpawnSubplant
			original = typeof(CompSpawnSubplant);
			patched = typeof(CompSpawnSubplant_Transpile);
			Transpile(original, patched, "DoGrowSubplant");

			//PawnCapacityUtility
			//original = typeof(PawnCapacityUtility);
			//patched = typeof(PawnCapacityUtility_Patch);
			//Prefix(original, patched, "CalculatePartEfficiency"); //not sure why this was made

			//ColoredText
			original = typeof(ColoredText);
			patched = typeof(ColoredText_Transpile);
			Transpile(original, patched, "Resolve");

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

			//HediffGiver_Hypothermia
			original = typeof(HediffGiver_Hypothermia);
			//patched = typeof(HediffGiver_Hypothermia_Patch);
			//Prefix(original, patched, "OnIntervalPassed");
			patched = typeof(HediffGiver_Hypothermia_Transpile);
			Transpile(original, patched, "OnIntervalPassed");

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

			//GenTemperature			
			original = typeof(GenTemperature);
			patched = typeof(GenTemperature_Patch);
			Prefix(original, patched, "GetTemperatureFromSeasonAtTile");
			Prefix(original, patched, "SeasonalShiftAmplitudeAt");
			Prefix(original, patched, "EqualizeTemperaturesThroughBuilding");
			Prefix(original, patched, "PushHeat", new Type[] { typeof(IntVec3), typeof(Map), typeof(float) });

			//WorldComponentUtility			
			original = typeof(WorldComponentUtility);
			patched = typeof(WorldComponentUtility_Patch);
			Prefix(original, patched, "WorldComponentTick");

			//Map			
			original = typeof(Map);
			patched = typeof(Map_Patch);
			//Prefix(original, patched, "MapUpdate");
			Prefix(original, patched, "get_IsPlayerHome");
			patched = typeof(Map_Transpile);
			Transpile(original, patched, "MapUpdate");

			//ThinkNode_SubtreesByTag			
			original = typeof(ThinkNode_SubtreesByTag);
			patched = typeof(ThinkNode_SubtreesByTag_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThinkNode_QueuedJob			
			original = typeof(ThinkNode_QueuedJob);
			patched = typeof(ThinkNode_QueuedJob_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//JobDriver			
			original = typeof(JobDriver);
			patched = typeof(JobDriver_Patch);
			Prefix(original, patched, "TryActuallyStartNextToil");
			Prefix(original, patched, "DriverTick");

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
			patched = typeof(CompCauseGameCondition_Patch); //TODO causedConditions is not thread-safe
			Prefix(original, patched, "CompTick");

			//MapGenerator (Z-levels)
			original = typeof(MapGenerator);
			patched = typeof(MapGenerator_Patch);
			Prefix(original, patched, "GenerateMap");

			//RenderTexture (Giddy-Up)
			original = typeof(RenderTexture);
			patched = typeof(RenderTexture_Patch);
			//Prefix(original, patched, "GetTemporary", new Type[] { typeof(int), typeof(int), typeof(int), typeof(RenderTextureFormat), typeof(RenderTextureReadWrite) });
			Prefix(original, patched, "GetTemporaryImpl");

			//GetTemporary (CE)
			//Prefix(original, patched, "GetTemporary", new Type[] { typeof(int), typeof(int), typeof(int), typeof(RenderTextureFormat), typeof(RenderTextureReadWrite), typeof(int) });
			//Prefix(original, patched, "get_active");
			//Prefix(original, patched, "set_active");

			//Graphics (Giddy-Up and others)
			original = typeof(Graphics);
			patched = typeof(Graphics_Patch);
			Prefix(original, patched, "Blit", new Type[] { typeof(Texture), typeof(RenderTexture) });
			Prefix(original, patched, "DrawMesh", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });

			//Graphics (Giddy-Up)
			original = typeof(Texture2D);
			patched = typeof(Texture2D_Patch);
			//Prefix(original, patched, "GetPixel", new Type[] { typeof(int), typeof(int) });
			Prefix(original, patched, "Internal_Create");
			Prefix(original, patched, "ReadPixels", new Type[] { typeof(Rect), typeof(int), typeof(int), typeof(bool) });
			Prefix(original, patched, "Apply", new Type[] { typeof(bool), typeof(bool) });

			//original = typeof(Mesh);
			//patched = typeof(Mesh_Patch);
			//ConstructorInfo constructorMethod4 = original.GetConstructor(Type.EmptyTypes);
			//MethodInfo cpMethod4 = patched.GetMethod("MeshSafe");
			//harmony.Patch(constructorMethod4, prefix: new HarmonyMethod(cpMethod4));

			original = typeof(SectionLayer);
			patched = typeof(SectionLayer_Patch);
			Prefix(original, patched, "GetSubMesh");

			original = typeof(GraphicDatabaseHeadRecords);
			patched = typeof(GraphicDatabaseHeadRecords_Patch);
			Prefix(original, patched, "BuildDatabaseIfNecessary");

			original = typeof(MeshMakerPlanes);
			patched = typeof(MeshMakerPlanes_Patch);
			Prefix(original, patched, "NewPlaneMesh", new Type[] { typeof(Vector2), typeof(bool), typeof(bool), typeof(bool) });

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

			//DamageWorker
			original = typeof(DamageWorker);
			patched = typeof(DamageWorker_Patch);
			Prefix(original, patched, "ExplosionAffectCell");
			Prefix(original, patched, "ExplosionCellsToHit", new Type[] { typeof(IntVec3), typeof(Map), typeof(float), typeof(IntVec3), typeof(IntVec3) });

			//TaleManager_Patch
			original = typeof(TaleManager);
			patched = typeof(TaleManager_Patch);
			Prefix(original, patched, "CheckCullUnusedVolatileTales");

			//Pawn_PlayerSettings
			original = typeof(Pawn_PlayerSettings);
			patched = typeof(Pawn_PlayerSettings_Patch);
			Prefix(original, patched, "set_Master");

			//AlertsReadout
			original = typeof(AlertsReadout);
			patched = typeof(AlertsReadout_Patch);
			Prefix(original, patched, "AlertsReadoutUpdate");

			//ReachabilityCache_Patch
			original = typeof(ReachabilityCache);
			patched = typeof(ReachabilityCache_Patch);
			Prefix(original, patched, "get_Count");
			Prefix(original, patched, "CachedResultFor");
			Prefix(original, patched, "AddCachedResult");
			Prefix(original, patched, "Clear");
			Prefix(original, patched, "ClearFor");
			Prefix(original, patched, "ClearForHostile");

			//RecordWorker_TimeGettingJoy
			original = typeof(RecordWorker_TimeGettingJoy);
			patched = typeof(RecordWorker_TimeGettingJoy_Patch);
			Prefix(original, patched, "ShouldMeasureTimeNow");

			//Building_Trap
			original = typeof(Building_Trap);
			patched = typeof(Building_Trap_Transpile);
			Transpile(original, patched, "Tick");

			//Alert_MinorBreakRisk
			original = typeof(Alert_MinorBreakRisk);
			patched = typeof(Alert_MinorBreakRisk_Patch);
			Prefix(original, patched, "GetReport");

			//FilthMaker
			original = typeof(FilthMaker);
			patched = typeof(FilthMaker_Patch);
			Prefix(original, patched, "TryMakeFilth", new Type[] { typeof(IntVec3), typeof(Map), typeof(ThingDef), typeof(IEnumerable<string>), typeof(bool), typeof(FilthSourceFlags) });

			//SickPawnVisitUtility
			original = typeof(SickPawnVisitUtility);
			patched = typeof(SickPawnVisitUtility_Patch);
			Prefix(original, patched, "FindRandomSickPawn");

			//RoofGrid
			original = typeof(RoofGrid);
			patched = typeof(RoofGrid_Patch);
			Prefix(original, patched, "SetRoof");

			//GenLeaving
			original = typeof(GenLeaving);
			patched = typeof(GenLeaving_Patch);
			Prefix(original, patched, "DropFilthDueToDamage");

			//World
			original = typeof(World);
			patched = typeof(World_Patch);
			Prefix(original, patched, "NaturalRockTypesIn");
			Prefix(original, patched, "CoastDirectionAt");

			//MemoryThoughtHandler
			original = typeof(MemoryThoughtHandler);
			patched = typeof(MemoryThoughtHandler_Patch);
			Prefix(original, patched, "MemoryThoughtInterval");
			Prefix(original, patched, "ExposeData");

			//PortraitRenderer
			original = typeof(PortraitRenderer);
			patched = typeof(PortraitRenderer_Patch);
			Prefix(original, patched, "RenderPortrait");

			//RegionDirtyer
			original = typeof(RegionDirtyer);
			patched = typeof(RegionDirtyer_Patch);
			Prefix(original, patched, "SetAllClean");
			Prefix(original, patched, "Notify_WalkabilityChanged");
			Prefix(original, patched, "Notify_ThingAffectingRegionsSpawned");
			Prefix(original, patched, "Notify_ThingAffectingRegionsDespawned");
			Prefix(original, patched, "SetAllDirty");
			Prefix(original, patched, "SetRegionDirty");

			//HaulAIUtility
			original = typeof(HaulAIUtility);
			patched = typeof(HaulAIUtility_Patch);
			Prefix(original, patched, "TryFindSpotToPlaceHaulableCloseTo");

			//DrugAIUtility
			original = typeof(DrugAIUtility);
			patched = typeof(DrugAIUtility_Patch);
			Prefix(original, patched, "IngestAndTakeToInventoryJob");

			//Verb_MeleeAttack
			//original = typeof(Verb_MeleeAttack);
			//patched = typeof(Verb_MeleeAttack_Patch);
			//Prefix(original, patched, "TryCastShot");

			//Verb_Tracker
			original = typeof(VerbTracker);
			patched = typeof(VerbTracker_Patch);
			Prefix(original, patched, "VerbsTick", false); //TODO loops twice... and removes verblist target == null - could also be transpiled although not ideal

			//ThinkNode_ConditionalAnyColonistTryingToExitMap
			original = typeof(ThinkNode_ConditionalAnyColonistTryingToExitMap);
			patched = typeof(ThinkNode_ConditionalAnyColonistTryingToExitMap_Patch);
			Prefix(original, patched, "Satisfied");

			//GenClamor+<>c__DisplayClass1_0
			original = TypeByName("Verse.GenClamor+<>c__DisplayClass1_0");
			patched = typeof(GenClamor_Patch);
			oMethod = Method(original, "<DoClamor>b__1");
			pMethod = Method(patched, "DoClamorb__1");
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));

			//TODO - should transpile ReplacePotentiallyRelatedPawns instead
			//FocusStrengthOffset_GraveCorpseRelationship.CanApply
			original = typeof(FocusStrengthOffset_GraveCorpseRelationship);
			patched = typeof(Pawn_RelationsTracker_Transpile);
			pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
			harmony.Patch(Method(original, "CanApply"), transpiler: new HarmonyMethod(pMethod));
			//PawnDiedOrDownedThoughtsUtility.AppendThoughts_Relations
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			harmony.Patch(Method(original, "AppendThoughts_Relations"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker.get_RelatedPawns
			original = TypeByName("RimWorld.Pawn_RelationsTracker+<get_RelatedPawns>d__30");
			harmony.Patch(Method(original, "MoveNext"), transpiler: new HarmonyMethod(pMethod));
			original = typeof(Pawn_RelationsTracker);
			//Pawn_RelationsTracker.Notify_PawnKilled
			harmony.Patch(Method(original, "Notify_PawnKilled"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker.Notify_PawnSold
			harmony.Patch(Method(original, "Notify_PawnSold"), transpiler: new HarmonyMethod(pMethod));

			original = typeof(JobGiver_Haul);
			patched = typeof(JobGiver_Haul_Patch);
			//Prefix(original, patched, "TryGiveJob"); debugging

			original = typeof(WorkGiver_Grower);
			patched = typeof(WorkGiver_Grower_Patch);
			//Prefix(original, patched, "WorkGiver_Grower_Patch");

			original = typeof(WorkGiver_GrowerSow);
			patched = typeof(WorkGiver_GrowerSow_Patch);
			Prefix(original, patched, "JobOnCell");

			//JoyGiver_Ingest
			//original = typeof(JoyGiver_Ingest);
			//patched = typeof(JoyGiver_Ingest_Patch);
			//Prefix(original, patched, "BestIngestItem");

			// Resources_Patch
			/* Doesn't work as Load is an external method (without a method body) and can therefor not be prefixed. Transpile would maybe be possible, but I dont think, it's a good idea...
			 * Changed method call to a rimthreaded specific one instead. See Resources_Patch::Load
			original = typeof(Resources);
			patched = typeof(Resources_Patch);
			Prefix(original, patched, "Load", new Type[] { typeof(string), typeof(Type) });
			*/



			//MOD COMPATIBILITY

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
				//Prefix(awesomeInventoryJobsJobGiver_FindItemByRadius, patched, methodName);
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
			if (combatExtended_ProjectileCE != null)
			{
				//string methodName = "CheckCellForCollision";
				//patched = typeof(ProjectileCE_Transpile);
				//Log.Message("RimThreaded is patching " + combatExtended_ProjectileCE.FullName + " " + methodName);
				//Transpile(combatExtended_ProjectileCE, patched, methodName);
				//methodName = "CheckForCollisionBetween";
				//patched = typeof(ProjectileCE_Transpile);
				//Log.Message("RimThreaded is patching " + combatExtended_ProjectileCE.FullName + " " + methodName);
				//Transpile(combatExtended_ProjectileCE, patched, methodName);
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

			Log.Message("RimThreaded patching is complete.");
		}

		public static void Prefix(Type original, Type patched, string methodName, Type[] orig_type)
		{
			MethodInfo oMethod = Method(original, methodName, orig_type);
			Type[] patch_type = new Type[orig_type.Length];
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

			MethodInfo pMethod = patched.GetMethod(methodName, patch_type);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + methodName + "(" + string.Join(",", orig_type.Select(x => x.ToString()).ToArray()) + ") not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + methodName + "(" + string.Join(",", patch_type.Select(x => x.ToString()).ToArray()) + ") not found");
			}
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod));
		}
		public static void Prefix(Type original, Type patched, string methodName, bool destructive = true)
		{
			MethodInfo oMethod = Method(original, methodName);
			MethodInfo pMethod = Method(patched, methodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + methodName + " not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + methodName + " not found");
			}
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod));
			if (!destructive)
			{
				nonDestructivePrefixes.Add(pMethod);
			}
		}
		public static void Prefix(Type original, Type patched, string originalMethodName, string patchedMethodName, bool destructive = true)
		{
			MethodInfo oMethod = Method(original, originalMethodName);
			MethodInfo pMethod = Method(patched, patchedMethodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + originalMethodName + " not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + patchedMethodName + " not found");
			}
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod));
			if (!destructive)
			{
				nonDestructivePrefixes.Add(pMethod);
			}
		}
		public static void Postfix(Type original, Type patched, string methodName)
		{
			MethodInfo oMethod = Method(original, methodName);
			MethodInfo pMethod = Method(patched, methodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + methodName + " not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + methodName + " not found");
			}
			harmony.Patch(oMethod, postfix: new HarmonyMethod(pMethod));
		}
		public static void Postfix(Type original, Type patched, string originalMethodName, string patchedMethodName)
		{
			MethodInfo oMethod = Method(original, originalMethodName);
			MethodInfo pMethod = Method(patched, patchedMethodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + originalMethodName + " not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + patchedMethodName + " not found");
			}
			harmony.Patch(oMethod, postfix: new HarmonyMethod(pMethod));
		}

		public static void Transpile(Type original, Type patched, string methodName)
		{
			MethodInfo oMethod = Method(original, methodName);
			MethodInfo pMethod = Method(patched, methodName);
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));
		}

		public static void Transpile(Type original, Type patched, string methodName, Type[] orig_type)
		{
			MethodInfo oMethod = Method(original, methodName, orig_type);
			Type[] patch_type = new Type[orig_type.Length];
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

			MethodInfo pMethod = Method(patched, methodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + methodName + "(" + string.Join(",", orig_type.Select(x => x.ToString()).ToArray()) + ") not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + methodName + "(" + string.Join(",", patch_type.Select(x => x.ToString()).ToArray()) + ") not found");
			}
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));
		}

		public static void Transpile(Type original, Type patched, string methodName, string[] harmonyAfter)
		{
			MethodInfo oMethod = Method(original, methodName);
			MethodInfo pMethod = Method(patched, methodName);
			HarmonyMethod transpilerMethod = new HarmonyMethod(pMethod)
			{
				after = harmonyAfter
			};
			harmony.Patch(oMethod, transpiler: transpilerMethod);
		}
	}

}