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

    public class PawnsFinder_Patch
    {
        public static AccessTools.FieldRef<Building_SteamGeyser, IntermittentSteamSprayer> steamSprayer =
            AccessTools.FieldRefAccess<Building_SteamGeyser, IntermittentSteamSprayer>("steamSprayer");
		public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists(ref List<Pawn> __result)
		{
			List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result = new List<Pawn>();
			//PawnsFinder.allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Clear();
			List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
			for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
			{
				Pawn p = allMapsCaravansAndTravelingTransportPods_Alive[i];
				if (null != p) {
					if (p.IsColonist)
					{
						allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
					}
				}
			}
			__result = allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result;
			return false;
		}
	}
}
