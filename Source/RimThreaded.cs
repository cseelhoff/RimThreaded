using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{
	public class RimThreadedSettings : ModSettings
	{
		public int maxThreads = 8;
		public string maxThreadsBuffer = "8";
		public int timeoutMS = 1000;
		public string timeoutMSBuffer = "1000";
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref maxThreadsBuffer, "maxThreadsBuffer", "8");
			Scribe_Values.Look(ref timeoutMSBuffer, "timeoutMSBuffer", "1000");
		}

		public void DoWindowContents(Rect inRect)
        {
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			Widgets.Label(listing_Standard.GetRect(30f), "Total worker threads (recommendation 1-2 per CPU core):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref maxThreads, ref maxThreadsBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timeout (in miliseconds) waiting for threads (default: 1000):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref timeoutMS, ref timeoutMSBuffer, 10);
			listing_Standard.End();
		}
	}


	class RimThreadedMod : Mod
	{
		public static RimThreadedSettings Settings;
		public RimThreadedMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<RimThreadedSettings>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
			Ticklist_Patch.maxThreads = Math.Max(Settings.maxThreads, 1);
			Ticklist_Patch.timeoutMS = Settings.timeoutMS;
		}

		public override string SettingsCategory()
		{
			return "RimThreaded";

		}

	}
	
	[StaticConstructorOnStartup]
	public class RimThreadedHarmony {

		public static BindingFlags bf = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		public static Harmony harmony = new Harmony("majorhoff.rimthreaded");
		//bugs - remove is out of sync for listerthings and thinggrid and maybe thingownerthing
		//perf impr - replace dicts with hashsets (maybe custom hash function too?)

		static RimThreadedHarmony() {
			Log.Message("RimThreaded Harmony is loading...");
			Type original = null;
			Type patched = null;
			
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
			patched = typeof(Ticklist_Patch);
			Prefix(original, patched, "Tick");

			//Rand
			original = typeof(Rand);
			patched = typeof(Rand_Patch);
			Prefix(original, patched, "set_Seed");
			Prefix(original, patched, "get_Int");
			Prefix(original, patched, "get_Value");
			Prefix(original, patched, "EnsureStateStackEmpty");
			Prefix(original, patched, "PopState");
			Prefix(original, patched, "TryRangeInclusiveWhere");
			Prefix(original, patched, "PushState", new Type[] { });


			//ThingOwner<Thing> - perf improvement - uses slow method invoke / reflection call
			original = typeof(ThingOwner<Thing>);
			patched = typeof(ThingOwnerThing_Patch);
			Prefix(original, patched, "Remove");
			Prefix(original, patched, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });

			//Pawn_PathFollower
			original = typeof(Pawn_PathFollower);
			patched = typeof(Pawn_PathFollower_Patch);
			Prefix(original, patched, "GenerateNewPath");
			//Prefix(original, patched, "NextCellDoorToWaitForOrManuallyOpen");
			//Prefix(original, patched, "SetupMoveIntoNextCell");
			//Prefix(original, patched, "TryEnterNextPathCell");

			//RegionListersUpdater
			original = typeof(RegionListersUpdater);
			patched = typeof(RegionListersUpdater_Patch);
			Prefix(original, patched, "DeregisterInRegions");
			Prefix(original, patched, "RegisterInRegions");

			//ListerThings
			original = typeof(ListerThings);
			patched = typeof(ListerThings_Patch);
			Prefix(original, patched, "Remove");
			Prefix(original, patched, "Add");

			//JobMaker
			original = typeof(JobMaker);
			patched = typeof(JobMaker_Patch);
			Prefix(original, patched, "MakeJob", new Type[] { });
			Prefix(original, patched, "ReturnToPool");

			//RegionTraverser
			original = typeof(RegionTraverser);
			patched = typeof(RegionTraverser_Patch);
			Prefix(original, patched, "BreadthFirstTraverse", new Type[] {
				typeof(Region),
				typeof(RegionEntryPredicate),
				typeof(RegionProcessor),
				typeof(int),
				typeof(RegionType)
			});

			//ThinkNode_Priority
			original = typeof(ThinkNode_Priority);
			patched = typeof(ThinkNode_Priority_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThinkNode_PrioritySorter
			original = typeof(ThinkNode_PrioritySorter);
			patched = typeof(ThinkNode_PrioritySorter_Patch);
			Prefix(original, patched, "TryIssueJobPackage");

			//ThingGrid
			original = typeof(ThingGrid);

			//ThingGrid_Transpile
			patched = typeof(ThingGrid_Transpile);
			//Transpile(original, patched, "RegisterInCell");
			//Transpile(original, patched, "DeregisterInCell");
			//Transpile(original, patched, "ThingsAt");
			//Transpile(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingCategory) });
			//Transpile(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingDef) );
			//MethodInfo originalApparelThingAt = original.GetMethod("ThingAt", bf, null, new Type[] { typeof(IntVec3) }, null);
			
			patched = typeof(ThingGrid_Patch);

			//ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Map) });
			//MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
			//harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));

			Prefix(original, patched, "RegisterInCell");
			Prefix(original, patched, "DeregisterInCell");
			//Prefix(original, patched, "ThingsListAt");
			Prefix(original, patched, "ThingsAt");
			//Prefix(original, patched, "ThingsListAtFast", new Type[] { typeof(int) });
			//Prefix(original, patched, "ThingsListAtFast", new Type[] { typeof(IntVec3) });
			Prefix(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingCategory) });
			Prefix(original, patched, "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingDef) });
			/*
			original = typeof(ThingGrid);
			patched = typeof(ThingGrid_Patch);
			MethodInfo originalApparelThingAt = original.GetMethod("ThingAt", bf, null, new Type[] { typeof(IntVec3) }, null);
			MethodInfo originalApparelThingAtGeneric = originalApparelThingAt.MakeGenericMethod(new Type[] { typeof(Apparel) });
			MethodInfo patchedApparelThingAt = patched.GetMethod("ThingAt_Apparel");
			HarmonyMethod prefixApparel = new HarmonyMethod(patchedApparelThingAt);
			harmony.Patch(originalApparelThingAtGeneric, prefix: prefixApparel);

			MethodInfo originalBuilding_DoorThingAt = original.GetMethod("ThingAt", bf, null, new Type[] { typeof(IntVec3) }, null);
			MethodInfo originalBuilding_DoorThingAtGeneric = originalBuilding_DoorThingAt.MakeGenericMethod(new Type[] {typeof(Building_Door)});
			MethodInfo patchedBuilding_DoorThingAt = patched.GetMethod("ThingAt_Building_Door");
			HarmonyMethod prefixBuilding_Door = new HarmonyMethod(patchedBuilding_DoorThingAt);
			harmony.Patch(originalBuilding_DoorThingAtGeneric, prefix: prefixBuilding_Door);
			*/
			//RealtimeMoteList			
			original = typeof(RealtimeMoteList);
			patched = typeof(RealtimeMoteList_Patch);
			Prefix(original, patched, "Clear");
			Prefix(original, patched, "MoteSpawned");
			Prefix(original, patched, "MoteDespawned");
			Prefix(original, patched, "MoteListUpdate");

			//GenTemperature			
			original = typeof(GenTemperature);
			patched = typeof(GenTemperature_Patch);
			Prefix(original, patched, "EqualizeTemperaturesThroughBuilding");
			Prefix(original, patched, "PushHeat", new Type[] { typeof(IntVec3), typeof(Map), typeof(float) });
			
			//RCellFinder			
			original = typeof(RCellFinder);
			patched = typeof(RCellFinder_Patch);
			Prefix(original, patched, "RandomWanderDestFor");

			//GenSpawn
			original = typeof(GenSpawn);
			patched = typeof(GenSpawn_Patch);
			Prefix(original, patched, "WipeExistingThings");
			Prefix(original, patched, "CheckMoveItemsAside");
			//Prefix(original, patched, "Spawn", new Type[] {
			//	typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4),
			//	typeof(WipeMode), typeof(bool) });

			//PawnDestinationReservationManager
			original = typeof(PawnDestinationReservationManager);
			patched = typeof(Verse_PawnDestinationReservationManager_Patch);
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

			//DynamicDrawManager
			original = typeof(DynamicDrawManager);
			patched = typeof(Verse_DynamicDrawManager_Patch);
			Prefix(original, patched, "RegisterDrawable");
			Prefix(original, patched, "DeRegisterDrawable");
			Prefix(original, patched, "DrawDynamicThings");
			Prefix(original, patched, "LogDynamicDrawThings");

			//Reachability - needs code rewrite - not efficient
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
			patched = typeof(JobDriver_Wait_Patch);
			Prefix(original, patched, "CheckForAutoAttack");

			//SelfDefenseUtility
			original = typeof(SelfDefenseUtility);
			patched = typeof(SelfDefenseUtility_Patch);
			Prefix(original, patched, "ShouldStartFleeing");

			//GenClosest
			original = typeof(GenClosest);
			patched = typeof(GenClosest_Patch);
			Prefix(original, patched, "RegionwiseBFSWorker");
			Prefix(original, patched, "ClosestThingReachable");
			Prefix(original, patched, "ClosestThing_Global");

			//PawnUtility
			original = typeof(PawnUtility);
			patched = typeof(PawnUtility_Patch);
			Prefix(original, patched, "PawnBlockingPathAt");
			Prefix(original, patched, "EnemiesAreNearby");

			//CellFinder
			original = typeof(CellFinder);
			patched = typeof(CellFinder_Patch);
			Prefix(original, patched, "TryFindRandomCellInRegion");
			Prefix(original, patched, "TryFindRandomReachableCellNear");
			Prefix(original, patched, "TryFindBestPawnStandCell");

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

			//Pawn_MeleeVerbs
			original = typeof(Pawn_MeleeVerbs);
			patched = typeof(Pawn_MeleeVerbs_Patch);
			Prefix(original, patched, "GetUpdatedAvailableVerbsList");

			//AutoUndrafter
			original = typeof(AutoUndrafter);
			patched = typeof(AutoUndrafter_Patch);
			Prefix(original, patched, "AnyHostilePreventingAutoUndraft");

			//AttackTargetsCache
			original = typeof(AttackTargetsCache);
			patched = typeof(AttackTargetsCache_Patch);
			Prefix(original, patched, "GetPotentialTargetsFor");
			Prefix(original, patched, "RegisterTarget");
			Prefix(original, patched, "DeregisterTarget");

			//PawnsFinder
			original = typeof(PawnsFinder);
			patched = typeof(PawnsFinder_Patch);
			Prefix(original, patched, "get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists");
			Prefix(original, patched, "get_AllMapsWorldAndTemporary_Alive");

			//PawnDiedOrDownedThoughtsUtility
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			patched = typeof(PawnDiedOrDownedThoughtsUtility_Patch);
			Prefix(original, patched, "RemoveLostThoughts");
			Prefix(original, patched, "RemoveDiedThoughts");

			//AttackTargetFinder
			original = typeof(AttackTargetFinder);
			patched = typeof(AttackTargetFinder_Patch);
			Prefix(original, patched, "BestAttackTarget");
			Prefix(original, patched, "CanSee");

			//ShootLeanUtility
			original = typeof(ShootLeanUtility);
			patched = typeof(ShootLeanUtility_Patch);
			Prefix(original, patched, "LeanShootingSourcesFromTo");

			//BuildableDef
			original = typeof(BuildableDef);
			patched = typeof(BuildableDef_Patch);
			Prefix(original, patched, "ForceAllowPlaceOver");

			//SustainerManager			
			original = typeof(SustainerManager);
			patched = typeof(SustainerManager_Patch);
			Prefix(original, patched, "get_AllSustainers");
			Prefix(original, patched, "RegisterSustainer");
			Prefix(original, patched, "DeregisterSustainer");
			Prefix(original, patched, "SustainerExists");
			Prefix(original, patched, "EndAllInMap");
			Prefix(original, patched, "UpdateAllSustainerScopes");

			//RecreateMapSustainers
			original = typeof(AmbientSoundManager);
			patched = typeof(AmbientSoundManager_Patch);
			Prefix(original, patched, "RecreateMapSustainers");

			//SubSustainer
			original = typeof(SubSustainer);
			patched = typeof(SubSustainer_Patch);
			Prefix(original, patched, "StartSample");
			Prefix(original, patched, "SubSustainerUpdate");

			//SoundStarter
			original = typeof(SoundStarter);
			patched = typeof(SoundStarter_Patch);
			Prefix(original, patched, "PlayOneShot");
			Prefix(original, patched, "PlayOneShotOnCamera");
			Prefix(original, patched, "TrySpawnSustainer");

			//Pawn_RelationsTracker			
			original = typeof(Pawn_RelationsTracker);
			patched = typeof(Pawn_RelationsTracker_Patch);
			Prefix(original, patched, "get_FamilyByBlood");

			//TickManager			
			original = typeof(TickManager);
			patched = typeof(TickManager_Patch);
			Prefix(original, patched, "get_TickRateMultiplier");

			//Battle			
			original = typeof(Battle);
			patched = typeof(Battle_Patch);
			Prefix(original, patched, "ExposeData");
			Prefix(original, patched, "Absorb");

			//Building_Door			
			original = typeof(Building_Door);
			patched = typeof(Building_Door_Patch);
			Prefix(original, patched, "get_BlockedOpenMomentary");
			Prefix(original, patched, "get_DoorPowerOn");

			//ThoughtHandler						
			original = typeof(ThoughtHandler);
			patched = typeof(ThoughtHandler_Patch);
			Prefix(original, patched, "TotalOpinionOffset");
			Prefix(original, patched, "MoodOffsetOfGroup");
			Prefix(original, patched, "TotalMoodOffset");
			Prefix(original, patched, "OpinionOffsetOfGroup");

			//FireUtility			
			original = typeof(FireUtility);
			patched = typeof(FireUtility_Patch);
			Prefix(original, patched, "ContainsStaticFire");

			//Fire			
			original = typeof(Fire);
			patched = typeof(Fire_Patch);
			Prefix(original, patched, "DoComplexCalcs");
			Prefix(original, patched, "Tick");

			//Projectile			
			original = typeof(Projectile);
			patched = typeof(Projectile_Patch);
			Prefix(original, patched, "ImpactSomething");
			Prefix(original, patched, "Tick");

			//GenGrid_Patch			
			original = typeof(GenGrid);
			patched = typeof(GenGrid_Patch);
			Prefix(original, patched, "InBounds", new Type[] { typeof(IntVec3), typeof(Map) });
			Prefix(original, patched, "InBounds", new Type[] { typeof(Vector3), typeof(Map) });

			//Explosion
			original = typeof(Explosion);
			patched = typeof(Explosion_Patch);
			Prefix(original, patched, "Tick");

			//AttackTargetReservationManager
			original = typeof(AttackTargetReservationManager);
			patched = typeof(AttackTargetReservationManager_Patch);
			Prefix(original, patched, "FirstReservationFor");
			Prefix(original, patched, "ReleaseClaimedBy");

			//PawnCollisionTweenerUtility
			original = typeof(PawnCollisionTweenerUtility);
			patched = typeof(PawnCollisionTweenerUtility_Patch);
			Prefix(original, patched, "GetPawnsStandingAtOrAboutToStandAt");
			Prefix(original, patched, "CanGoDirectlyToNextCell");

			//GridsUtility			
			original = typeof(GridsUtility);
			patched = typeof(GridsUtility_Patch);
			Prefix(original, patched, "GetTerrain");
			Prefix(original, patched, "GetGas");

			//ReservationManager
			original = typeof(ReservationManager);
			patched = typeof(ReservationManager_Patch);
			Prefix(original, patched, "ReleaseClaimedBy");
			Prefix(original, patched, "Reserve");
			Prefix(original, patched, "Release");
			Prefix(original, patched, "FirstReservationFor");
			Prefix(original, patched, "CanReserve");

			//FloodFiller - inefficient global lock			
			original = typeof(FloodFiller);
			patched = typeof(FloodFiller_Patch);
			Prefix(original, patched, "FloodFill", new Type[] { typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, int, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>) });

			//Verb
			original = typeof(Verb);
			patched = typeof(Verb_Patch);
			Prefix(original, patched, "CanHitFromCellIgnoringRange");

			//FastPriorityQueue<KeyValuePair<IntVec3, float>>
			original = typeof(FastPriorityQueue<KeyValuePair<IntVec3, float>>);
			patched = typeof(FastPriorityQueueKeyValuePairIntVec3Float_Patch);
			Prefix(original, patched, "Push");
			Prefix(original, patched, "Pop");
			Prefix(original, patched, "Clear");
			//Prefix(original, patched, "SwapElements");
			//Prefix(original, patched, "CompareElements");

			//MapPawns
			original = typeof(MapPawns);
			patched = typeof(MapPawns_Patch);
			Prefix(original, patched, "get_AllPawns");
			Prefix(original, patched, "LogListedPawns");
			Prefix(original, patched, "RegisterPawn");
			Prefix(original, patched, "get_AnyPawnBlockingMapRemoval");
			Prefix(original, patched, "get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount");
			Prefix(original, patched, "DeRegisterPawn");
			Prefix(original, patched, "FreeHumanlikesSpawnedOfFaction");
			Prefix(original, patched, "SpawnedPawnsInFaction");
			Prefix(original, patched, "get_AllPawnsUnspawned");
			Prefix(original, patched, "get_SpawnedPawnsWithAnyHediff");

			//MapTemperatures
			original = typeof(MapTemperature);
			patched = typeof(MapTemperature_Patch);
			Prefix(original, patched, "MapTemperatureTick");

			//Region
			original = typeof(Region);
			patched = typeof(Region_Patch);
			Prefix(original, patched, "DangerFor");

			//Pawn_WorkSettings
			original = typeof(Pawn_WorkSettings);
			patched = typeof(Pawn_WorkSettings_Patch);
			Prefix(original, patched, "CacheWorkGiversInOrder");

			//Sample
			original = typeof(Sample);
			patched = typeof(Sample_Patch);
			Prefix(original, patched, "Update");

			//Sustainer
			original = typeof(Sustainer);
			patched = typeof(Sustainer_Patch);
			Prefix(original, patched, "Cleanup");

			//ImmunityHandler
			original = typeof(ImmunityHandler);
			patched = typeof(ImmunityHandler_Patch);
			Prefix(original, patched, "ImmunityHandlerTick");

			//Room
			original = typeof(Room);
			patched = typeof(Room_Patch);
			Prefix(original, patched, "OpenRoofCountStopAt");

			//LongEventHandler
			original = typeof(LongEventHandler);
			patched = typeof(LongEventHandler_Patch);
			Prefix(original, patched, "ExecuteToExecuteWhenFinished");

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

			//LordToil_Siege
			original = typeof(LordToil_Siege);
			patched = typeof(LordToil_Siege_Patch);
			Prefix(original, patched, "UpdateAllDuties");

			//BattleLog
			original = typeof(BattleLog);
			patched = typeof(BattleLog_Patch);
			Prefix(original, patched, "Add");

			//PawnCapacitiesHandler
			original = typeof(PawnCapacitiesHandler);
			patched = typeof(PawnCapacitiesHandler_Patch);
			Prefix(original, patched, "GetLevel");
			Prefix(original, patched, "Notify_CapacityLevelsDirty");
			Prefix(original, patched, "Clear");
			ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
			MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
			harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));

			//PathFinder
			original = typeof(PathFinder);
			patched = typeof(PathFinder_Patch);
			Prefix(original, patched, "FindPath", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) });
			ConstructorInfo constructorMethod2 = original.GetConstructor(new Type[] { typeof(Map) });
			MethodInfo cpMethod2 = patched.GetMethod("Postfix_Constructor");
			harmony.Patch(constructorMethod2, postfix: new HarmonyMethod(cpMethod2));

			//WorldPawns
			original = typeof(WorldPawns);
			patched = typeof(WorldPawns_Patch);
			Prefix(original, patched, "get_AllPawnsAlive");

			//PawnPath
			original = typeof(PawnPath);
			patched = typeof(PawnPath_Patch);
			Prefix(original, patched, "AddNode");
			Prefix(original, patched, "ReleaseToPool");

			//GenCollection
			original = typeof(GenCollection);
			patched = typeof(GenCollection_Patch);
			//Prefix(original, patched, "RemoveAll", new Type[], { typeof(Dictionary<TKey, TValue>) , typeof(Predicate< KeyValuePair < TKey, TValue >>) });
			MethodInfo[] genCollectionMethods = original.GetMethods();
			//MethodInfo originalRemoveAll = original.GetMethod("RemoveAll");
			//HACK - because I don't know how to get method the right way
			MethodInfo originalRemoveAll = genCollectionMethods[29];
			MethodInfo originalRemoveAllGeneric = originalRemoveAll.MakeGenericMethod(new Type[] { typeof(Pawn), typeof(SituationalThoughtHandler_Patch.CachedSocialThoughts) });
			MethodInfo patchedRemoveAll = patched.GetMethod("RemoveAll_Pawn_SituationalThoughtHandler_Patch");
			HarmonyMethod prefixRemoveAll = new HarmonyMethod(patchedRemoveAll);
			harmony.Patch(originalRemoveAllGeneric, prefix: prefixRemoveAll);

			//SoundSizeAggregator
			original = typeof(SoundSizeAggregator);
			patched = typeof(SoundSizeAggregator_Patch);
			Prefix(original, patched, "RegisterReporter");
			Prefix(original, patched, "RemoveReporter");

			//HediffSet
			original = typeof(HediffSet);
			patched = typeof(HediffSet_Patch);
			Prefix(original, patched, "PartIsMissing");

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
			patched = typeof(Pawn_InteractionsTracker_Patch);
			Prefix(original, patched, "TryInteractRandomly");

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
			patched = typeof(FoodUtility_Patch);
			Prefix(original, patched, "FoodOptimality");

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
			Prefix(original, patched, "CombineNewAndReusedRoomsIntoContiguousGroups");

			//GenRadial
			original = typeof(GenRadial);
			patched = typeof(GenRadial_Patch);
			Prefix(original, patched, "ProcessEquidistantCells");

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
			patched = typeof(WorkGiver_DoBill_Patch);
			Prefix(original, patched, "TryFindBestBillIngredients");

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
			patched = typeof(WorkGiver_ConstructDeliverResources_Patch);
			Prefix(original, patched, "ResourceDeliverJobFor");

			//GenText
			original = typeof(GenText);
			patched = typeof(GenText_Patch);
			Prefix(original, patched, "CapitalizeSentences");


			//PERFORMANCE IMPROVEMENTS

			//HediffGiver_Heat
			original = typeof(HediffGiver_Heat);
			patched = typeof(HediffGiver_Heat_Patch);
			Prefix(original, patched, "OnIntervalPassed");

			//HediffGiver_Hypothermia
			original = typeof(HediffGiver_Hypothermia);
			patched = typeof(HediffGiver_Hypothermia_Patch);
			Prefix(original, patched, "OnIntervalPassed");

			//Pawn_MindState - hack for speedup. replaced (GenLocalDate.DayTick((Thing)__instance.pawn) interactions today with always 0
			original = typeof(Pawn_MindState);
			patched = typeof(Pawn_MindState_Patch);
			Prefix(original, patched, "MindStateTick");
			
			//GenTemperature			
			original = typeof(GenTemperature);
			patched = typeof(GenTemperature_Patch);
			Prefix(original, patched, "SeasonalShiftAmplitudeAt");

			//WorldObjectsHolder
			original = typeof(WorldObjectsHolder);
			patched = typeof(WorldObjectsHolder_Patch);
			Prefix(original, patched, "WorldObjectsHolderTick");

			//WorldPawns
			original = typeof(WorldPawns);
			patched = typeof(WorldPawns_Patch);
			Prefix(original, patched, "WorldPawnsTick");

			Log.Message("RimThreaded patching is complete.");
		}



		public static void Prefix(Type original, Type patched, String methodName, Type[] orig_type)
		{
			MethodInfo oMethod = original.GetMethod(methodName, bf, null, orig_type, null);
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
		public static void Prefix(Type original, Type patched, String methodName)
		{
			MethodInfo oMethod = original.GetMethod(methodName, bf);
			MethodInfo pMethod = patched.GetMethod(methodName);
			if (null == oMethod)
			{
				Log.Message(original.ToString() + "." + methodName + " not found");
			}
			if (null == pMethod)
			{
				Log.Message(patched.ToString() + "." + methodName + " not found");
			}
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod));
		}
		public static void Postfix(Type original, Type patched, String methodName)
		{
			MethodInfo oMethod = original.GetMethod(methodName, bf);
			MethodInfo pMethod = patched.GetMethod(methodName);
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

		public static void Transpile(Type original, Type patched, String methodName)
        {
			MethodInfo oMethod = original.GetMethod(methodName, bf);
			MethodInfo pMethod = patched.GetMethod(methodName);
			harmony.Patch(oMethod, transpiler: new HarmonyMethod(pMethod));
		}

	}
	
}

