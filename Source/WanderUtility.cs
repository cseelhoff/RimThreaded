using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class WanderUtility_Patch
	{
        [ThreadStatic]
        static List<IntVec3> gatherSpots;

        [ThreadStatic]
        static List<IntVec3> candidateCells;

        [ThreadStatic]
        static List<Building> candidateBuildingsInRandomOrder;

        public static bool GetColonyWanderRoot(ref IntVec3 __result, Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                
                if(gatherSpots == null)
                {
                    gatherSpots = new List<IntVec3>();
                } else
                {
                    gatherSpots.Clear();
                }
                
                for (int index = 0; index < pawn.Map.gatherSpotLister.activeSpots.Count; ++index)
                {
                    IntVec3 position = pawn.Map.gatherSpotLister.activeSpots[index].parent.Position;
                    if (!position.IsForbidden(pawn) && pawn.CanReach(position, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))
                        gatherSpots.Add(position);
                }
                if (gatherSpots.Count > 0) {
                    __result = gatherSpots.RandomElement();
                    return false;
                }
            }
            //WanderUtility.candidateCells.Clear();
            if (candidateCells == null)
            {
                candidateCells = new List<IntVec3>();
            } else
            {
                candidateCells.Clear();
            }
            //WanderUtility.candidateBuildingsInRandomOrder.Clear();
            if (candidateBuildingsInRandomOrder == null)
            {
                candidateBuildingsInRandomOrder = new List<Building>();
            } else
            {
                candidateBuildingsInRandomOrder.Clear();
            }
            candidateBuildingsInRandomOrder.AddRange(pawn.Map.listerBuildings.allBuildingsColonist);
            candidateBuildingsInRandomOrder.Shuffle();
            int num = 0;
            int index1 = 0;
            while (index1 < candidateBuildingsInRandomOrder.Count)
            {
                if (num > 80 && candidateCells.Count > 0)
                {
                    __result = candidateCells.RandomElement();
                    return false;
                }
                Building building = candidateBuildingsInRandomOrder[index1];
                if ((building.def == ThingDefOf.Wall || building.def.building.ai_chillDestination) && (!building.Position.IsForbidden(pawn) && pawn.Map.areaManager.Home[building.Position]))
                {
                    IntVec3 c = GenAdjFast.AdjacentCells8Way(building).RandomElement();
                    if (c.Standable(building.Map) && !c.IsForbidden(pawn) && (pawn.CanReach(c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsInPrisonCell(pawn.Map)))
                    {
                        candidateCells.Add(c);
                        if ((pawn.Position - building.Position).LengthHorizontalSquared <= 1225)
                        {
                            __result = c;
                            return false;
                        }
                    }
                }
                ++index1;
                ++num;
            }
            Pawn result;
            Map map = pawn.Map;
            MapPawns mapPawns = map.mapPawns;
            List<Pawn> freeColonistsSpawned = mapPawns.FreeColonistsSpawned;

            List<Pawn> pawnList = new List<Pawn>(freeColonistsSpawned.Count);
            for (int i = 0; i < freeColonistsSpawned.Count; i++)
            {
                Pawn p;
                try
                {
                    p = freeColonistsSpawned[i];
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (p != null)
                {
                    IntVec3 position = p.Position;
                    if (position != null)
                    {
                        if (!p.Position.IsForbidden(pawn))
                        {
                            bool canReach = pawn.CanReach(
                                p.Position,
                                PathEndMode.Touch,
                                Danger.None,
                                false,
                                TraverseMode.ByPawn);
                            if (canReach)
                            {
                                pawnList.Add(p);
                            }
                        }
                    }
                }
            }
            __result = pawnList.TryRandomElement(out result) ?
                result.Position :
                pawn.Position;
            return false;
		}

	}
}
