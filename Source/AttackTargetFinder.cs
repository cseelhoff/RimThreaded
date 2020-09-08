using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using UnityEngine;

namespace RimThreaded
{

    public class AttackTargetFinder_Patch
    {

		private static IAttackTarget FindBestReachableMeleeTarget(
		  Predicate<IAttackTarget> validator,
		  Pawn searcherPawn,
		  float maxTargDist,
		  bool canBash)
		{
			maxTargDist = Mathf.Min(maxTargDist, 30f);
			IAttackTarget reachableTarget = (IAttackTarget)null;
			Func<IntVec3, IAttackTarget> bestTargetOnCell = (Func<IntVec3, IAttackTarget>)(x =>
			{
				List<Thing> thingList = x.GetThingList(searcherPawn.Map);
				for (int index = 0; index < thingList.Count; ++index)
				{
					Thing thing = thingList[index];
					if (thing is IAttackTarget target && validator(target) && ReachabilityImmediate.CanReachImmediate(x, (LocalTargetInfo)thing, searcherPawn.Map, PathEndMode.Touch, searcherPawn) && (searcherPawn.CanReachImmediate((LocalTargetInfo)thing, PathEndMode.Touch) || searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn, target)))
						return target;
				}
				return (IAttackTarget)null;
			});
			searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, (Predicate<IntVec3>)(x => x.Walkable(searcherPawn.Map) && (double)x.DistanceToSquared(searcherPawn.Position) <= (double)maxTargDist * (double)maxTargDist && (canBash || !(x.GetEdifice(searcherPawn.Map) is Building_Door edifice) || edifice.CanPhysicallyPass(searcherPawn)) && !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, true, false, false)), (Func<IntVec3, bool>)(x =>
			{
				for (int index = 0; index < 8; ++index)
				{
					IntVec3 c = x + GenAdj.AdjacentCells[index];
					if (c.InBounds(searcherPawn.Map))
					{
						IAttackTarget attackTarget = bestTargetOnCell(c);
						if (attackTarget != null)
						{
							reachableTarget = attackTarget;
							break;
						}
					}
				}
				return reachableTarget != null;
			}), int.MaxValue, false, (IEnumerable<IntVec3>)null);
			return reachableTarget;
		}

		private static float FriendlyFireConeTargetScoreOffset(
		  IAttackTarget target,
		  IAttackTargetSearcher searcher,
		  Verb verb)
		{
			if (!(searcher.Thing is Pawn thing) || thing.RaceProps.intelligence < Intelligence.ToolUser || (thing.RaceProps.IsMechanoid || !(verb is Verb_Shoot verbShoot)))
				return 0.0f;
			ThingDef defaultProjectile = verbShoot.verbProps.defaultProjectile;
			if (defaultProjectile == null || defaultProjectile.projectile.flyOverhead)
				return 0.0f;
			Map map = thing.Map;
			ShotReport report = ShotReport.HitReportFor((Thing)thing, verb, (LocalTargetInfo)(Thing)target);
			double forcedMissRadius = (double)verb.verbProps.forcedMissRadius;
			IntVec3 dest1 = report.ShootLine.Dest;
			ShootLine shootLine = report.ShootLine;
			IntVec3 source = shootLine.Source;
			IntVec3 vector = dest1 - source;
			float radius = Mathf.Max(VerbUtility.CalculateAdjustedForcedMiss((float)forcedMissRadius, vector), 1.5f);
			shootLine = report.ShootLine;
			IEnumerable<IntVec3> intVec3s = GenRadial.RadialCellsAround(shootLine.Dest, radius, true).Where<IntVec3>((Func<IntVec3, bool>)(dest => dest.InBounds(map))).Select<IntVec3, ShootLine>((Func<IntVec3, ShootLine>)(dest => new ShootLine(report.ShootLine.Source, dest))).SelectMany<ShootLine, IntVec3>((Func<ShootLine, IEnumerable<IntVec3>>)(line => line.Points().Concat<IntVec3>(line.Dest).TakeWhile<IntVec3>((Func<IntVec3, bool>)(pos => pos.CanBeSeenOverFast(map))))).Distinct<IntVec3>();
			float num1 = 0.0f;
			foreach (IntVec3 c in intVec3s)
			{
				shootLine = report.ShootLine;
				float num2 = VerbUtility.InterceptChanceFactorFromDistance(shootLine.Source.ToVector3Shifted(), c);
				if ((double)num2 > 0.0)
				{
					List<Thing> thingList = c.GetThingList(map);
					for (int index = 0; index < thingList.Count; ++index)
					{
						Thing b = thingList[index];
						if (b is IAttackTarget && b != target)
						{
							float num3 = (b != searcher ? (!(b is Pawn) ? 10f : (b.def.race.Animal ? 7f : 18f)) : 40f) * num2;
							float num4 = !searcher.Thing.HostileTo(b) ? num3 * -1f : num3 * 0.6f;
							num1 += num4;
						}
					}
				}
			}
			return num1;
		}

		private static float FriendlyFireBlastRadiusTargetScoreOffset(
			  IAttackTarget target,
			  IAttackTargetSearcher searcher,
			  Verb verb)
		{
			if ((double)verb.verbProps.ai_AvoidFriendlyFireRadius <= 0.0)
				return 0.0f;
			Map map = target.Thing.Map;
			IntVec3 position = target.Thing.Position;
			int num1 = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
			float num2 = 0.0f;
			for (int index1 = 0; index1 < num1; ++index1)
			{
				IntVec3 intVec3 = position + GenRadial.RadialPattern[index1];
				if (intVec3.InBounds(map))
				{
					bool flag = true;
					List<Thing> thingList = intVec3.GetThingList(map);
					for (int index2 = 0; index2 < thingList.Count; ++index2)
					{
						if (thingList[index2] is IAttackTarget && thingList[index2] != target)
						{
							if (flag)
							{
								if (GenSight.LineOfSight(position, intVec3, map, true, (Func<IntVec3, bool>)null, 0, 0))
									flag = false;
								else
									break;
							}
							float num3 = thingList[index2] != searcher ? (!(thingList[index2] is Pawn) ? 10f : (thingList[index2].def.race.Animal ? 7f : 18f)) : 40f;
							if (searcher.Thing.HostileTo(thingList[index2]))
								num2 += num3 * 0.6f;
							else
								num2 -= num3;
						}
					}
				}
			}
			return num2;
		}


		private static float GetShootingTargetScore(
		  IAttackTarget target,
		  IAttackTargetSearcher searcher,
		  Verb verb)
		{
			float num1 = 60f - Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
			if (target.TargetCurrentlyAimingAt == (LocalTargetInfo)searcher.Thing)
				num1 += 10f;
			if (searcher.LastAttackedTarget == (LocalTargetInfo)target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
				num1 += 40f;
			float num2 = num1 - CoverUtility.CalculateOverallBlockChance((LocalTargetInfo)target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
			if (target is Pawn pawn && pawn.RaceProps.Animal && (pawn.Faction != null && !pawn.IsFighting()))
				num2 -= 50f;
			return (num2 + FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb) + FriendlyFireConeTargetScoreOffset(target, searcher, verb)) * target.TargetPriorityFactor;
		}

		private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(
			  List<IAttackTarget> rawTargets,
			  IAttackTargetSearcher searcher,
			  Verb verb)
		{
			List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();
			//AttackTargetFinder.availableShootingTargets.Clear();
			if (rawTargets.Count == 0)
				return availableShootingTargets;
			//AttackTargetFinder.tmpTargetScores.Clear();
			List<float> tmpTargetScores = new List<float>();
			//AttackTargetFinder.tmpCanShootAtTarget.Clear();
			List<bool> tmpCanShootAtTarget = new List<bool>();
			float b = 0.0f;
			IAttackTarget first = (IAttackTarget)null;
			for (int index = 0; index < rawTargets.Count; ++index)
			{
				tmpTargetScores.Add(float.MinValue);
				tmpCanShootAtTarget.Add(false);
				if (rawTargets[index] != searcher)
				{
					bool flag = CanShootAtFromCurrentPosition(rawTargets[index], searcher, verb);
					tmpCanShootAtTarget[index] = flag;
					if (flag)
					{
						float shootingTargetScore = GetShootingTargetScore(rawTargets[index], searcher, verb);
						tmpTargetScores[index] = shootingTargetScore;
						if (first == null || (double)shootingTargetScore > (double)b)
						{
							first = rawTargets[index];
							b = shootingTargetScore;
						}
					}
				}
			}
			if ((double)b < 1.0)
			{
				if (first != null)
					availableShootingTargets.Add(new Pair<IAttackTarget, float>(first, 1f));
			}
			else
			{
				float num = b - 30f;
				for (int index = 0; index < rawTargets.Count; ++index)
				{
					if (rawTargets[index] != searcher && tmpCanShootAtTarget[index])
					{
						float tmpTargetScore = tmpTargetScores[index];
						if ((double)tmpTargetScore >= (double)num)
						{
							float second = Mathf.InverseLerp(b - 30f, b, tmpTargetScore);
							availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[index], second));
						}
					}
				}
			}
			return availableShootingTargets;
		}


		private static IAttackTarget GetRandomShootingTargetByScore(
		  List<IAttackTarget> targets,
		  IAttackTargetSearcher searcher,
		  Verb verb)
		{
			Pair<IAttackTarget, float> result;
			return GetAvailableShootingTargetsByScore(targets, searcher, verb).TryRandomElementByWeight<Pair<IAttackTarget, float>>((Func<Pair<IAttackTarget, float>, float>)(x => x.Second), out result) ? result.First : (IAttackTarget)null;
		}
		private static bool CanShootAtFromCurrentPosition(
		  IAttackTarget target,
		  IAttackTargetSearcher searcher,
		  Verb verb)
		{
			return verb != null && verb.CanHitTargetFrom(searcher.Thing.Position, (LocalTargetInfo)target.Thing);
		}

		private static bool CanReach(Thing searcher, Thing target, bool canBash)
		{
			if (searcher is Pawn pawn)
			{
				if (!pawn.CanReach((LocalTargetInfo)target, PathEndMode.Touch, Danger.Some, canBash, TraverseMode.ByPawn))
					return false;
			}
			else
			{
				TraverseMode mode = canBash ? TraverseMode.PassDoors : TraverseMode.NoPassClosedDoors;
				if (!searcher.Map.reachability.CanReach(searcher.Position, (LocalTargetInfo)target, PathEndMode.Touch, TraverseParms.For(mode, Danger.Deadly, false)))
					return false;
			}
			return true;
		}

		private static bool HasRangedAttack(IAttackTargetSearcher t)
		{
			Verb currentEffectiveVerb = t.CurrentEffectiveVerb;
			return currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack;
		}

		public static bool BestAttackTarget(ref IAttackTarget __result, IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
		{
			Thing searcherThing = searcher.Thing;
			Pawn searcherPawn = searcher as Pawn;
			Verb verb = searcher.CurrentEffectiveVerb;
			if (verb == null)
			{
				Log.Error("BestAttackTarget with " + searcher.ToStringSafe<IAttackTargetSearcher>() + " who has no attack verb.", false);
				__result = null;
				return false;
			}
			bool onlyTargetMachines = verb.IsEMP();
			float minDistSquared = minDist * minDist;
			float num = maxTravelRadiusFromLocus + verb.verbProps.range;
			float maxLocusDistSquared = num * num;
			Func<IntVec3, bool> losValidator = null;
			if ((flags & TargetScanFlags.LOSBlockableByGas) != TargetScanFlags.None)
			{
				losValidator = delegate (IntVec3 vec3)
				{
					Gas gas = vec3.GetGas(searcherThing.Map);
					return gas == null || !gas.def.gas.blockTurretTracking;
				};
			}
			Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
			{
				Thing thing = t.Thing;
				if (t == searcher)
				{
					return false;
				}
				if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
				{
					return false;
				}
				if (!canTakeTargetsCloserThanEffectiveMinRange)
				{
					float num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
					if (num2 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
					{
						return false;
					}
				}
				if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
				{
					return false;
				}
				if (!searcherThing.HostileTo(thing))
				{
					return false;
				}
				if (validator != null && !validator(thing))
				{
					return false;
				}
				if (searcherPawn != null)
				{
					Lord lord = searcherPawn.GetLord();
					if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
					{
						return false;
					}
				}
				if ((flags & TargetScanFlags.NeedNotUnderThickRoof) != TargetScanFlags.None)
				{
					RoofDef roof = thing.Position.GetRoof(thing.Map);
					if (roof != null && roof.isThickRoof)
					{
						return false;
					}
				}
				if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None)
				{
					if (losValidator != null && (!losValidator(searcherThing.Position) || !losValidator(thing.Position)))
					{
						return false;
					}
					if (!searcherThing.CanSee(thing, losValidator))
					{
						if (t is Pawn)
						{
							if ((flags & TargetScanFlags.NeedLOSToPawns) != TargetScanFlags.None)
							{
								return false;
							}
						}
						else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != TargetScanFlags.None)
						{
							return false;
						}
					}
				}
				if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None) && t.ThreatDisabled(searcher))
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None && !AttackTargetFinder.IsAutoTargetable(t))
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedActiveThreat) != TargetScanFlags.None && !GenHostility.IsActiveThreatTo(t, searcher.Thing.Faction))
				{
					return false;
				}
				Pawn pawn = t as Pawn;
				if (onlyTargetMachines && pawn != null && pawn.RaceProps.IsFlesh)
				{
					return false;
				}
				if ((flags & TargetScanFlags.NeedNonBurning) != TargetScanFlags.None && thing.IsBurning())
				{
					return false;
				}
				if (searcherThing.def.race != null && searcherThing.def.race.intelligence >= Intelligence.Humanlike)
				{
					CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
					if (compExplosive != null && compExplosive.wickStarted)
					{
						return false;
					}
				}
				if (thing.def.size.x == 1 && thing.def.size.z == 1)
				{
					if (thing.Position.Fogged(thing.Map))
					{
						return false;
					}
				}
				else
				{
					bool flag2 = false;
					using (CellRect.Enumerator enumerator = thing.OccupiedRect().GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (!enumerator.Current.Fogged(thing.Map))
							{
								flag2 = true;
								break;
							}
						}
					}
					if (!flag2)
					{
						return false;
					}
				}
				return true;
			};
			if (HasRangedAttack(searcher) && (searcherPawn == null || !searcherPawn.InAggroMentalState))
			{
				List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
				//AttackTargetFinder.tmpTargets.Clear();
				tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
				if ((flags & TargetScanFlags.NeedReachable) != TargetScanFlags.None)
				{
					Predicate<IAttackTarget> oldValidator = innerValidator;
					innerValidator = ((IAttackTarget t) => oldValidator(t) && CanReach(searcherThing, t.Thing, canBash));
				}
				bool flag = false;
				for (int i = 0; i < tmpTargets.Count; i++)
				{
					IAttackTarget attackTarget = tmpTargets[i];
					if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
					{
						flag = true;
						break;
					}
				}
				IAttackTarget result;
				if (flag)
				{
					tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
					result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb);
				}
				else
				{
					Predicate<Thing> validator2;
					if ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != TargetScanFlags.None && (flags & TargetScanFlags.NeedReachable) == TargetScanFlags.None)
					{
						validator2 = ((Thing t) => innerValidator((IAttackTarget)t) && (CanReach(searcherThing, t, canBash) || CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)));
					}
					else
					{
						validator2 = ((Thing t) => innerValidator((IAttackTarget)t));
					}
					result = (IAttackTarget)GenClosest.ClosestThing_Global(searcherThing.Position, tmpTargets, maxDist, validator2, null);
				}
				tmpTargets.Clear();
				__result = result;
				return false;
			}
			if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f && !searcherPawn.InMentalState)
			{
				Predicate<IAttackTarget> oldValidator = innerValidator;
				innerValidator = ((IAttackTarget t) => oldValidator(t) && t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius));
			}
			IAttackTarget attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(searcherThing.Position, searcherThing.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.Touch, TraverseParms.For(searcherPawn, Danger.Deadly, TraverseMode.ByPawn, canBash), maxDist, (Thing x) => innerValidator((IAttackTarget)x), null, 0, (maxDist > 800f) ? -1 : 40, false, RegionType.Set_Passable, false);
			if (attackTarget2 != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
			{
				IAttackTarget attackTarget3 = FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
				if (attackTarget3 != null)
				{
					float lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
					float lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
					if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
					{
						attackTarget2 = attackTarget3;
					}
				}
			}
			__result = attackTarget2;
			return false;
		}

		public static bool CanSee(ref bool __result, Thing seer, Thing target, Func<IntVec3, bool> validator = null)
		{
			List<IntVec3> tempDestList = new List<IntVec3>();
			List<IntVec3> tempSourceList = new List<IntVec3>();
			ShootLeanUtility.CalcShootableCellsOf(tempDestList, target);
			for (int index = 0; index < tempDestList.Count; ++index)
			{
				if (GenSight.LineOfSight(seer.Position, tempDestList[index], seer.Map, true, validator, 0, 0)) {
					__result = true;
					return false;
				}
			}
			ShootLeanUtility.LeanShootingSourcesFromTo(seer.Position, target.Position, seer.Map, tempSourceList);
			for (int index1 = 0; index1 < tempSourceList.Count; ++index1)
			{
				for (int index2 = 0; index2 < tempDestList.Count; ++index2)
				{
					if (GenSight.LineOfSight(tempSourceList[index1], tempDestList[index2], seer.Map, true, validator, 0, 0))
					{
						__result = true;
						return false;
					}
				}
			}
			__result = false;
			return false;
		}

	}
}
