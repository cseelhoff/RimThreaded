using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

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
		private static readonly MethodInfo methodWorkGiversRelated =
			Method(typeof(JobGiver_Work), "WorkGiversRelated", new Type[] { typeof(WorkGiverDef), typeof(WorkGiverDef) });
		private static readonly Func<JobGiver_Work, WorkGiverDef, WorkGiverDef, bool> funcWorkGiversRelated =
			(Func<JobGiver_Work, WorkGiverDef, WorkGiverDef, bool>)Delegate.CreateDelegate(
				typeof(Func<JobGiver_Work, WorkGiverDef, WorkGiverDef, bool>), methodWorkGiversRelated);

		private static readonly MethodInfo methodPawnCanUseWorkGiver =
			Method(typeof(JobGiver_Work), "PawnCanUseWorkGiver", new Type[] { typeof(Pawn), typeof(WorkGiver) });
		private static readonly Func<JobGiver_Work, Pawn, WorkGiver, bool> funcPawnCanUseWorkGiver =
			(Func<JobGiver_Work, Pawn, WorkGiver, bool>)Delegate.CreateDelegate(
				typeof(Func<JobGiver_Work, Pawn, WorkGiver, bool>), methodPawnCanUseWorkGiver);


		public static bool TryIssueJobPackage(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
		{
			if (__instance.emergency && pawn.mindState.priorityWork.IsPrioritized)
			{
				List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
				for (int i = 0; i < workGiversByPriority.Count; i++)
				{
					WorkGiver worker = workGiversByPriority[i].Worker;
					if (funcWorkGiversRelated(__instance, pawn.mindState.priorityWork.WorkGiver, worker.def))
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
				if (!funcPawnCanUseWorkGiver(__instance, pawn, workGiver))
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
								//TODO: use better ThingRequest groups
								if (
									workGiver.def.defName.Equals("DoctorFeedAnimals") ||
									workGiver.def.defName.Equals("DoctorFeedHumanlikes") ||
									workGiver.def.defName.Equals("DoctorTendToAnimals") ||
									workGiver.def.defName.Equals("DoctorTendToHumanlikes") ||
									workGiver.def.defName.Equals("DoBillsUseCraftingSpot") ||
									workGiver.def.defName.Equals("DoctorTendEmergency") ||
									workGiver.def.defName.Equals("HaulCorpses") ||
									workGiver.def.defName.Equals("FillFermentingBarrel") ||
									workGiver.def.defName.Equals("HandlingFeedPatientAnimals") ||
									workGiver.def.defName.Equals("Train") ||
									workGiver.def.defName.Equals("VisitSickPawn") ||
									workGiver.def.defName.Equals("DoBillsButcherFlesh") ||
									workGiver.def.defName.Equals("DoBillsCook") ||
									workGiver.def.defName.Equals("DoBillsMakeApparel")
								)
								{
									//ClosestThingReachable2 checks validator before CanReach
									DateTime startTime = DateTime.Now;

									//long
									thing = GenClosest_Patch.ClosestThingReachable2(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);

									if (DateTime.Now.Subtract(startTime).TotalMilliseconds > 200)
									{
										Log.Warning("ClosestThingReachable2 Took over 200ms for workGiver: " + workGiver.def.defName);
									}
								}
								else if (
								   workGiver.def.defName.Equals("HaulGeneral")
								)
								{
									//ClosestThingReachable2 checks validator before CanReach
									DateTime startTime = DateTime.Now;

									//long
									thing = HaulingCache.ClosestThingReachable(pawn, scanner, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);

									if (DateTime.Now.Subtract(startTime).TotalMilliseconds > 100)
									{
										Log.Warning("HaulingCache.ClosestThingReachable Took over 100ms for HaulGeneral: " + workGiver.def.defName);
										Log.Warning(scanner.PotentialWorkThingRequest.ToString());
										Log.Warning(validator.ToString());
									}
								}
								/*
								else if(
										workGiver.def.defName.Equals("DoBillsButcherFlesh") ||
										workGiver.def.defName.Equals("DoBillsCook") ||
										workGiver.def.defName.Equals("DoBillsMakeApparel")) 
								{
									
									thing = null;
									//ThingGrid_Patch
									int mapSizeX = pawn.Map.Size.x;
									int mapSizeZ = pawn.Map.Size.z;
									int index = pawn.Map.cellIndices.CellToIndex(pawn.Position);
									//Dictionary<Bill, float> billPointsDict = ThingGrid_Patch.thingBillPoints[t.def];
									Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>> ingredientDict = ThingGrid_Patch.mapIngredientDict[pawn.Map];
									ThingRequest thingReq = scanner.PotentialWorkThingRequest;
									if(!ingredientDict.TryGetValue(scanner, out Dictionary<float, List<HashSet<Thing>[]>> scoreToJumboCellsList)) {
										scoreToJumboCellsList = new Dictionary<float, List<HashSet<Thing>[]>>();
										List<Thing> thingsMatchingRequest = pawn.Map.listerThings.ThingsMatching(thingReq);
									}
									
								}
								*/
								else
                                {
									DateTime startTime = DateTime.Now;
									//long
									thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
									if (DateTime.Now.Subtract(startTime).TotalMilliseconds > 200)
									{
										Log.Warning("ClosestThingReachable Took over 200ms for workGiver: " + workGiver.def.defName);
									}
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
							if (scanner is WorkGiver_Grower workGiver_Grower)
							{
								RimThreaded.WorkGiver_GrowerSow_Patch_JobOnCell = 0;

								//thing = HaulingCache.ClosestThingReachable(pawn, scanner, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);

								IntVec3 bestCell = WorkGiver_Grower_Patch.ClosestLocationReachable(workGiver_Grower, pawn);
								Log.Message(bestCell.ToString());
								if (bestCell.IsValid)
								{
									bestTargetOfLastPriority = new TargetInfo(bestCell, pawn.Map);
									scannerWhoProvidedTarget = scanner;
								}
								//Log.Message(RimThreaded.WorkGiver_GrowerSow_Patch_JobOnCell.ToString());
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
						__result = new ThinkResult(job3, __instance, list[j].def.tagToGive);
						return false;
					}
					//HACK - I know. I'm awful.
					//Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
					Log.Warning(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."));
				}
				num = workGiver.def.priorityInType;
			}
			__result = ThinkResult.NoJob;
			return false;
		}

		private static Job GiverTryGiveJobPrioritized(JobGiver_Work __instance, Pawn pawn, WorkGiver giver, IntVec3 cell)
		{
			if (!funcPawnCanUseWorkGiver(__instance, pawn, giver))
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
