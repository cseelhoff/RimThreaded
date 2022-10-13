using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using static Verse.AI.AttackTargetReservationManager;
//using static HarmonyLib.AccessTools;
//using HarmonyLib;

namespace RimThreaded.RW_Patches
{

    public class AttackTargetReservationManager_Patch // rebuild to not create new tmp lists and to recycle reservations.
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(AttackTargetReservationManager);
            Type patched = typeof(AttackTargetReservationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(FirstReservationFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseClaimedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllForTarget));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllClaimedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetReservationsCount));
            RimThreadedHarmony.Prefix(original, patched, nameof(Reserve));
            RimThreadedHarmony.Prefix(original, patched, nameof(IsReservedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(Release));
        }
        /*		internal static void RunNonDestructivePatches()
                {
                    Type original = typeof(AttackTargetReservationManager);
                    Type patched = typeof(AttackTargetReservationManager_Patch);
                    ConstructorInfo oCtor = Constructor(original, new Type[] { typeof(Map)});
                    MethodInfo pMethod = Method(patched, nameof(ATRMConstructor));
                    RimThreadedHarmony.harmony.Patch(oCtor, postfix: new HarmonyMethod(pMethod));
                }*/

        [ThreadStatic] static public List<AttackTargetReservation> newAttackTargetReservations;

        internal static void InitializeThreadStatics()
        {
            newAttackTargetReservations = new List<AttackTargetReservation>();
        }
        /*public static void ATRMConstructor(AttackTargetReservationManager __instance, Map map)
        {
			newAttackTargetReservations[__instance] = new List<AttackTargetReservation>();//only 1 thread will have this added big mistake maybe this can be done in another way.
		}*/
        public static bool Release(AttackTargetReservationManager __instance, Pawn claimant, Job job, IAttackTarget target)
        {
            lock (__instance)
            {
                if (target == null)
                {
                    Log.Warning(string.Concat(claimant, " tried to release reservation on null attack target."));
                    return false;
                }
                List<AttackTargetReservation> snapshotReservations = __instance.reservations;

                newAttackTargetReservations.Clear();
                for (int i = 0; i < snapshotReservations.Count; i++)
                {
                    AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                    if (attackTargetReservation.target != target || attackTargetReservation.claimant != claimant || attackTargetReservation.job != job)
                    {
                        newAttackTargetReservations.Add(attackTargetReservation);
                    }
                    else
                    {
                        SimplePool_Patch<AttackTargetReservation>.Return(attackTargetReservation);
                    }
                }
                __instance.reservations.Clear();
                __instance.reservations.AddRange(newAttackTargetReservations);
            }
            return false;
        }
        public static bool ReleaseAllForTarget(AttackTargetReservationManager __instance, IAttackTarget target)
        {
            lock (__instance)
            {
                List<AttackTargetReservation> snapshotReservations = __instance.reservations;

                newAttackTargetReservations.Clear();

                for (int i = 0; i < snapshotReservations.Count - 1; i++)
                {
                    AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                    if (attackTargetReservation.target != target)
                    {
                        newAttackTargetReservations.Add(attackTargetReservation);
                    }
                    else
                    {
                        SimplePool_Patch<AttackTargetReservation>.Return(attackTargetReservation);
                    }
                }
                __instance.reservations.Clear();
                __instance.reservations.AddRange(newAttackTargetReservations);
            }

            return false;
        }

        public static bool ReleaseAllClaimedBy(AttackTargetReservationManager __instance, Pawn claimant)
        {
            lock (__instance)
            {
                List<AttackTargetReservation> snapshotReservations = __instance.reservations;

                newAttackTargetReservations.Clear();
                for (int i = 0; i < snapshotReservations.Count - 1; i++)
                {
                    AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                    if (attackTargetReservation.claimant != claimant)
                    {
                        newAttackTargetReservations.Add(attackTargetReservation);
                    }
                    else
                    {
                        SimplePool_Patch<AttackTargetReservation>.Return(attackTargetReservation);
                    }
                }
                __instance.reservations.Clear();
                __instance.reservations.AddRange(newAttackTargetReservations);
            }
            return false;
        }


        public static bool Reserve(AttackTargetReservationManager __instance, Pawn claimant, Job job, IAttackTarget target)
        {
            if (target == null)
            {
                Log.Warning(string.Concat(claimant, " tried to reserve null attack target."));
            }
            else if (!__instance.IsReservedBy(claimant, target))
            {

                AttackTargetReservation attackTargetReservation = SimplePool_Patch<AttackTargetReservation>.Get();
                attackTargetReservation.target = target;
                attackTargetReservation.claimant = claimant;
                attackTargetReservation.job = job;

                lock (__instance)
                {

                    newAttackTargetReservations.Clear();
                    newAttackTargetReservations.AddRange(__instance.reservations);
                    newAttackTargetReservations.Add(attackTargetReservation);

                    __instance.reservations.Clear();
                    __instance.reservations.AddRange(newAttackTargetReservations);
                }
            }
            return false;
        }

        public static bool FirstReservationFor(AttackTargetReservationManager __instance, ref IAttackTarget __result, Pawn claimant)
        {
            List<AttackTargetReservation> snapshotReservations = __instance.reservations;
            for (int i = snapshotReservations.Count - 1; i >= 0; i--)
            {
                AttackTargetReservation reservation = snapshotReservations[i];
                if (reservation.claimant != claimant) continue;
                __result = reservation.target;
                return false;
            }
            __result = null;
            return false;
        }
        public static bool ReleaseClaimedBy(AttackTargetReservationManager __instance, Pawn claimant, Job job)
        {
            lock (__instance)
            {
                List<AttackTargetReservation> snapshotReservations = __instance.reservations;

                newAttackTargetReservations.Clear();
                for (int i = 0; i < snapshotReservations.Count - 1; i++)
                {
                    AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                    if (attackTargetReservation.claimant != claimant || attackTargetReservation.job != job)
                    {
                        newAttackTargetReservations.Add(attackTargetReservation);
                    }
                    else
                    {
                        SimplePool_Patch<AttackTargetReservation>.Return(attackTargetReservation);
                    }
                }
                __instance.reservations.Clear();
                __instance.reservations.AddRange(newAttackTargetReservations);
            }
            return false;
        }
        public static bool IsReservedBy(AttackTargetReservationManager __instance, ref bool __result, Pawn claimant, IAttackTarget target)
        {
            List<AttackTargetReservation> snapshotReservations = __instance.reservations;
            for (int i = 0; i < snapshotReservations.Count; i++)
            {
                AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                if (attackTargetReservation.target != target || attackTargetReservation.claimant != claimant) continue;
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }

        public static bool GetReservationsCount(AttackTargetReservationManager __instance, ref int __result, IAttackTarget target, Faction faction)
        {
            int num = 0;
            List<AttackTargetReservation> snapshotReservations = __instance.reservations;
            for (int i = 0; i < snapshotReservations.Count; i++)
            {
                AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                if (attackTargetReservation.target == target && attackTargetReservation.claimant.Faction == faction)
                {
                    num++;
                }
            }
            __result = num;
            return false;
        }
    }
}
