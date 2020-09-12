# RimThreaded
RimThreaded enables Rimworld to utilize multiple threads and thus greatly increases the speed of the game.

Version 1.0.12

CHANGE LOG:
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

This project is still a work in progress and will likely not play well with other mods. I am uploading this hoping to get feedback and identify some bugs as I continue development. If you would like to contribute, I have provided a github link below.

This is my first mod submission, and there is definitely some messy code, so I welcome all kinds of input.

I hope others are as excited as I am to see this great game get multi-threading support!

NOTE: The number of threads to utilize should be set in the mod settings, according to your specific computer's core count.

LOAD ORDER:
The long answer is that this code replaces many of the game's built in methods using Harmony "prefix" (for modders out there). I know this has made some other modders not happy about how I did this. Maybe there is a better way I should have done this? maybe a transpiler? I don't exactly know what I'm doing - being the first mod and all.

That being said, since it replaces methods, I *think* towards the top? Someone please correct me if they have had better luck using a different mod order. I am actually only testing this with vanilla+royalty+tickspersecond right now, so I am probably not the best person to ask. I'll update this description as people reply with different experiences.

BUGS:
https://github.com/cseelhoff/RimThreaded/issues
Your (likely) log location: C:\Users\username\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\player.log

CREDITS:

Bug testing:
Special thank you for helping me test Austin (Stanui)!
And thank you to others in Rimworld community who have posted their bug findings!

Coding:
Big thanks to Kiame Vivacity for all of his help! including fixing the sound issue that was driving me nuts!

Logo:
Thank you ArchieV1 for the logo! (https://github.com/ArchieV1)
Logo help from: Marnador (https://ludeon.com/forums/index.php?action=profile;u=36313) and JKimsey (https://pixabay.com/users/jkimsey-253161/)

Video Review:
Thank you BaRKy for reviewing my mod! I am honored! (https://www.youtube.com/watch?v=EWudgTJksMU)
