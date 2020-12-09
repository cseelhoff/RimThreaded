using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class RecordWorker_TimeGettingJoy_Patch
    {
        public static bool ShouldMeasureTimeNow(RecordWorker_TimeGettingJoy __instance, ref bool __result, Pawn pawn)
        {
            __result = false;
            Job curJob;
            if (pawn != null) {
                curJob = pawn.CurJob;
                if (curJob != null)
                {
                    if (curJob.def != null)
                    {
                        __result = curJob.def.joyKind != null;
                        return false;
                    }
                }
            }
            return false;
        }
    }
}
