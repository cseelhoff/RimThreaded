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

    public class Toils_Ingest_Patch
	{
        public static bool TryFindAdjacentIngestionPlaceSpot(ref bool __result,
              IntVec3 root,
              ThingDef ingestibleDef,
              Pawn pawn,
              out IntVec3 placeSpot)
        {
            placeSpot = IntVec3.Invalid;
            for (int index = 0; index < 4; ++index)
            {
                IntVec3 c = root + GenAdj.CardinalDirections[index];
                if (c.HasEatSurface(pawn.Map) && (!pawn.Map.thingGrid.ThingsAt(c).Where<Thing>((Func<Thing, bool>)(t => t.def == ingestibleDef)).Any<Thing>() && !c.IsForbidden(pawn)))
                {
                    placeSpot = c;
                    __result = true;
                    return false;
                }
            }
            if (!placeSpot.IsValid)
            {
                //Toils_Ingest.spotSearchList.Clear();
                List<IntVec3> spotSearchList = new List<IntVec3>();
                List<IntVec3> cardinals = ((IEnumerable<IntVec3>)GenAdj.CardinalDirections).ToList<IntVec3>();
                List<IntVec3> diagonals = ((IEnumerable<IntVec3>)GenAdj.DiagonalDirections).ToList<IntVec3>();
                cardinals.Shuffle<IntVec3>();
                for (int index = 0; index < 4; ++index)
                    spotSearchList.Add(cardinals[index]);
                diagonals.Shuffle<IntVec3>();
                for (int index = 0; index < 4; ++index)
                    spotSearchList.Add(diagonals[index]);
                spotSearchList.Add(IntVec3.Zero);
                for (int index = 0; index < spotSearchList.Count; ++index)
                {
                    IntVec3 c = root + spotSearchList[index];
                    if (c.Walkable(pawn.Map) && !c.IsForbidden(pawn) && !pawn.Map.thingGrid.ThingsAt(c).Where<Thing>((Func<Thing, bool>)(t => t.def == ingestibleDef)).Any<Thing>())
                    {
                        placeSpot = c;
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
