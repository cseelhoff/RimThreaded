# RimThreaded
RimThreaded enables Rimworld to utilize multiple threads and thus greatly increases the speed of the game.

DESCRIPTION:  
-THIS MOD IS A WIP AND IS NOT GUARANTEED TO BE COMPATIBLE WITH OTHER MODS-  
I am uploading this hoping to get feedback and identify some bugs as I continue development. If you would like to contribute, I have provided a github link below. Submissions of bug reports with error logs are the most helpful to progress this project! Also submitting lists of known working and incompatible mods helps too.

SETTINGS: The number of threads to utilize should be set in the mod settings, according to your specific computer's core count.  

LOAD ORDER/MOD COMPATIBILITY:  
https://trello.com/b/EG9T6VnW/rimthreaded - Thanks IcyBlackAgeis!  

BUGS:  
https://github.com/cseelhoff/RimThreaded/issues  
Your (likely) log location: C:\Users\username\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\player.log  

DISCORD:  
https://discord.gg/3JJuWK8  

CREDITS:  
Bug testing:  
Special thank you for helping me test Austin (Stanui)!  
And thank you to others in Rimworld community who have posted their bug findings!  

Coding:  
Big thanks to Kiame Vivacity for all of his help! including fixing the sound issue that was driving me nuts!  
Thank you bookdude 13 for your many bugfixes!  
Thank you to Ataman for helping me fix the LVM deep storage bug  

Logo:  
Thank you ArchieV1 for the logo! (https://github.com/ArchieV1)  
Logo help from: Marnador (https://ludeon.com/forums/index.php?action=profile;u=36313) and JKimsey (https://pixabay.com/users/jkimsey-253161/)  

Video Review:  
Thank you BaRKy for reviewing my mod! I am honored! (https://www.youtube.com/watch?v=EWudgTJksMU)  

CHANGE LOG:  
  
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
