using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;
using static Verse.PawnDestinationReservationManager;

namespace RimThreaded
{
    [StaticConstructorOnStartup]
    public class PawnDestinationReservationManager_Patch
    {
        private static readonly Material DestinationMat = MaterialPool.MatFrom("UI/Overlays/ReservedDestination");
        private static readonly Material DestinationSelectionMat = MaterialPool.MatFrom("UI/Overlays/ReservedDestinationSelection");
        public static ConcurrentDictionary<Faction, PawnDestinationSet> reservedDestinations = 
            new ConcurrentDictionary<Faction, PawnDestinationSet>();

        public static bool MostRecentReservationFor(PawnDestinationReservationManager __instance, ref PawnDestinationReservation __result, Pawn p)
        {
            if (p.Faction == null)
            {
                __result = null;
                return false;
            }

            List<PawnDestinationReservation> list = __instance.GetPawnDestinationSetFor(p.Faction).list;
            for (int i = 0; i < list.Count; i++)
            {
                PawnDestinationReservation pawnDestinationReservation;
                try
                {
                    pawnDestinationReservation = list[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                if (pawnDestinationReservation != null && pawnDestinationReservation.claimant == p && !pawnDestinationReservation.obsolete)
                {
                    __result = pawnDestinationReservation;
                    return false;
                }
            }

            __result = null;
            return false;
        }

        public static bool FirstObsoleteReservationFor(PawnDestinationReservationManager __instance, ref IntVec3 __result, Pawn p)
        {
            if (p.Faction == null)
            {
                __result = IntVec3.Invalid;
                return false;
            }
            PawnDestinationSet pawnDestinationSet = null;
            GetPawnDestinationSetFor(__instance, ref pawnDestinationSet, p.Faction);
            List<PawnDestinationReservation> list = pawnDestinationSet.list;
            PawnDestinationReservation pawnDestinationReservation;
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    pawnDestinationReservation = list[i];
                } catch (ArgumentException) { break; }
                if (null != pawnDestinationReservation)
                {
                    if (pawnDestinationReservation.claimant == p && pawnDestinationReservation.obsolete)
                    {
                        __result = pawnDestinationReservation.target;
                        return false;
                    }
                }
            }
            __result = IntVec3.Invalid;
            return false;
        }

        public static bool CanReserve(PawnDestinationReservationManager __instance, ref bool __result, IntVec3 c, Pawn searcher, bool draftedOnly = false)
        {
            if (searcher.Faction == null)
            {
                __result = true;
                return false;
            }
            if (searcher.Faction == Faction.OfPlayer)
            {
                __result = CanReserveInt(__instance, c, searcher.Faction, searcher, draftedOnly);
                return false;
            }
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!faction.HostileTo(searcher.Faction) && !CanReserveInt(__instance, c, faction, searcher, draftedOnly))
                {
                    __result = false;
                    return false;
                }
            }
            __result = true;
            return false;
        }

        private static bool CanReserveInt(PawnDestinationReservationManager __instance, IntVec3 c, Faction faction, Pawn ignoreClaimant = null, bool draftedOnly = false)
        {
            if (faction == null)
            {
                return true;
            }
            PawnDestinationSet pawnDestinationSet = null;
            GetPawnDestinationSetFor(__instance, ref pawnDestinationSet, faction);
            List<PawnDestinationReservation> list = pawnDestinationSet.list;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (null != list[i])
                {
                    if (null != list[i].claimant)
                    {
                        if (list[i].target == c && (
                            ignoreClaimant == null ||
                            list[i].claimant != ignoreClaimant) && (!draftedOnly ||
                            list[i].claimant.Drafted))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool Notify_FactionRemoved(PawnDestinationReservationManager __instance, Faction faction)
        {
            reservedDestinations.TryRemove(faction, out _);            
            return false;
        }

        public static bool GetPawnDestinationSetFor(PawnDestinationReservationManager __instance, ref PawnDestinationSet __result, Faction faction)
        {
            PawnDestinationSet value = reservedDestinations.GetOrAdd(faction, new PawnDestinationSet());
            __result = value;
            return false;
        }
        public static PawnDestinationSet GetPawnDestinationSetFor2(Faction faction)
        {
            PawnDestinationSet value = reservedDestinations.GetOrAdd(faction, new PawnDestinationSet());
            return value;
        }
        public static bool Reserve(PawnDestinationReservationManager __instance, Pawn p, Job job, IntVec3 loc)
        {
            if (p.Faction == null)
                return false;
            Pawn claimant;
            if (p.Drafted && p.Faction == Faction.OfPlayer && (__instance.IsReserved(loc, out claimant) && claimant != p) && (!claimant.HostileTo(p) && claimant.Faction != p.Faction) && (claimant.mindState == null || claimant.mindState.mentalStateHandler == null || !claimant.mindState.mentalStateHandler.InMentalState || claimant.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Aggro && claimant.mindState.mentalStateHandler.CurStateDef.category != MentalStateCategory.Malicious))
                claimant.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
            ObsoleteAllClaimedBy2(p);
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            lock (list)
            {
                list.Add(new PawnDestinationReservation()
                {
                    target = loc,
                    claimant = p,
                    job = job
                });
            }
            
            return false;
        }
        public static void ObsoleteAllClaimedBy2(Pawn p)
        {
            if (p.Faction == null)
                return;
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            lock (list)
            {
                for (int index = 0; index < list.Count; ++index)
                {
                    if (list[index].claimant == p)
                    {
                        list[index].obsolete = true;
                        if (list[index].job == null)
                        {
                            list[index] = list[list.Count - 1];
                            list.RemoveLast();
                            --index;
                        }
                    }
                }
            }
            return;
        }
        public static bool ObsoleteAllClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
                return false;
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            lock (list)
            {
                for (int index = 0; index < list.Count; ++index)
                {
                    if (list[index].claimant == p)
                    {
                        list[index].obsolete = true;
                        if (list[index].job == null)
                        {
                            list[index] = list[list.Count - 1];
                            list.RemoveLast();
                            --index;
                        }
                    }
                }
            }
            return false;
        }

        public static bool ReleaseAllObsoleteClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
                return false;
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            int index = 0;
            lock (list)
            {
                while (index < list.Count)
                {
                    if (list[index].claimant == p && list[index].obsolete)
                    {
                        list[index] = list[list.Count - 1];
                        list.RemoveLast();
                    }
                    else
                        ++index;
                }
            }
            return false;
        }

        public static bool ReleaseAllClaimedBy(PawnDestinationReservationManager __instance, Pawn p)
        {
            if (p.Faction == null)
                return false;
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            int index = 0;
            lock (list)
            {
                while (index < list.Count)
                {
                    if (list[index].claimant == p)
                    {
                        list[index] = list[list.Count - 1];
                        list.RemoveLast();
                    }
                    else
                        ++index;
                }
            }
            return false;            
        }

        public static bool ReleaseClaimedBy(PawnDestinationReservationManager __instance, Pawn p, Job job)
        {
            if (p.Faction == null)
                return false;
            PawnDestinationSet destinationSet = GetPawnDestinationSetFor2(p.Faction);
            List<PawnDestinationReservation> list = destinationSet.list;
            lock (list)
            {
                for (int index = 0; index < list.Count; ++index)
                {
                    if (list[index].claimant == p && list[index].job == job)
                    {
                        list[index].job = null;
                        if (list[index].obsolete)
                        {
                            list[index] = list[list.Count - 1];
                            list.RemoveLast();
                            --index;
                        }
                    }
                }
            }
            return false;
        }

        public static bool DebugDrawReservations(PawnDestinationReservationManager __instance)
        {

            foreach (KeyValuePair<Faction, PawnDestinationSet> reservedDestination in reservedDestinations)
            {
                List<PawnDestinationReservation> list = reservedDestination.Value.list;
                lock (list)
                {
                    foreach (PawnDestinationReservation destinationReservation in list)
                    {
                        IntVec3 target = destinationReservation.target;
                        MaterialPropertyBlock properties = new MaterialPropertyBlock();
                        properties.SetColor("_Color", reservedDestination.Key.Color);
                        Vector3 s = new Vector3(1f, 1f, 1f);
                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(target.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), Quaternion.identity, s);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, DestinationMat, 0, Camera.main, 0, properties);
                        if (Find.Selector.IsSelected((object)destinationReservation.claimant))
                            Graphics.DrawMesh(MeshPool.plane10, matrix, DestinationSelectionMat, 0);
                    }
                }
                    
            }
            
            return false;
        }

        public static bool IsReserved(PawnDestinationReservationManager __instance, ref bool __result, IntVec3 loc, out Pawn claimant)
        {

            foreach (KeyValuePair<Faction, PawnDestinationSet> reservedDestination in reservedDestinations)
            {
                List<PawnDestinationReservation> list = reservedDestination.Value.list;
                lock (list)
                {
                    for (int index = 0; index < list.Count; ++index)
                    {
                        if (list[index].target == loc)
                        {
                            claimant = list[index].claimant;
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            
            claimant = null;
            __result = false;
            return false;
        }



    }

}


