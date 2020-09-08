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

    public class AttackTargetReservationManager_Patch
	{
        public static AccessTools.FieldRef<AttackTargetReservationManager, List<AttackTargetReservationManager.AttackTargetReservation>> reservations =
            AccessTools.FieldRefAccess<AttackTargetReservationManager, List<AttackTargetReservationManager.AttackTargetReservation>>("reservations");

		public static bool FirstReservationFor(AttackTargetReservationManager __instance, ref IAttackTarget __result, Pawn claimant)
		{
			lock (reservations(__instance))
			{
				for (int i = reservations(__instance).Count - 1; i >= 0; i--)
				{
					if (null != reservations(__instance)[i])
					{
						if (reservations(__instance)[i].claimant == claimant)
						{
							__result = reservations(__instance)[i].target;
							return false;
						}
					}
				}
			}
			__result = null;
			return false;
		}

	}
}
