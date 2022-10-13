using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimThreaded.RW_Patches
{
    class ThinkNode_JoinVoluntarilyJoinableLord_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(ThinkNode_JoinVoluntarilyJoinableLord);
            Type patched = typeof(ThinkNode_JoinVoluntarilyJoinableLord_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(JoinVoluntarilyJoinableLord));
        }

        public static bool JoinVoluntarilyJoinableLord(ThinkNode_JoinVoluntarilyJoinableLord __instance, Pawn pawn)
        {
            Lord lord1 = pawn.GetLord();
            Lord lord2 = null;
            float num1 = 0.0f;
            if (lord1 != null)
            {
                if (!(lord1.LordJob is LordJob_VoluntarilyJoinable lordJob2))
                    return false;
                lord2 = lord1;
                num1 = lordJob2.VoluntaryJoinPriorityFor(pawn);
            }
            Map map = pawn.Map; //changed
            if (map != null) //changed
            {
                List<Lord> lords = map.lordManager.lords;//changed
                for (int index = 0; index < lords.Count; ++index)
                {
                    if (lords[index].LordJob is LordJob_VoluntarilyJoinable lordJob4 && lords[index].CurLordToil.VoluntaryJoinDutyHookFor(pawn) == __instance.dutyHook)
                    {
                        float num2 = lordJob4.VoluntaryJoinPriorityFor(pawn);
                        if ((double)num2 > 0.0 && (lord2 == null || (double)num2 > (double)num1))
                        {
                            lord2 = lords[index];
                            num1 = num2;
                        }
                    }
                }
            }
            if (lord2 == null || lord1 == lord2)
                return false;
            lord1?.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
            lord2.AddPawn(pawn);
            return false;
        }
    }
}
