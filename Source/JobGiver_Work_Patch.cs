using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class JobGiver_Work_Patch
	{
		internal static void RunDestructivePatches()
		{
			Type original = typeof(JobGiver_Work);
			Type patched = typeof(JobGiver_Work_Patch);
			RimThreadedHarmony.Prefix(original, patched, "TryIssueJobPackage");
		}
        public static bool TryIssueJobPackage(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
		{
#if DEBUG
			DateTime startTime = DateTime.Now;
#endif
			if (__instance.emergency && pawn.mindState.priorityWork.IsPrioritized)
			{
				List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
				for (int i = 0; i < workGiversByPriority.Count; i++)
				{
					WorkGiver worker = workGiversByPriority[i].Worker;
					if (__instance.WorkGiversRelated(pawn.mindState.priorityWork.WorkGiver, worker.def))
					{
						Job job = GiverTryGiveJobPrioritized(__instance, pawn, worker, pawn.mindState.priorityWork.Cell);
						if (job != null)
						{
							job.playerForced = true;
							__result = new ThinkResult(job, __instance, workGiversByPriority[i].tagToGive);
							return false;
						}
					}
				}
				pawn.mindState.priorityWork.Clear();
			}
			List<WorkGiver> list = (!__instance.emergency) ? pawn.workSettings.WorkGiversInOrderNormal : pawn.workSettings.WorkGiversInOrderEmergency;
			int num = -999;
			TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
			WorkGiver_Scanner scannerWhoProvidedTarget = null;
			WorkGiver_Scanner scanner;
			IntVec3 pawnPosition;
			float closestDistSquared;
			float bestPriority;
			bool prioritized;
			bool allowUnreachable;
			Danger maxPathDanger;
			for (int j = 0; j < list.Count; j++)
			{
				WorkGiver workGiver = list[j];
				if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
				{
					break;
				}
				if (!__instance.PawnCanUseWorkGiver(pawn, workGiver))
				{
					continue;
				}
				try
				{
					Job job2 = workGiver.NonScanJob(pawn);
					if (job2 != null)
					{
						__result = new ThinkResult(job2, __instance, workGiver.def.tagToGive);
						return false;
					}
					scanner = (workGiver as WorkGiver_Scanner);

					if (scanner != null)
					{
						if (scanner.def.scanThings)
						{
//----------------------THERE HAVE BEEN NO CHANGES ABOVE THIS---------------------------------

							Predicate<Thing> validator;
							if (scanner is WorkGiver_DoBill workGiver_DoBill)
							{
								validator = (Thing t) => !t.IsForbidden(pawn) && WorkGiver_Scanner_Patch.HasJobOnThing(workGiver_DoBill, pawn, t);
							}
							else
							{
								validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
							}
							IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
							Thing thing;
							if (scanner.Prioritized)
							{
								IEnumerable<Thing> enumerable2 = enumerable;
								if (enumerable2 == null)
								{
									enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
								}
								thing = ((!scanner.AllowUnreachable) ? 
									GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),    9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : 
									GenClosest.ClosestThing_Global(pawn.Position, enumerable2,     99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
							}
							else if (scanner.AllowUnreachable)
							{
								IEnumerable<Thing> enumerable3 = enumerable;
								if (enumerable3 == null)
								{
									enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
								}
								thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, validator);
							}
							else
							{                               
								if (
								   workGiver.def.defName.Equals("HaulGeneral")
								)
								{
									thing = HaulingCache.ClosestThingReachable(pawn, scanner, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
								}
								else if (scanner.PotentialWorkThingRequest.singleDef == null)
                                {
									//ThingRequestGroup
									//thing = GenClosest_Patch.ClosestThingReachable2(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
									thing = GenClosest_Patch.ClosestThingRequestGroup(pawn, scanner, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
								}
								else if(scanner.PotentialWorkThingRequest.group == ThingRequestGroup.Undefined)
								{
									//ThingDef singleDef
									thing = GenClosest_Patch.ClosestThingDef(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest.singleDef, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
								}
								else
                                {
									//Other PotentialWorkThingRequest
									thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
								}
							}
							if (thing != null)
							{
								bestTargetOfLastPriority = thing;
								scannerWhoProvidedTarget = scanner;
							}
						}
						if (scanner.def.scanCells)
						{
							pawnPosition = pawn.Position;
							closestDistSquared = 99999f;
							bestPriority = float.MinValue;
							prioritized = scanner.Prioritized;
							allowUnreachable = scanner.AllowUnreachable;
							maxPathDanger = scanner.MaxPathDanger(pawn);
							IEnumerable<IntVec3> enumerable4;
							if (scanner is WorkGiver_GrowerSow workGiver_Grower)
							{
								//RimThreaded.WorkGiver_GrowerSow_Patch_JobOnCell = 0;

								//thing = HaulingCache.ClosestThingReachable(pawn, scanner, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
								IntVec3 bestCell = WorkGiver_Grower_Patch.ClosestLocationReachable(workGiver_Grower, pawn);
								//Log.Message(bestCell.ToString());
								if (bestCell.IsValid)
								{
									bestTargetOfLastPriority = new TargetInfo(bestCell, pawn.Map);
									scannerWhoProvidedTarget = scanner;
								}
								//Log.Message(RimThreaded.WorkGiver_GrowerSow_Patch_JobOnCell.ToString());
							}
							else if(scanner is WorkGiver_GrowerHarvest workGiver_GrowerHarvest)
                            {
								IntVec3 bestCell = WorkGiver_GrowerHarvest_Patch.ClosestLocationReachable(workGiver_GrowerHarvest, pawn);
								if (bestCell.IsValid)
								{
									bestTargetOfLastPriority = new TargetInfo(bestCell, pawn.Map);
									scannerWhoProvidedTarget = scanner;
								}
								/*
								enumerable4 = workGiver_GrowerHarvest.PotentialWorkCellsGlobal(pawn);
								IList<IntVec3> list2;
								if ((list2 = (enumerable4 as IList<IntVec3>)) != null)
								{
									for (int k = 0; k < list2.Count; k++)
									{
										ProcessCell(list2[k]);
									}
								}
								else
								{
									foreach (IntVec3 item in enumerable4)
									{
										ProcessCell(item);
									}
								}
								*/
							}
							else
							{
								enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
								IList<IntVec3> list2;
								if ((list2 = (enumerable4 as IList<IntVec3>)) != null)
								{
									for (int k = 0; k < list2.Count; k++)
									{
										ProcessCell(list2[k]);
									}
								}
								else
								{
									foreach (IntVec3 item in enumerable4)
									{
										ProcessCell(item);
									}
								}
							}

						}
					}
					void ProcessCell(IntVec3 c)
					{
						float newDistanceSquared = (c - pawnPosition).LengthHorizontalSquared;
						float newPriority = 0f;

						if (prioritized)
						{
							newPriority = scanner.GetPriority(pawn, c);
							if (newPriority < bestPriority)
							{
								return;
							}
						}

						if (newDistanceSquared < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
						{
							if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
							{
								return;
							}

							bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
							scannerWhoProvidedTarget = scanner;
							closestDistSquared = newDistanceSquared;
							bestPriority = newPriority;
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ", ex.ToString()));
				}
				finally
				{
				}
				if (bestTargetOfLastPriority.IsValid)
				{
					Job job3 = (!bestTargetOfLastPriority.HasThing) ? scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell) : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
					if (job3 != null)
					{
						job3.workGiverDef = scannerWhoProvidedTarget.def;
						__result = new ThinkResult(job3, __instance, workGiver.def.tagToGive);
						return false;
					}

					//If this was a cached plant job, deregister it and check if it is still valid to be registered
                    if (scannerWhoProvidedTarget is WorkGiver_GrowerSow)
                    {
                        Map map = pawn.Map;
                        IntVec3 cell = bestTargetOfLastPriority.Cell;
						JumboCell.ReregisterObject(map, cell, RimThreaded.plantSowing_Cache);
                    }
					//HACK - I know. I'm awful.
					//Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
					if (Prefs.LogVerbose) {
						Log.Warning(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."));
					}
				}
				num = workGiver.def.priorityInType;

#if DEBUG
				int milli99 = (int)DateTime.Now.Subtract(startTime).TotalMilliseconds;
				if (milli99 > 100)
				{
					Log.Warning("99 JobGiver_Work.TryIssueJobPackage Took over " + milli99.ToString() + "ms for workGiver: " + workGiver.def.defName);
					//Log.Warning(scanner.PotentialWorkThingRequest.ToString());
					//Log.Warning(validator.ToString());
				}
#endif


			}
			__result = ThinkResult.NoJob;
			return false;
		}

		private static Job GiverTryGiveJobPrioritized(JobGiver_Work __instance, Pawn pawn, WorkGiver giver, IntVec3 cell)
		{
			if (!__instance.PawnCanUseWorkGiver(pawn, giver))
			{
				return null;
			}
			try
			{
				Job job = giver.NonScanJob(pawn);
				if (job != null)
				{
					return job;
				}
				WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
				if (scanner != null)
				{
					if (giver.def.scanThings)
					{
						Predicate<Thing> predicate;
						if (scanner is WorkGiver_DoBill workGiver_DoBill)
						{
							predicate = (Thing t) => !t.IsForbidden(pawn) && WorkGiver_Scanner_Patch.HasJobOnThing(workGiver_DoBill, pawn, t);
						}
						else
						{
							predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
						}

						List<Thing> thingList = cell.GetThingList(pawn.Map);
						for (int i = 0; i < thingList.Count; i++)
						{
							Thing thing = thingList[i];
							if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
							{
								Job job2 = scanner.JobOnThing(pawn, thing);
								if (job2 != null)
								{
									job2.workGiverDef = giver.def;
								}
								return job2;
							}
						}
					}
					if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
					{
						Job job3 = scanner.JobOnCell(pawn, cell);
						if (job3 != null)
						{
							job3.workGiverDef = giver.def;
						}
						return job3;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ", giver.def.defName, ": ", ex.ToString()));
			}
			return null;
		}
	}
}
