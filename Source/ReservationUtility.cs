using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class ReservationUtility_Patch
    {
		public static bool CanReserve(ref bool __result, Pawn p, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			if (!p.Spawned)
			{
				__result = false;
				return false;
			}
			__result = false;
			__result = ReservationManager_Patch.CanReserve(p.Map.reservationManager, ref __result,
					p,
					target,
					maxPawns,
					stackCount,
					layer,
					ignoreOtherReservations);
			
			return false;
		}

	}
}
