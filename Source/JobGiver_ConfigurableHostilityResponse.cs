using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class JobGiver_ConfigurableHostilityResponse_Patch
    {
		public static AccessTools.FieldRef<AutoUndrafter, Pawn> pawn =
			AccessTools.FieldRefAccess<AutoUndrafter, Pawn>("pawn");
        public static bool TryGetFleeJob(JobGiver_ConfigurableHostilityResponse __instance, ref Job __result, Pawn pawn)
        {
            if (!SelfDefenseUtility.ShouldStartFleeing(pawn))
            {
                __result = null;
                return false;
            }

            IntVec3 c;
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.FleeAndCower)
            {
                c = pawn.CurJob.targetA.Cell;
            }
            else
            {
                //tmpThreats.Clear();
                List<Thing> tmpThreats = new List<Thing>();
                List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
                for (int i = 0; i < potentialTargetsFor.Count; i++)
                {
                    Thing thing = potentialTargetsFor[i].Thing;
                    if (SelfDefenseUtility.ShouldFleeFrom(thing, pawn, checkDistance: false, checkLOS: false))
                    {
                        tmpThreats.Add(thing);
                    }
                }

                List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.AlwaysFlee);
                for (int j = 0; j < list.Count; j++)
                {
                    Thing thing2 = list[j];
                    if (SelfDefenseUtility.ShouldFleeFrom(thing2, pawn, checkDistance: false, checkLOS: false))
                    {
                        tmpThreats.Add(thing2);
                    }
                }

                if (!tmpThreats.Any())
                {
                    Log.Error(pawn.LabelShort + " decided to flee but there is not any threat around.");
                    Region region = pawn.GetRegion();
                    if (region == null)
                    {
                        __result = null;
                        return false;
                    }

                    RegionTraverser.BreadthFirstTraverse(region, (Region from, Region reg) => reg.door == null || reg.door.Open, delegate (Region reg)
                    {
                        List<Thing> list2 = reg.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
                        for (int k = 0; k < list2.Count; k++)
                        {
                            Thing thing3 = list2[k];
                            if (SelfDefenseUtility.ShouldFleeFrom(thing3, pawn, checkDistance: false, checkLOS: false))
                            {
                                tmpThreats.Add(thing3);
                                Log.Warning($"  Found a viable threat {thing3.LabelShort}; tests are {thing3.Map.attackTargetsCache.Debug_CheckIfInAllTargets(thing3 as IAttackTarget)}, {thing3.Map.attackTargetsCache.Debug_CheckIfHostileToFaction(pawn.Faction, thing3 as IAttackTarget)}, {thing3 is IAttackTarget}");
                            }
                        }

                        return false;
                    }, 9);
                    if (!tmpThreats.Any())
                    {
                        __result = null;
                        return false;
                    }
                }

                c = CellFinderLoose.GetFleeDest(pawn, tmpThreats);
                //tmpThreats.Clear();
            }

            __result = JobMaker.MakeJob(JobDefOf.FleeAndCower, c);
            return false;
        }

    }
}
