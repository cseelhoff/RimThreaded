using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{

	public class JobDriver_Wait_Patch
	{
		public static bool CheckForAutoAttack(Verse.AI.JobDriver_Wait __instance)
		{
			Pawn this_pawn = __instance.pawn;
			if (this_pawn.Downed || this_pawn.stances.FullBodyBusy)
			{
				return false;
			}
			Map map = this_pawn.Map;
			List<Thing> fireList = map.listerThings.ThingsOfDef(ThingDefOf.Fire);

			bool flag = PawnUtility.EnemiesAreNearby(this_pawn, 1) && !this_pawn.WorkTagIsDisabled(WorkTags.Violent);
			bool flag2 = this_pawn.RaceProps.ToolUser && this_pawn.Faction == Faction.OfPlayer && !this_pawn.WorkTagIsDisabled(WorkTags.Firefighting) && (!this_pawn.InMentalState || this_pawn.MentalState.def.allowBeatfire);

			if (!flag && !flag2)
				return false;

			__instance.collideWithPawns = false;

			Fire fire = null;
			for (int i = 0; i < 9; i++)
			{
				IntVec3 c = this_pawn.Position + GenAdj.AdjacentCellsAndInside[i];
				if (c.InBounds(map))
				{
					Thing[] thingList;
					List<Thing> thingList1 = c.GetThingList(map);
					lock (thingList1)
					{
                        thingList = thingList1.ToArray();
                    }
					for (int j = 0; j < thingList.Length; j++)
					{
						if (flag)
						{
							Pawn pawn = thingList[j] as Pawn;
							if (pawn != null && !pawn.Downed && this_pawn.HostileTo(pawn) && GenHostility.IsActiveThreatTo(pawn, this_pawn.Faction))
							{
								this_pawn.meleeVerbs.TryMeleeAttack(pawn, null, false);
								__instance.collideWithPawns = true;
								return false;
							}
						}
						if (flag2)
						{
							Fire fire2 = thingList[j] as Fire;
							if (fire2 != null && (fire == null || fire2.fireSize < fire.fireSize || i == 8) && (fire2.parent == null || fire2.parent != this_pawn))
							{
								fire = fire2;
							}
						}
					}					
				}
			}
			if (fire != null)
			{
				this_pawn.natives.TryBeatFire(fire);
				return false;
			}
			if (flag && __instance.job.canUseRangedWeapon && this_pawn.Faction != null && __instance.job.def == JobDefOf.Wait_Combat && (this_pawn.drafter == null || this_pawn.drafter.FireAtWill))
			{
				Verb currentEffectiveVerb = this_pawn.CurrentEffectiveVerb;
				if (currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack)
				{
					TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
					if (currentEffectiveVerb.IsIncendiary())
					{
						targetScanFlags |= TargetScanFlags.NeedNonBurning;
					}
					Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this_pawn, targetScanFlags, null, 0f, 9999f);
					if (thing != null)
					{
						this_pawn.TryStartAttack(thing);
						__instance.collideWithPawns = true;
						return false;
					}
				}
			}
			
			return false;
		}
	}

}
