#region Assembly Assembly-CSharp, Version=1.2.7558.21380, Culture=neutral, PublicKeyToken=null
// C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class JobDriver_Patch
    {
        public static FieldRef<JobDriver, int> curToilIndex = FieldRefAccess<JobDriver, int>("curToilIndex");
        public static FieldRef<JobDriver, int> nextToilIndex = FieldRefAccess<JobDriver, int>("nextToilIndex");
        public static FieldRef<JobDriver, bool> wantBeginNextToil = FieldRefAccess<JobDriver, bool>("wantBeginNextToil");
        public static FieldRef<JobDriver, ToilCompleteMode> curToilCompleteMode = FieldRefAccess<JobDriver, ToilCompleteMode>("curToilCompleteMode");
        public static FieldRef<JobDriver, List<Toil>> toils = FieldRefAccess<JobDriver, List<Toil>>("toils");


        private static bool get_CanStartNextToilInBusyStance2(JobDriver __instance)
        {
            int num = curToilIndex(__instance) + 1;
            if (num >= toils(__instance).Count)
            {
                return false;
            }

            return toils(__instance)[num].atomicWithPrevious;
        }
        public static Toil get_CurToil2(JobDriver __instance)
        {
            int cti = curToilIndex(__instance);
            if (cti < 0 || __instance.job == null || __instance.pawn.CurJob != __instance.job)
            {
                return null;
            }

            if (cti >= toils(__instance).Count)
            {
                Log.Warning(__instance.pawn + " with job " + __instance.pawn.CurJob + " tried to get CurToil with curToilIndex=" + curToilIndex(__instance) + " but only has " + toils(__instance).Count + " toils.");
                return null;
            }
            Toil toil;
            try
            {
                toil = toils(__instance)[cti];
            } catch (ArgumentOutOfRangeException)
            {
                Log.Warning(__instance.pawn + " with job " + __instance.pawn.CurJob + " tried to get CurToil with curToilIndex=" + curToilIndex(__instance) + " but only has " + toils(__instance).Count + " toils.");
                return null;
            }
            return toil;
        }
        public static bool CheckCurrentToilEndOrFail2(JobDriver __instance)
        {
            try
            {
                Toil curToil = get_CurToil2(__instance);
                List<Func<JobCondition>> listFuncJobConditions = __instance.globalFailConditions;
                if (listFuncJobConditions != null)
                {
                    for (int i = 0; i < listFuncJobConditions.Count; i++)
                    {
                        JobCondition jobCondition = listFuncJobConditions[i]();
                        if (jobCondition != JobCondition.Ongoing)
                        {
                            if (__instance.pawn.jobs.debugLog)
                            {
                                __instance.pawn.jobs.DebugLogEvent(__instance.GetType().Name + " ends current job " + __instance.job.ToStringSafe() + " because of globalFailConditions[" + i + "]");
                            }

                            __instance.EndJobWith(jobCondition);
                            return true;
                        }
                    }
                }

                if (curToil != null && curToil.endConditions != null)
                {
                    for (int j = 0; j < curToil.endConditions.Count; j++)
                    {
                        JobCondition jobCondition2 = curToil.endConditions[j]();
                        if (jobCondition2 != JobCondition.Ongoing)
                        {
                            if (__instance.pawn.jobs.debugLog)
                            {
                                __instance.pawn.jobs.DebugLogEvent(__instance.GetType().Name + " ends current job " + __instance.job.ToStringSafe() + " because of toils[" + curToilIndex + "].endConditions[" + j + "]");
                            }

                            __instance.EndJobWith(jobCondition2);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                JobUtility.TryStartErrorRecoverJob(__instance.pawn, "Exception in CheckCurrentToilEndOrFail for pawn " + __instance.pawn.ToStringSafe(), exception, __instance);
                return true;
            }
        }

        protected static bool get_HaveCurToil2(JobDriver __instance)
        {
            if (curToilIndex(__instance) >= 0 && curToilIndex(__instance) < toils(__instance).Count && __instance.job != null)
            {
                return __instance.pawn.CurJob == __instance.job;
            }

            return false;
        }

        public static bool TryActuallyStartNextToil(JobDriver __instance)
        {
            if (!__instance.pawn.Spawned || (__instance.pawn.stances.FullBodyBusy && !get_CanStartNextToilInBusyStance2(__instance)) || __instance.job == null || __instance.pawn.CurJob != __instance.job)
            {
                return false;
            }
            /*
            if (get_HaveCurToil2(__instance))
            {
                get_CurToil2(__instance).Cleanup(curToilIndex(__instance), __instance);
            }
            */
            if (curToilIndex(__instance) >= 0 && curToilIndex(__instance) < toils(__instance).Count && __instance.job != null)
            {
                if (__instance.pawn.CurJob == __instance.job)
                {
                    Toil curToil2 = toils(__instance)[curToilIndex(__instance)];
                    curToil2.Cleanup(curToilIndex(__instance), __instance);
                }
            }
            
            if (nextToilIndex(__instance) >= 0)
            {
                curToilIndex(__instance) = nextToilIndex(__instance);
                nextToilIndex(__instance) = -1;
            }
            else
            {
                curToilIndex(__instance)++;
            }

            wantBeginNextToil(__instance) = false;

            if (!get_HaveCurToil2(__instance))
            {
                if (__instance.pawn.stances != null && __instance.pawn.stances.curStance.StanceBusy)
                {
                    Log.ErrorOnce(__instance.pawn.ToStringSafe() + " ended job " + __instance.job.ToStringSafe() + " due to running out of toils during a busy stance.", 6453432);
                }

                __instance.EndJobWith(JobCondition.Succeeded);
                return false;
            }


            __instance.debugTicksSpentThisToil = 0;
            Toil curToil = get_CurToil2(__instance);
            if (curToil != null)
            {
                __instance.ticksLeftThisToil = curToil.defaultDuration;
                curToilCompleteMode(__instance) = curToil.defaultCompleteMode;
            }
            if (CheckCurrentToilEndOrFail2(__instance))
            {
                return false;
            }

            curToil = get_CurToil2(__instance);
            Toil gct = get_CurToil2(__instance);
            if (gct != null && gct.preInitActions != null)
            {
                List<Action> preInitActions = gct.preInitActions;
                for (int i = 0; i < preInitActions.Count; i++)
                {
                    try
                    {
                        gct = get_CurToil2(__instance);
                        if (gct != null)
                        {
                            preInitActions = gct.preInitActions;
                        }
                        else
                        {
                            break;
                        }
                        preInitActions[i]();
                    }
                    catch (Exception exception)
                    {
                        JobUtility.TryStartErrorRecoverJob(__instance.pawn, "JobDriver threw exception in preInitActions[" + i + "] for pawn " + __instance.pawn.ToStringSafe(), exception, __instance);
                        return false;
                    }

                    if (get_CurToil2(__instance) != curToil)
                    {
                        break;
                    }
                    gct = get_CurToil2(__instance);
                    if (gct != null)
                    {
                        preInitActions = gct.preInitActions;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Toil gct2 = get_CurToil2(__instance);
            if (gct2 == curToil)
            {
                if (gct2 != null)
                {
                    if (gct2.initAction != null)
                    {
                        try
                        {
                            gct2.initAction();
                        }
                        catch (Exception exception2)
                        {
                            JobUtility.TryStartErrorRecoverJob(__instance.pawn, "JobDriver threw exception in initAction for pawn " + __instance.pawn.ToStringSafe(), exception2, __instance);
                            return false;
                        }
                    }

                    if (!__instance.ended && curToilCompleteMode(__instance) == ToilCompleteMode.Instant && get_CurToil2(__instance) == curToil)
                    {
                        __instance.ReadyForNextToil();
                    }
                }
            }
            return false;
        }

        public static bool DriverTick(JobDriver __instance)
        {
            try
            {
                __instance.ticksLeftThisToil--;
                __instance.debugTicksSpentThisToil++;
                if (get_CurToil2(__instance) == null)
                {
                    if (!__instance.pawn.stances.FullBodyBusy || get_CanStartNextToilInBusyStance2(__instance))
                    {
                        __instance.ReadyForNextToil();
                    }
                }
                else
                {
                    if (CheckCurrentToilEndOrFail2(__instance))
                    {
                        return false;
                    }

                    if (curToilCompleteMode(__instance) == ToilCompleteMode.Delay)
                    {
                        if (__instance.ticksLeftThisToil > 0)
                        {
                            goto IL_0099;
                        }

                        __instance.ReadyForNextToil();
                    }
                    else
                    {
                        if (curToilCompleteMode(__instance) != ToilCompleteMode.FinishedBusy || __instance.pawn.stances.FullBodyBusy)
                        {
                            goto IL_0099;
                        }

                        __instance.ReadyForNextToil();
                    }

                    return false;
                }

                goto end_IL_0000;
            IL_01b8:
                if (__instance.job.mote != null)
                {
                    __instance.job.mote.Maintain();
                }

                goto end_IL_0000;
            IL_0099:
                if (wantBeginNextToil(__instance))
                {
                    TryActuallyStartNextToil(__instance);
                    return false;
                }

                if (curToilCompleteMode(__instance) == ToilCompleteMode.Instant && __instance.debugTicksSpentThisToil > 300)
                {
                    Log.Error(string.Concat(__instance.pawn, " had to be broken from frozen state. He was doing job ", __instance.job, ", toilindex=", curToilIndex));
                    __instance.ReadyForNextToil();
                    return false;
                }

                Job startingJob = __instance.pawn.CurJob;
                int startingJobId = startingJob.loadID;
                if (get_CurToil2(__instance) != null && get_CurToil2(__instance).preTickActions != null)
                {
                    Toil curToil = get_CurToil2(__instance);
                    for (int i = 0; i < curToil.preTickActions.Count; i++)
                    {
                        curToil.preTickActions[i]();
                        if (JobChanged() || get_CurToil2(__instance) != curToil || wantBeginNextToil(__instance))
                        {
                            return false;
                        }
                    }
                }

                if (get_CurToil2(__instance).tickAction == null)
                {
                    goto IL_01b8;
                }

                get_CurToil2(__instance).tickAction();
                if (!JobChanged())
                {
                    goto IL_01b8;
                }

            end_IL_0000:
                bool JobChanged()
                {
                    if (__instance.pawn.CurJob == startingJob)
                    {
                        return __instance.pawn.CurJob.loadID != startingJobId;
                    }

                    return true;
                }
            }
            catch (Exception exception)
            {
                JobUtility.TryStartErrorRecoverJob(__instance.pawn, "Exception in JobDriver tick for pawn " + __instance.pawn.ToStringSafe(), exception, __instance);
            }
            return false;
        }


    }
}