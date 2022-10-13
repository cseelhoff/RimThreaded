using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RimThreaded.RW_Patches
{
    public class WorkGiver_DoBill_RegionProcessor
    {
        public List<Thing> newRelevantThings = new List<Thing>();
        public List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();
        public List<Thing> relevantThings = new List<Thing>();
        public HashSet<Thing> processedThings = new HashSet<Thing>();
        public Bill bill;
        public Pawn pawn;
        public Predicate<Thing> baseValidator;
        public bool billGiverIsPawn;
        public int adjacentRegionsAvailable;
        public IntVec3 rootCell;
        public List<ThingCount> chosen;
        public int regionsProcessed = 0;
        public bool foundAll = false;

        public WorkGiver_DoBill_RegionProcessor()
        {
        }

        public bool Get_RegionProcessor(Region r)
        {
            List<Thing> thingList = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
            for (int index = 0; index < thingList.Count; ++index)
            {
                Thing thing = thingList[index];
                if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(
                    thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing) && !(thing.def.IsMedicine & billGiverIsPawn))
                {
                    newRelevantThings.Add(thing);
                    processedThings.Add(thing);
                }
            }
            ++regionsProcessed;
            if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
            {
                relevantThings.AddRange(newRelevantThings);
                newRelevantThings.Clear();
                if (WorkGiver_DoBill_Patch.TryFindBestBillIngredientsInSet2(relevantThings, bill, chosen, rootCell, billGiverIsPawn, ingredientsOrdered))
                {
                    foundAll = true;
                    return true;
                }
            }
            return false;
        }
    }
}
