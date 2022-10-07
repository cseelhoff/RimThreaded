using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static Verse.PawnDestinationReservationManager;
using System;

namespace RimThreaded.RW_Patches
{

    public class PawnDestinationReservationManager_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(PawnDestinationReservationManager);
            Type patched = typeof(PawnDestinationReservationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetPawnDestinationSetFor");
            RimThreadedHarmony.Prefix(original, patched, "Notify_FactionRemoved");
            RimThreadedHarmony.Prefix(original, patched, "Reserve");
            RimThreadedHarmony.Prefix(original, patched, "ObsoleteAllClaimedBy");
            RimThreadedHarmony.Prefix(original, patched, "ReleaseAllObsoleteClaimedBy");
            RimThreadedHarmony.Prefix(original, patched, "ReleaseAllClaimedBy");
            RimThreadedHarmony.Prefix(original, patched, "ReleaseClaimedBy");
            //Prefix(original, patched, "FirstObsoleteReservationFor"); //needed? excessive lock? Pawn destination reservation manager failed to clean up properly;
        }

        public static bool GetPawnDestinationSetFor(PawnDestinationReservationManager __instance, ref PawnDestinationSet __result, Faction faction)
        {
            lock (__instance)
            {
                if (!__instance.reservedDestinations.TryGetValue(faction, out PawnDestinationSet value))
                {
                    value = new PawnDestinationSet();
                    __instance.reservedDestinations.Add(faction, value);
                }
                __result = value;
            }

            return false;
        }
        public static bool Reserve(PawnDestinationReservationManager __instance, Pawn p, Job job, IntVec3 loc)
        {
            if (p.Faction != null)
            {
                if (p.Drafted &&
                    p.Faction == Faction.OfPlayer &&
                    __instance.IsReserved(loc, out Pawn claimant) &&
                    claimant != p &&
                    !claimant.HostileTo(p) &&
                    claimant.Faction != p.Faction &&
                    (
                        claimant.mindState == null ||
                        claimant.mindState.mentalStateHandler == null ||
                        !claimant.mindState.mentalStateHandler.InMentalState ||
                        
                            claimant.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Aggro &&
                            claimant.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Malicious))
                {
                    claimant.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }

                __instance.ObsoleteAllClaimedBy(p);
                lock (__instance)
                {
                    __instance.GetPawnDestinationSetFor(p.Faction).list.Add(new PawnDestinationReservation
                    {
                        target = loc,
                        claimant = p,
                        job = job
                    });
                }
            }
            return false;
        }

        public static bool ReleaseAllObsoleteClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
            {
                return false;
            }

            lock (__instance)
            {
                List<PawnDestinationReservation> list = new List<PawnDestinationReservation>(__instance.GetPawnDestinationSetFor(p.Faction).list);
                int num = 0;
                while (num < list.Count)
                {
                    if (list[num].claimant == p && list[num].obsolete)
                    {
                        list[num] = list[list.Count - 1];
                        list.RemoveLast();
                    }
                    else
                    {
                        num++;
                    }
                }
                __instance.GetPawnDestinationSetFor(p.Faction).list = list;
            }
            return false;
        }

        public static bool ReleaseAllClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
            {
                return false;
            }
            lock (__instance)
            {
                List<PawnDestinationReservation> list = new List<PawnDestinationReservation>(__instance.GetPawnDestinationSetFor(p.Faction).list);
                int num = 0;
                while (num < list.Count)
                {
                    if (list[num].claimant == p)
                    {
                        list[num] = list[list.Count - 1];
                        list.RemoveLast();
                    }
                    else
                    {
                        num++;
                    }
                }
                __instance.GetPawnDestinationSetFor(p.Faction).list = list;
            }
            return false;
        }

        public static bool ReleaseClaimedBy(PawnDestinationReservationManager __instance, Pawn p, Job job)
        {
            if (p.Faction == null)
            {
                return false;
            }
            lock (__instance)
            {
                List<PawnDestinationReservation> list = new List<PawnDestinationReservation>(__instance.GetPawnDestinationSetFor(p.Faction).list);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].claimant == p && list[i].job == job)
                    {
                        list[i].job = null;
                        if (list[i].obsolete)
                        {
                            list[i] = list[list.Count - 1];
                            list.RemoveLast();
                            i--;
                        }
                    }
                }
                __instance.GetPawnDestinationSetFor(p.Faction).list = list;
            }
            return false;
        }

        public static bool Notify_FactionRemoved(PawnDestinationReservationManager __instance, Faction faction)
        {
            lock (__instance)
            {
                if (__instance.reservedDestinations.ContainsKey(faction))
                {
                    Dictionary<Faction, PawnDestinationSet> newReservedDestinations = new Dictionary<Faction, PawnDestinationSet>(__instance.reservedDestinations);
                    newReservedDestinations.Remove(faction);
                    __instance.reservedDestinations = newReservedDestinations;
                }
            }
            return false;
        }
        public static bool ObsoleteAllClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
            {
                return false;
            }

            lock (__instance)
            {
                List<PawnDestinationReservation> list = new List<PawnDestinationReservation>(__instance.GetPawnDestinationSetFor(p.Faction).list);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].claimant == p)
                    {
                        list[i].obsolete = true;
                        if (list[i].job == null)
                        {
                            list[i] = list[list.Count - 1];
                            list.RemoveLast();
                            i--;
                        }
                    }
                }
                __instance.GetPawnDestinationSetFor(p.Faction).list = list;
            }
            return false;
        }

    }

}


