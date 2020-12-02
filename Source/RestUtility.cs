using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class RestUtility_Patch
    {
        public static bool GetBedSleepingSlotPosFor(ref IntVec3 __result, Pawn pawn, Building_Bed bed)
        {
            for (int i = 0; i < bed.OwnersForReading.Count; i++)
            {
                if (bed.OwnersForReading[i] == pawn)
                {
                    __result = bed.GetSleepingSlotPos(i);
                    return false;
                }
            }

            for (int j = 0; j < bed.SleepingSlotsCount; j++)
            {
                Pawn curOccupant = bed.GetCurOccupant(j);
                if ((j >= bed.OwnersForReading.Count || bed.OwnersForReading[j] == null) && curOccupant == pawn)
                {
                    __result = bed.GetSleepingSlotPos(j);
                    return false;
                }
            }

            for (int k = 0; k < bed.SleepingSlotsCount; k++)
            {
                Pawn curOccupant2 = bed.GetCurOccupant(k);
                if ((k >= bed.OwnersForReading.Count || bed.OwnersForReading[k] == null) && curOccupant2 == null)
                {
                    __result = bed.GetSleepingSlotPos(k);
                    return false;
                }
            }

            Log.Warning(string.Concat("Could not find good sleeping slot position for ", pawn, ". Perhaps AnyUnoccupiedSleepingSlot check is missing somewhere."));
            __result = bed.GetSleepingSlotPos(0);
            return false;
        }

    }
}