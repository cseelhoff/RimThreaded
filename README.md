# RimThreaded
RimThreaded enables RimWorld to utilize multiple threads and thus greatly increases the speed of the game.

JOIN OUR COMMUNITY ON DISCORD:  
https://discord.gg/3JJuWK8

SETTINGS: The number of threads to utilize should be set in the mod settings, according to your specific computer's core count.

JOIN OUR COMMUNITY ON DISCORD:
https://discord.gg/3JJuWK8

SETTINGS: The number of threads to utilize should be set in the mod settings, according to your specific computer's core count.

LOAD ORDER:
Put RimThreaded last in load order.

MOD COMPATIBILITY:
See discord channel.

SUBMIT BUGS:
https://github.com/cseelhoff/RimThreaded/issues/new/choose

CREDITS:
Bug testing:
Special thank you for helping me test Austin (Stanui)!
And thank you to others in Rimworld community who have posted their bug findings!

Coding:
Big thanks to JoJo for his continued help bug fixing and adding mod compatibility!
Big thanks to Kiame Vivacity for his help with fixing sound!
Thank you bookdude13 for your many bugfixes!
Thank you to Ataman for helping me fix the LVM deep storage bug!

Logo:
Thank you ArchieV1 for the logo! https://github.com/ArchieV1
Logo help from: Marnador https://ludeon.com/forums/index.php?action=profile;u=36313 and JKimsey https://pixabay.com/users/jkimsey-253161/

Video Review:
Thank you BaRKy for reviewing my mod! I am honored! https://www.youtube.com/watch?v=EWudgTJksMU

DONATE:
Some subscribers insisted that I set up a donation page. For those looking, here it is: https://ko-fi.com/rimthreaded

CHANGE LOG:  

Version 1.2.2  
-Fixed bug in AttackTargetsCache.GetPotentialTargetsFor  

Version 1.2.1  
-Fixed bug in GenTemperature.GetTemperatureFromSeasonAtTile  
-Fixed bug in World.NaturalRockTypesIn  
-Fixed bug in CellFinder.TryFindRandomCellNear  
-Fixed bug in TradeShip.PassingShipTick  

Version 1.2.0  
-Major overhaul for RimThreaded.cs, improving multimap support  
-Major overhaul for SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick, improving multimap support  
-Major overhaul for TradeShip.PassingShipTick, improving multimap support  
-Major overhaul for WildPlantSpawner.WildPlantSpawnerTickInternal, improving multimap support  
-Fixed bug in DijkstraIntVec3.Run  
-Fixed bug in LordManager.LordOf  
-Fixed bug in PathFinder.getRegionCostCalculatorWrapper  
-Fixed bug in PawnDestinationReservationManager.MostRecentReservationFor  
-Fixed bug in PhysicalInteractionReservationManager.FirstReserverOf  
-Fixed bug in Thing.get_FlammableNow  
-Fixed bug in FoodUtility.FoodOptimality  

Version 1.1.38  
-Fixed bug in BiomeDef.CachePlantCommonalitiesIfShould  
-Fixed bug in Explosion.AffectCell  
-Fixed bug in Explosion.Tick  
-Fixed bug in Explosion.StartExplosion  
-Fixed bug in FilthMaker.TryMakeFilth  
-Fixed bug in GenCollection.TryRandomElementByWeight_Pawn  
-Fixed bug in GenLeaving.DropFilthDueToDamage  
-Fixed bug in ListerThings.ThingsOfDef  
-Fixed bug in Projectile.CanHit  
-Fixed bug in ReachabilityCache.AddCachedResult  
-Fixed bug in RegionListersUpdater.RegisterAllAt  
-Fixed bug in RoofGrid.SetRoof  
-Fixed bug in SickPawnVisitUtility.FindRandomSickPawn  
-Fixed bug in TemperatureCache.TryCacheRegionTempInfo  
-Fixed bug in Verb.get_DirectOwner  
-Fixed bug in WorkGiver_DoBill.TryFindBestBillIngredients  

Version 1.1.37  
-Fixed bug in MOARANDROIDS.PawnGroupMakerUtility_Patch.androidTiers_GeneratePawns (Thanks JoJo!)  
-Performance improvement for Alert_MinorBreakRisk.GetReport()  
-Fixed bug in MapPawns.get_AllPawnsUnspawned()  
-Changed default setting for timeout from 1000ms to 3000ms  

Version 1.1.36  
-Transpiled PawnCapacitiesHandler.GetLevel  

Version 1.1.35  
-Fixed bug in Graphics.DrawMesh  
-Fixed bug in AlertsReadout.AlertsReadoutUpdate  
-Fixed bug in ReachabilityCache.ClearForHostile  
-Transpiled WorkGiver_DoBill.TryFindBestBillIngredients  
-Transpiled WorkGiver_DoBill.AddEveryMedicineToRelevantThings  
-Transpiled Building_Trap.Tick  
-Transpiled JobsOfOpportunity.Hauling.CanHaul  

Version 1.1.34  
-Fixed bug in GUIStyle.CalcSize  
-Fixed bug in DubsSkylight_getPatch.Postfix  
-Fixed bug in JobsOfOpportunity.Hauling.CanHaul  

Version 1.1.33  
-Fixed bug in HediffSet.GetFirstHediffOfDef  
-Fixed bug in HediffSet.HasTendableHediff  
-Fixed bug in HediffSet.HasImmunizableNotImmuneHediff  
-Fixed bug in Pawn_HealthTracker.RemoveHediff  
-Fixed bug in RecordWorker_TimeGettingJoy.ShouldMeasureTimeNow  
-Fixed bug in Building_Trap.Tick  
-Fixed bug in DubsSkylight_getPatch.Postfix  
-Fixed bug in ImmunityHandler.NeededImmunitiesNow  
-Fixed bug in Lord.AddPawn  
-Fixed bug in Lord.RemovePawn  
-Fixed bug in LordManager.LordOf  
-Fixed bug in RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms  
-Fixed bug in ReservationManager.Reserve  

Version 1.1.32  
-Improved performance for Reachability.CanReach  
-Fixed bug in Projectile.CanHit  
-Added Debug information to RegionCostCalculatorWrapper.Init  

Version 1.1.31  
-Fixed bug in BodyPartDef.IsSolid  
-Fixed bug in GridsUtility.GetThingList  
-Fixed bug in Pawn.Destroy  
-Fixed bug in PawnDiedOrDownedThoughtsUtility.RemoveLostThoughts  
-Fixed bug in PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts  
-Fixed bug in PawnDiedOrDownedThoughtsUtility.RemoveResuedRelativeThought  
-Fixed bug in Pawn_PlayerSettings.set_Master  
-Fixed bug in SustainerAggregatorUtility.AggregateOrSpawnSustainerFor  

Version 1.1.30  
-Fixed bug in BeautyUtility.FillBeautyRelevantCells  
-Fixed bug in CellFinder.TryFindBestPawnStandCell
-Fixed bug in DamageWorker.ExplosionAffectCell  
-Fixed bug in GrammarResolver.ResolveUnsafe  
-Fixed bug in Job.MakeDriver  
-Fixed bug in Job.ToString  
-Fixed bug in Pawn.Destroy  
-Fixed bug in PawnRelationUtility.GetMostImportantColonyRelative  
-Fixed bug in PawnsFinder.get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners  
-Fixed bug in QuestUtility.GetExtraFaction  
-Fixed bug in ReservationManager.CanReserveStack  
-Fixed bug in ReservationManager.Reserve  
-Fixed bug in RestUtility.GetBedSleepingSlotPosFor  
-Fixed bug in SelfDefenseUtility.ShouldStartFleeing  
-Fixed bug in SoundSizeAggregator.get_AggregateSize  
-Fixed bug in TaleManager.CheckCullUnusedVolatileTales  
-Improved performance for Pawn_PlayerSettings.set_Master  
-Improved performance for Lord.AddPawn  
-Improved performance for Lord.RemovePawn  
-Improved performance for LordManager.LordOf  

Version 1.1.29  
-Fixed bug in AttackTargetReservationManager.ReleaseClaimedBy  
-Fixed bug in CompCauseGameCondition.CompTick  
-Fixed bug in FoodUtility.FoodOptimality  
-Fixed bug in HediffSet.HasDirectlyAddedPartFor  
-Fixed bug in JobDriver.TryActuallyStartNextToil  
-Fixed bug in JobGiver_OptimizeApparel.ApparelScoreGain  
-Fixed bug in JobGiver_OptimizeApparel.ApparelScoreGain_NewTmp  
-Fixed bug in PathGrid.CalculatedCostAt  
-Fixed bug in PawnCapacitiesHandler.CapableOf  
-Fixed bug in ReservationManager.CanReserve  

Version 1.1.28  
-Fixed bug in GenClosest.RegionwiseBFSWorker  
-Fixed bug in JobDriver.DriverTick  
-Fixed bug in Pawn_RotationTracker.UpdateRotation  
-Fixed bug in Room.get_ContainedAndAdjacentThings  

Version 1.1.27  
-Fixed bug in BeautyUtility.FillBeautyRelevantCells  
-Fixed bug in Building_Door.get_DoorPowerOn  
-Fixed bug in ColoredText.Resolve  
-Fixed bug in DijkstraIntVec3.Run  
-Fixed bug in FastPriorityQueueKeyValuePairIntFloat  
-Fixed bug in FastPriorityQueueRegionLinkQueueEntry  
-Fixed bug in GenLabel.ThingLabel  
-Fixed bug in GrammarResolver.RandomPossiblyResolvableEntryb__0  
-Fixed bug in HediffSet.PartIsMissing  
-Fixed bug in HediffSet.MoveNext  
-Fixed bug in InfestationCellFinder.CalculateDistanceToColonyBuildingGrid  
-Fixed bug in InfestationCellFinder.GetScoreAt  
-Fixed bug in JobDriver.DriverTick  
-Fixed bug in JobDriver.CheckCurrentToilEndOrFail  
-Fixed bug in ListerThings.Add  
-Fixed bug in Map.get_IsPlayerHome  
-Fixed bug in PathFinder.getRegionCostCalculatorWrapper  
-Fixed bug in PawnCapacitiesHandler.GetLevel  
-Fixed bug in PawnCapacityUtility.CalculatePartEfficiency  
-Fixed bug in PawnDiedOrDownedThoughtsUtility.RemoveLostThoughts  
-Fixed bug in PawnDiedOrDownedThoughtsUtility.RemoveResuedRelativeThought  
-Fixed bug in Pawn_HealthTracker.SetDead  
-Fixed bug in Region.OverlapWith  
-Fixed bug in Thing.SpawnSetup  
-Fixed bug in Thing.DeSpawn  
-Fixed bug in WildPlantSpawner.CalculatePlantsWhichCanGrowAt  
-Added transpiling methods for developers  

Version 1.1.26  
-Temporarily removed GenLabel.ThingLabel Patch from 1.1.25  
-Transpiled AttackTargetReservationManager.Reserve  
-Fixed bug in GrammarResolver.RandomPossiblyResolvableEntry  

Version 1.1.25  
-Fixed bug in GenLabel.ThingLabel  
-Fixed bug in GrammarResolver.AddRule  
-Fixed bug in CompSpawnSubplant.DoGrowSubplant  
-Changed Error message to Warning in Pawn_PathFollower.StartPath  

Version 1.1.24  
-Fixed bug in GrammarResolverSimpleStringExtensions.Formatted  

Version 1.1.23  
-Fixed bug in Verb_LaunchProjectileCE.CanHitFromCellIgnoringRange  

Version 1.1.22  
-Fixed bug in GenCollection.RemoveAll  
-Fixed bug in RecipeWorkerCounter.GetCarriedCount  
-Fixed bug in Pawn_RotationTracker.UpdateRotation  
-Fixed bug in Texture2D.GetPixel  
-Fixed bug in Verb_LaunchProjectileCE.CanHitFromCellIgnoringRange  
-Fixed bug in Verb_LaunchProjectileCE.TryFindCEShootLineFromTo  
-Fixed bug in Verb_MeleeAttackCE.TryCastShot  
-Fixed bug in HediffSet.CacheMissingPartsCommonAncestors  
-Fixed bug in JobDriver.get_CurToil  

Version 1.1.21  
-Fixed bug in GUIStyle.CalcSize  
-Fixed bug in Reachability.DetermineStartRegions2  
-Improved speed for Pawn_JobTracker_DetermineNextJob_Transpile (GiddyUp)  

Version 1.1.20  
-Fixed bug in MapPawns.RegisterPawn  

Version 1.1.19  
-Hotfix for MapPawns.RegisterPawn  
-Hotfix for MapPawns.DeRegisterPawn  

Version 1.1.18   
-Fixed bug in GenAdj.TryFindRandomAdjacentCell8WayWithRoomGroup  
-Fixed bug in GenCollections.RemoveAll  
-Fixed bug in MapPawns.SpawnedPawnsInFaction  
-Fixed bug in MapPawns.EnsureFactionsListsInit  
-Fixed bug in MapPawns.EnsureFactionsListsInit  
-Transpiled MapPawns.RegisterPawn  
-Transpiled MapPawns.DeRegisterPawn  

Version 1.1.17  
-Fixed bug in HediffSet.CacheMissingPartsCommonAncestors  
-Fixed bug in UniqueIDsManager.GetNextID  

Version 1.1.16  
-Fixed bug in PawnRules.RimWorld_Pawn_GuestTracker_SetGuestStatus  

Version 1.1.15  
-Fixed bug in AttackTargetReservationManager.ReleaseAllForTarget  
-Fixed bug in AttackTargetReservationManager.ReleaseAllClaimedBy  
-Fixed bug in AttackTargetReservationManager.IsReservedBy  
-Fixed bug in GrammarResolver.AddRule  

Version 1.1.14  
-Fixed bug in CE_Utility.BlitCrop  
-Fixed bug in CE_Utility.GetColorSafe  
-Fixed bug in AttackTargetReservationManager.ReleaseAllForTarget  
-Fixed bug in Texture2D.GetPixel  

Version 1.1.13  
-Changed how forced slowdowns work. They slowdowns happen, but can be overridden.  
-RimThreaded now enables speed 4 even without dev mode  
-Fixed bug in PawnRules.RimWorld_Pawn_GuestTracker_SetGuestStatus  
-Fixed bug in BuildableDef.get_PlaceWorkers  
-Fixed bug in GenCollection.RemoveAll_Pawn_SituationalThoughtHandler
-Fixed bug in LordToil_Siege.UpdateAllDuties  
-Fixed bug in PathGrid.ContainsPathCostIgnoreRepeater  

Version 1.1.12  
-Added Compatibility for Awesome Inventory  
-Fixed bug in RenderTexture.GetTemporaryImpl  
-Fixed bug in Texture2D.getReadableTexture  

Version 1.1.11  
-Optimized code for calling thread-safe functions from main thread  
-Added feature for configurable thread timeout for long running methods  
-Fixed bug in ResourceCounter.get_TotalHumanEdibleNutrition  
-Fixed bug in ResourceCounter.ResetDefs  
-Fixed bug in ResourceCounter.ResetResourceCounts  
-Fixed bug in ResourceCounter.GetCount  
-Fixed bug in ResourceCounter.GetCountIn  
-Fixed bug in ResourceCounter.UpdateResourceCounts  
-Fixed bug in WanderUtility.GetColonyWanderRoot  
-Fixed bug in RCellFinder.RandomWanderDestFor  
-Fixed bug in Room.RemoveRegion  
-Fixed bug in MapPawns.FreeHumanlikesSpawnedOfFaction  

Version 1.1.10  
-Fixed bug in WanderUtility.GetColonyWanderRoot  
-Fixed bug in RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms  

Version 1.1.9  
-Performance improvement in RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms  
-Fixed bug in WorldFloodFiller.FloodFill  

Version 1.1.8  
-Fixed bug in ImmunityHandler.NeededImmunitiesNow  
-Fixed bug in JobDriver.TryActuallyStartNextToil  
-Fixed bug in JobQueue.EnqueueFirst  
-Fixed bug in JobQueue.EnqueueLast  
-Fixed bug in JobQueue.Contains  
-Fixed bug in JobQueue.Extract  
-Fixed bug in JobQueue.Dequeue  
-Fixed bug in PawnsFinder.get_AllMapsCaravansAndTravelingTransportPods_Alive  
-Fixed bug in PawnsFinder.get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists  
-Fixed bug in PawnsFinder.get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists  
-Fixed bug in PawnsFinder.get_AllMapsWorldAndTemporary_Alive  
-Fixed bug in PawnUtility.ForceWait  
-Fixed bug in PlayLog.RemoveEntry  
-Fixed bug in RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms  
-Fixed bug in ReservationUtilitiy.CanReserve  
-Fixed bug in WanderUtility.GetColonyWanderRoot  

Version 1.1.7  
-Transpiled Pawn_JobTracker_DetermineNextJob_Transpile.Postfix (GiddyUpCore compatibility)

Version 1.1.6  
-Fixed bug in Room.RemoveRegion (game frozen when building)

Version 1.1.5  
-Transpiled Rand.PopState  
-Transpiled Rand.PushState  
-Transpiled Rand.TryRangeInclusiveWhere  

Version 1.1.4  
-Transpiled CompUtility.CompGuest  
-Transpiled CompUtility.OnPawnRemoved  
-Transpiled HediffGiver_Hypothermia.OnIntervalPassed  
-Transpiled ListerThings.Add  
-Transpiled ListerThings.Remove  

Version 1.1.3  
-Fixed bugs in GrammarResolverSimple.Formatted  
-Fixed bugs in Rand  

Version 1.1.2  
-Fixed several bugs in Rand (fixed incompatibility with Map Reroll)  

Version 1.1.1  
-Transpiled BattleLog.Add  
-Transpiled Pawn_InteractionsTracker.TryInteractRandomly  
-Transpiled RemoveRegion.RemoveRegion  
-Fixed bug in ImmunityHandler.NeededImmunitiesNow  

Version 1.1.0  
-Fixed bug in WorldComponent Ticks  
-Fixed bug in TradeShip Ticks  
-Fixed bug in WildPlantSpawner Ticks  
-Fixed bug in Faction Ticks  

Version 1.0.83  
-Fixed bug in PathFinder.FindPath  
-Fixed bug in GiddyUpCore.Storage.ExtendedDataStorage.GetExtendedDataFor  

Version 1.0.82  
-Major performance improvement in PathFinder.FindPath  
-Fixed bug in RegionListersUpdater.RegisterInRegions  
-Fixed bug in RegionListersUpdater.DeregisterInRegions  

Version 1.0.81  
-Fixed bug in FireUtility.ContainsStaticFire  
-Fixed bug in GUIStyle.CalcHeight  
-Fixed bug in ImmunityHandler.NeededImmunitiesNow  
-Fixed bug in ImmunityHandler.ImmunityHandlerTick  
-Fixed bug in RegionListersUpdater.DeregisterInRegions  
-Fixed bug in ReservationManager.ReleaseClaimedBy  

Version 1.0.80  
-Fixed bug causing incidents to trigger twice  
-Fixed bug in GiddyUpCore.Jobs.JobDriver_Mounted.waitForRider  

Version 1.0.79  
-Added feature to assist in automating method transpiling  
-Added method RenderTexture.ReleaseTemporaryThreadSafe  
-Fixed bug in GiddyUpCore.Utilities.TextureUtility.getReadableTexture  
-Fixed bug in GiddyUpCore.Utilities.TextureUtility.setDrawOffset  
-Fixed bug in GiddyUpCore.Storage.ExtendedDataStorage.DeleteExtendedDataFor  
-Fixed bug in Texture2D.Internal_Create  
-Fixed bug in Texture2D.ReadPixels  
-Fixed bug in Texture2D.Apply  
-Fixed bug in HediffSet.AddDirect  

Version 1.0.78
-Added extra error logs to Pawn.Tick (hopefully this will give more useful logs with thread timeout errors)  
-Transpiled BuildableDef.ForceAllowPlaceOver  
-Transpiled Building_Door.get_BlockedOpenMomentary  
-Transpiled AttackTargetReservationManager.IsReservedBy  
-Transpiled GridsUtility.GetGas  
-Transpiled ReservationManager.CanReserve  
-Transpiled HediffSet.PartIsMissing  
-Transpiled HediffSet.HasDirectlyAddedPartFor  
-Transpiled FoodUtility.FoodOptimality  

Version 1.0.77
-Improved performance of GenClosest.ClosestThingReachable  
-Fixed bug in ResourceCounter.ResetDefs  
-Fixed bug in ResourceCounter.ResetResourceCounts  
-Fixed bug in ResourceCounter.GetCount  
-Fixed bug in ResourceCounter.GetCountIn  
-Fixed bug in ResourceCounter.UpdateResourceCounts  

Version 1.0.76
-Fixed bug in HediffSet.HasDirectlyAddedPartFor  
-Fixed bug in Region.get_AnyCell  
-Fixed bug in RegionAndRoomUpdater.SetAllClean2  
-Fixed bug in RegionCostCalculator.GetPreciseRegionLinkDistances  
-Fixed bug in RenderTexture.GetTemporary  
-Fixed bug in ResourceCounter.get_TotalHumanEdibleNutrition  
-Fixed bug in WorldGrid.IsNeighbor  

Version 1.0.75
-Fixed bug in InfestationCellFinder.CalculateDistanceToColonyBuildingGrid  
-Transpiled Map.MapUpdate  
-Transpiled Pawn_WorkSettings.CacheWorkGiversInOrder  

Version 1.0.74  
-Fixed bug in ThingOwnerUtility.GetAllThingsRecursively(Thing) that caused item wealth to be 0  

Version 1.0.73  
-Fixed bug in PathFinder.FindPath that prevented pawns from reaching diagonal grids  

Version 1.0.72  
-Added feature to modify TimeSpeed multipliers in mod settings  
-Added feature to display ticks per second  

Version 1.0.71  
-Fixed bug with looping sounds  
-Replaced many ConcurrentDictionaries with more efficient Dictionaries using locks  

Version 1.0.70  
-Fixed bug in Room.OpenRoofCountStopAt (fixed issue with rooms showing "Unroofed -1")  

Version 1.0.69  
-Fixed bug in GridsUtility.IsInPrisonCell  
-Fixed bug in MapPawns.FreeHumanlikesSpawnedOfFaction  
-Fixed bug in Reachability.CheckRegionBasedReachability  
-Fixed bug in MapPawns.TryRebuildDirtyRegionsAndRooms  
-Fixed bug in TemperatureCache.TryCacheRegionTempInfo  
-Fixed bug in WanderUtility.GetColonyWanderRoot  
  
Version 1.0.68  
-Fixed bug in Room.get_PsychologicallyOutdoors
-Fixed bug in SteadyEnvironmentEffects.DoCellSteadyEffects  

Version 1.0.67  
-Fixed bug in Region.DangerFor (null room group)  
     
Version 1.0.66  
-Fixed bug in Region.DangerFor (again... source of another Room.get_Temperature bug)  
-Fixed bug in PawnsFinder.get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists  
-Fixed bug in SteadyEnvironmentEffects.DoCellSteadyEffects  
   
Version 1.0.65  
-Added feature in settings to suppress 'Could not load UnityEngine.Texture2D' error  
-Fixed bug in JobDriver.TryActuallyStartNextToil  
-Fixed bug in PawnUtility.ForceWait  
-Fixed bug in Region.DangerFor (source of Room.get_Temperature bug)  
-Fixed bug in ThinkNode_QueuedJob.TryIssueJobPackage  
  
Version 1.0.64  
-Fixed bug in FloatMenuMakerMap.AddHumanlikeOrders (colonists can force wear clothes again)  
-Fixed bug in RenderTexture.set_active (getting closer to giddy-up compatibility)  
-Fixed bug in RenderTexture.get_active (getting closer to giddy-up compatibility)  
  
Version 1.0.63  
-Fixed bug in Graphics.blit (caused crashes with giddy-up)  
-Fixed bug in Dijkstra.Run(int)  
-Fixed bug in ResourceCounter.get_TotalHumanEdibleNutrition  
-Fixed bug in SustainerManager.UpdateAllSustainerScopes  
-Removed 2 methods that qualify for timeoutExemptThreads  

Version 1.0.62  
-Fixed bug in CellFinder.TryFindRandomCellNear  
-Fixed bug in ThinkNode_SubtreesByTag.TryIssueJobPackage  
-Fixed bug in Building_Door.get_DoorPowerOn  
-Fixed bug in ListerThings_Patch.Add  
-Fixed bug in RCellFinder.RandomWanderDestFor  

Version 1.0.61  
-Transpiled Verb.TryFindShootLineFromTo (for compatibility with CombatExtended)  
-Transpiled Verb.CanHitFromCellIgnoringRange  

Version 1.0.60  
-Transpiled FloatMenuMakerMap.AddHumanlikeOrders  
-Added Harmony Priority for WorkGiver_ConstructDeliverResources.ResourceDeliverJobFor (compatibility with CodeOptimist.JobsOfOpportunity) 

Version 1.0.59  
-Fixed bug in ThingGrid.ThingAt(Building_Door)  
-Fixed bug in FloatMenuMakerMap.AddHumanlikeOrders  
-Fixed bug in AttackTargetReservationManager.IsReservedBy  
-Fixed bug in AttackTargetReservationManager.CanReserve  
-Fixed bug in ReservationManager.FirstRespectedReserver  
-Fixed bug in Verb.TryFindShootLineFromTo  
-Fixed bug in PathGrid.CalculatedCostAt  

Version 1.0.58   
-Fixed bugs in AttackTargetFinder.GetRandomShootingTargetByScore  
-Fixed bug in SubSustainer  
-Disabled vanilla game feature that forces game to switch to Normal speed during some events  
  
Version 1.0.57  
-Fixed bugs in ShootLeanUtility  
-Added mod compatibility for mods (like Giddy-Up) that call Graphics.Blit from worker threads  
  
Version 1.0.56  
-Fixed bug in LightningBoltMeshMaker.NewBoltMesh  
-Fixed bugs in MeditiationFocusTypeAvailabilityCache  
-Fixed bug in AmbientSoundManager.EnsureWorldAmbientSoundCreated  

Version 1.0.55  
-Fixed more sounds issues  
-Fixed bug in JobQueue.AnyCanBeginNow  
-Fixed bug in MapTemperature.MapTemperatureTick  

Version 1.0.54  
-Added mod compatibility for mods (like Giddy-Up) that call RenderTexture.GetTemporary from worker threads  
-Fixed bug in SampleSustainer.TryMakeAndPlay  

Version 1.0.53  
-Fixed bug in SampleSustainer.TryMakeAndPlay  
-Fixed bug in MapTemperature.MapTemperatureTick  

Version 1.0.52  
-Fixed many sound issues!  
-Fixed bug in SituationalThoughtHandler.AppendSocialThoughts  

Version 1.0.51  
-Fixed bug in TickList.Register  
-Fixed bug in TickList.Deregister  
-Added function to exempt threads from timeout (helps long running methods like map generation)

Version 1.0.50  
-Forced MapGenerator.GenerateMap to run on main thread only (hopefully helps z-level compatibility)  
-Fixed bug in ReservationManager.IsReservedByAnyoneOf  
-Removed a few messy hacks that I never should have used  

Version 1.0.49  
-Transpiled GrammarResolverSimple.Formatted  
-Transpiled WorkGiver_ConstructDeliverResources.ResourceDeliverJobFor  

Version 1.0.48  
-Transpiled GenRadial.ProcessEquidistantCells  
-Added feature to list mods possibly conflicting with RimThreaded in Mod Settings  

Version 1.0.47  
-Transpiled PathFinder.FindPath!  
-Fixed bug in StoryState.RecordPopulationIncrease  
-Fixed bug in ThinkNode_Priority.TryIssueJobPackage  

Version 1.0.46  
-Transpiled Fire.DoComplexCalcs  
-Fixed bug in SustainerAggregatorUtility.AggregateOrSpawnSustainerFor  

Version 1.0.45  
-Transpiled AttackTargetFinder.AutoAttack  
-Transpiled AttackTargetFinder.CanSee  

Version 1.0.44  
-Fixed bug in ThingOwnerThing.Remove  
-Fixed bug in JobDriver_Wait.AutoAttack  
-Transpiled JobDriver_Wait.AutoAttack  

Version 1.0.43  
-Transpiled ThingOwnerThing.TryAdd  
-Transpiled ThingOwnerThing.Remove  

Version 1.0.42  
-Fixed bug in MapPawns.PawnsInFaction  
-Multithreaded a few miscellaneous functions remaining from DoSingleTick  
-Fixed many "Thread timeout" bugs  

Version 1.0.41  
-Optimized code in ThinkNode_PrioritySorter.TryIssueJobPackage  

Version 1.0.40  
-Reworked tons of multithreading code (seriously... too many to list here)  

Version 1.0.39  
-Added multithreading to FactionManager  
-Added multithreading to SteadyEnvironmentEffects  
-Added multithreading to WildPlantSpawner  
-Added multithreading to WindManager  
-Fixed bug in WorldPawns  
-Fixed bug in WorldObjectsHolder  
-Fixed bug in BiomeDef  
-Fixed bug in Explosion AffectCell  

Version 1.0.38  
-Fixed bug in Mod Settings that did not load properly  
-Added feature that allows Mod Settings to be applied in game without restart  

Version 1.0.37  
-Optimized PathFinder  
-Removed Pawn_PathFollower patch  

Version 1.0.36  
-Fixed bug in AttackTargetsCache  
-Fixed bug in GenText  
-Fixed bug in PawnUtility  

Version 1.0.35  
-Fixed multithreading for WorldObjectsHolderTick  
-Fixed multithreading for WorldPawnsTick  
-Fixed bug in PawnDestinationReservationManager  
-Fixed bug in WorkGiver_ConstructDeliverResources  

Version 1.0.34  
-Added multithreading for WorldObjectsHolderTick  
-Added multithreading for WorldPawnsTick  
-Fixed bug in DynamicDrawManager  
-Fixed bug in JobGiver_Work  
-Fixed bug in ReservationManager  
-Fixed bug in Room  
-Fixed bug in ThingCountUtility  
-Simplified code in Ticklist.Tick  

Version 1.0.33  
-Fixed bug in GenTemperature - SeasonalShiftAmplitudeAt bug #131  
-Fixed bug in WorkGiver_DoBill  

Version 1.0.32 - All credit goes to bookdude13. Thanks bud!  
-Fixed bug in ThinkNode_Priority - TryIssueJobPackage  
-Fixed bug in HediffSet - PartIsMissing  
-Fixed bug in MapTemperature - MapTemperatureTick  

Version 1.0.31  
-Added performance optimization to GenTemperature - SeasonalShiftAmplitudeAt  
-Fixed bug in Medicine - GetMedicineCountToFullyHeal  
-Fixed bugs in ReservationManager  

Version 1.0.30
-Fixed bug in WealthWatcher - ForceRecount  
-Fixed bug in BeautyUtility - AverageBeautyPerceptible  

Version 1.0.29  
-Fixed bug in Fire (rain was not extinguishing)  
-Fixed bug in SituationalThoughtHandler  

Version 1.0.28  
-Fixed bug in GenRadial (fixes problem with blight)  

Version 1.0.27  
-Fixed bugs in RegionAndRoomUpdater - bug #112  

Version 1.0.26  
-Fixed bug in WanderUtility - GetColonyWanderRoot  

Version 1.0.25  
-Fixed bug in Thread abort timeouts  
-Fixed bug in BeautyUtility  
-Fixed bug in FoodUtility  

Version 1.0.24  
-Fixed bug in Thread abort timeouts  
-Fixed bug in BeautyUtility  
-Fixed bug in FoodUtility  
-Fixed bug in GenClosest  
-Fixed bug in MapPawns  
-Fixed bug in ReservationManager  
-Fixed bug in TendUtility  
-Fixed bug in Toils_Ingest  

Version 1.0.23  
-Fixed bug in BFSWorker  
-Fixed bug in JobGiver_ConfigurableHostilityResponse  
-Fixed bug in LanguageWordInfo  
-Fixed bug in PathFinder  
-Fixed bug in Projectiles (Bullet, DoomsdayRocket, Explosive, Spark, WaterSplash)  

Version 1.0.22  
-Fixed bug in GenCollection  
-Fixed bug in HediffSet  
-Fixed bug in LordToil_Siege  
-Fixed bug in PathFinder  
-Fixed bug in PawnCollisionTweenerUtility  
-Fixed bug in PawnDestinationReservationManager  
-Fixed bug in PawnUtility  
-Fixed bug in ReservationManager  
-Fixed bug in TickList (fixed many Threads timing out)  
-Fixed bug in SoundSizeAggregator  

Version 1.0.21  
-Fixed bug with AmbientSoundManager  
-Fixed bug with Pawn_PathFollower  
-Fixed bug with Region  
-Fixed bug with ReservationManager  
-Fixed bug with SustainerManager  
-Fixed bug with ThingGrid (Building_Door)  

Version 1.0.20  
-Fixed bug with AttackTargetReservationManager  
-Fixed bug with MapPawns  
-Fixed bug with Pawn_PathFollower  

Version 1.0.19  
-Fixed bug with PawnPath  
-Fixed bug with ReservationManager  
-Fixed bug with ThingGrid  
-Fixed bug with TickList  

Version 1.0.18  
-Fixed bug with PawnCapacitiesHandler (infinite stat recursion bug 13)  
-Fixed bug with PathFinder  
-Fixed bug with PawnsFinder  
-Fixed bug with ThingGrid  
-Fixed bug with WorldPawns  

Version 1.0.17  
-Fixed bug with AttackTargetReservationManager  
-Fixed bug with BattleLog  
-Fixed bug with Pawn_RecordsTracker  
-Fixed bug with ThingOwnerUtility  

Version 1.0.16  
-Fixed bug with BuildableDef  
-Fixed bug with GenAdjFast  
-Fixed bug with GenSpawn  
-Fixed bug with LordToil_Siege  
-Fixed bug with PawnCollisionTweenerUtility  
-Fixed bug with SituationalThoughtHandler  

Version 1.0.15  
-Fixed bug with SituationalThoughtHandler  

Version 1.0.14  
-Fixed a bunch of bugs appearing in combat. Many of which were causing thread timeouts.  

Version 1.0.13  
-Fixed a bug that did not allow colonists wear apparel  
    
Version 1.0.12  
-Fixed tons of sound issues - All credit goes to KV!  

Version 1.0.11  
-Added enhancement/bug 51 (LVM deep storage mod is not compatible with RimThreaded; items scatter out of containers)  
-Fixed bug 54 (Verse.Room.OpenRoofCountStopAt)  

Version 1.0.10  
-Added enhancement 46 (Add a custom timeout setting for threads)  

Version 1.0.9  
-Fixed bug 34 (UnityEngine.AudioSource.SetPitch)  
-Fixed missing code in GenSpawn.Spawn  
-Fixed bug 41 (Pawns not extinguingishing fires)  
-Fixed bug 43 (Verse.ImmunityHandler.TryAddImmunityRecord)  

Version 1.0.8  
-Fixed bug 10 (RimWorld.Pawn_WorkSettings.CacheWorkGiversInOrder)  

Version 1.0.7  
-Fixed bug 33 (RimWorld.Building_Door.get_DoorPowerOn)  
-Fixed bug 35 (Verse.MapPawns.FreeHunmanlikesSpawnedOfFaction)  
-Fixed bug 36 (RimThreaded.ReservationManager_Patch.CanReserve)  
-Fixed duplicate code section in SubSustainer  

Version 1.0.6  
-Fixed Audio issue with sustainers. Thanks Kiame Vivacity! (bug 3)  
-Fixed bug 17 - Verse.Region.DangerFor  
-Fixed bug 29 - RimThreaded.ThingOwnerUtility_Patch.GetAllThingsRecursively  
-Fixed some other bugs with MapPawns possibly related to bug 10  
-Fixed bug in RimThreaded that allows Camera+ to work well now. No more saying this mod is incompatible with everything ;-)  

Version 1.0.5  
-Fixed bug 26 (related to error with Verse.MapPawns.get_AllPawns)  

Version 1.0.4  
-Fixed a bug in the randomizer that caused a lot of issues. Bugs fixed: 14, 19, and 20  

Version 1.0.3  
-Fixed the dreaded "Thread did not finish..." causing game to completely freeze (threads are now aborted and auto-restarted)  
-Fixed Projectiles not hitting  
-Removed some old unused methods  
