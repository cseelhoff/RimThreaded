using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Verse.PawnDestinationReservationManager;
namespace RimThreaded
{
    class PawnDestinationReservationManager_Patch
    {
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
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                if (pawnDestinationReservation != null && pawnDestinationReservation.claimant == p && !pawnDestinationReservation.obsolete)
                {
                    __result = list[i];
                    return false;
                }
            }

            __result = null;
            return false;
        }
    }
}
