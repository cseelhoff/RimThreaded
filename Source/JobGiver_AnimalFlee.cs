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

    public class JobGiver_AnimalFlee_Patch
	{
        private static Job FleeJob2(Pawn pawn, Thing danger)
        {
            IntVec3 intVec;
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Flee)
            {
                intVec = pawn.CurJob.targetA.Cell;
            }
            else
            {
                //tmpThings.Clear();
                List<Thing> tmpThings = new List<Thing>
                {                    
                    danger
                };
                intVec = CellFinderLoose.GetFleeDest(pawn, tmpThings, 24f);
                //tmpThings.Clear();
            }

            if (intVec != pawn.Position)
            {
                return JobMaker.MakeJob(JobDefOf.Flee, intVec, danger);
            }

            return null;
        }
        public static bool FleeLargeFireJob(JobGiver_AnimalFlee __instance, ref Job __result, Pawn pawn)
        {
            if (pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Fire).Count < 60)
            {
                __result = null;
                return false;
            }

            TraverseParms tp = TraverseParms.For(pawn);
            Fire closestFire = null;
            float closestDistSq = -1f;
            int firesCount = 0;

            BreadthFirstTraverse(pawn, tp, closestDistSq, closestFire, firesCount);

            if (closestDistSq <= 100f && firesCount >= 60)
            {
                Job job = FleeJob2(pawn, closestFire);
                if (job != null)
                {
                    __result = job;
                    return false;
                }
            }

            __result = null;
            return false;
        }

        private static int BreadthFirstTraverse(Pawn pawn, TraverseParms tp, float closestDistSq, Fire closestFire, int firesCount)
        {
            RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate (Region x)
            {
                List<Thing> list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Fire);
                for (int i = 0; i < list.Count; i++)
                {
                    float num = pawn.Position.DistanceToSquared(list[i].Position);
                    if (!(num > 400f))
                    {
                        if (closestFire == null || num < closestDistSq)
                        {
                            closestDistSq = num;
                            closestFire = (Fire)list[i];
                        }
                        firesCount++;
                    }
                }

                return closestDistSq <= 100f && firesCount >= 60;
            }, 18);
            return firesCount;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobGiver_AnimalFlee);
            Type patched = typeof(JobGiver_AnimalFlee_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FleeLargeFireJob");
        }
    }
}
