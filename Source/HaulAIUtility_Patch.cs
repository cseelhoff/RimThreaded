using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class HaulAIUtility_Patch
    {
        [ThreadStatic]
        static List<IntVec3> candidates;

        public static bool TryFindSpotToPlaceHaulableCloseTo(ref bool __result, Thing haulable, Pawn worker, IntVec3 center, out IntVec3 spot)
        {
            Region region = center.GetRegion(worker.Map);
            if (region == null)
            {
                spot = center;
                __result = false;
                return false;
            }

            TraverseParms traverseParms = TraverseParms.For(worker);
            IntVec3 foundCell = IntVec3.Invalid;
            if(candidates == null)
            {
                candidates = new List<IntVec3>();
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.Allows(traverseParms, isDestination: false), delegate (Region r)
            {
                candidates.Clear();
                candidates.AddRange(r.Cells);
                candidates.Sort((IntVec3 a, IntVec3 b) => a.DistanceToSquared(center).CompareTo(b.DistanceToSquared(center)));
                for (int i = 0; i < candidates.Count; i++)
                {
                    IntVec3 intVec = candidates[i];
                    if (HaulablePlaceValidator(haulable, worker, intVec))
                    {
                        foundCell = intVec;
                        return true;
                    }
                }

                return false;
            }, 100);
            if (foundCell.IsValid)
            {
                spot = foundCell;
                __result = true;
                return false;
            }

            spot = center;
            __result = false;
            return false;
        }
        private static bool HaulablePlaceValidator(Thing haulable, Pawn worker, IntVec3 c)
        {
            if (!worker.CanReserveAndReach(c, PathEndMode.OnCell, worker.NormalMaxDanger()))
            {
                return false;
            }

            if (GenPlace.HaulPlaceBlockerIn(haulable, c, worker.Map, checkBlueprintsAndFrames: true) != null)
            {
                return false;
            }

            if (!c.Standable(worker.Map))
            {
                return false;
            }

            if (c == haulable.Position && haulable.Spawned)
            {
                return false;
            }

            if (c.ContainsStaticFire(worker.Map))
            {
                return false;
            }

            if (haulable != null && haulable.def.BlocksPlanting() && worker.Map.zoneManager.ZoneAt(c) is Zone_Growing)
            {
                return false;
            }

            if (haulable.def.passability != 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 c2 = c + GenAdj.AdjacentCells[i];
                    if (worker.Map.designationManager.DesignationAt(c2, DesignationDefOf.Mine) != null)
                    {
                        return false;
                    }
                }
            }

            Building edifice = c.GetEdifice(worker.Map);
            if (edifice != null && edifice is Building_Trap)
            {
                return false;
            }

            return true;
        }

    }
}
