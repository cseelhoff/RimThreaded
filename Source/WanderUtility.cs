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

    public class WanderUtility_Patch
	{
		public static bool GetColonyWanderRoot(ref IntVec3 __result, Pawn pawn)
    {
            if (pawn.RaceProps.Humanlike)
            {
                //WanderUtility.gatherSpots.Clear();
                List<IntVec3> gatherSpots = new List<IntVec3>();
                for (int index = 0; index < pawn.Map.gatherSpotLister.activeSpots.Count; ++index)
                {
                    IntVec3 position = pawn.Map.gatherSpotLister.activeSpots[index].parent.Position;
                    if (!position.IsForbidden(pawn) && pawn.CanReach((LocalTargetInfo)position, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))
                        gatherSpots.Add(position);
                }
                if (gatherSpots.Count > 0) {
                    __result = gatherSpots.RandomElement<IntVec3>();
                    return false;
                }
            }
            //WanderUtility.candidateCells.Clear();
            List<IntVec3> candidateCells = new List<IntVec3>();
            //WanderUtility.candidateBuildingsInRandomOrder.Clear();
            List<Building> candidateBuildingsInRandomOrder = new List<Building>();
            candidateBuildingsInRandomOrder.AddRange((IEnumerable<Building>)pawn.Map.listerBuildings.allBuildingsColonist);
            candidateBuildingsInRandomOrder.Shuffle<Building>();
            int num = 0;
            int index1 = 0;
            while (index1 < candidateBuildingsInRandomOrder.Count)
            {
                if (num > 80 && candidateCells.Count > 0)
                {
                    __result = candidateCells.RandomElement<IntVec3>();
                    return false;
                }
                Building building = candidateBuildingsInRandomOrder[index1];
                if ((building.def == ThingDefOf.Wall || building.def.building.ai_chillDestination) && (!building.Position.IsForbidden(pawn) && pawn.Map.areaManager.Home[building.Position]))
                {
                    IntVec3 c = GenAdjFast.AdjacentCells8Way((LocalTargetInfo)(Thing)building).RandomElement<IntVec3>();
                    if (c.Standable(building.Map) && !c.IsForbidden(pawn) && (pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsInPrisonCell(pawn.Map)))
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
            __result = pawn.Map.mapPawns.FreeColonistsSpawned.Where<Pawn>((Func<Pawn, bool>)(c => !c.Position.IsForbidden(pawn) && pawn.CanReach((LocalTargetInfo)c.Position, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))).TryRandomElement<Pawn>(out result) ? result.Position : pawn.Position;
			return false;
		}

	}
}
