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

    public class SelfDefenseUtility_Patch
    {

        public static bool ShouldStartFleeing(ref bool __result, Pawn pawn)
        {
            List<Thing> thingList1 = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.AlwaysFlee);
            lock (thingList1)
            {
                for (int index = 0; index < thingList1.Count; ++index)
                {
                    Thing t = thingList1[index];
                    if (null != t)
                    {
                        if (SelfDefenseUtility.ShouldFleeFrom(t, pawn, true, false))
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            bool foundThreat = false;
            Region region = pawn.GetRegion(RegionType.Set_Passable);
            if (region == null)
            {
                __result = false;
                return false;
            }
            RegionTraverser.BreadthFirstTraverse(region, (RegionEntryPredicate)((from, reg) => reg.door == null || reg.door.Open), (RegionProcessor)(reg =>
            {
                List<Thing> thingList2 = reg.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
                for (int index = 0; index < thingList2.Count; ++index)
                {
                    Thing t;
                    try
                    {
                        t = thingList2[index];
                    }
                    catch (ArgumentOutOfRangeException) { break; }
                    if (null != t)
                    {
                        if (SelfDefenseUtility.ShouldFleeFrom(t, pawn, true, true))
                        {
                            foundThreat = true;
                            break;
                        }
                    }
                }

                return foundThreat;
            }), 9, RegionType.Set_Passable);
            __result = foundThreat;
            return false;
        }


    }
}