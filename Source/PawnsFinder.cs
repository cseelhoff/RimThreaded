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

		public static List<Pawn> allMapsWorldAndTemporary_Alive_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsWorldAndTemporary_Alive_Result");

		public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result");


		public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists(ref List<Pawn> __result)
		{
			lock (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result)
			{
				allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Clear();
			}
			List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
			for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
			{
				if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsFreeColonist)
				{
					lock (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result)
					{
						allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
					}
				}
			}

			__result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result;
			return false;
		}
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
		public static bool get_AllMapsWorldAndTemporary_Alive(ref List<Pawn> __result)
		{
			allMapsWorldAndTemporary_Alive_Result.Clear();
			lock (allMapsWorldAndTemporary_Alive_Result)
			{
				allMapsWorldAndTemporary_Alive_Result.AddRange(PawnsFinder.AllMaps);
				if (Find.World != null)
				{
					allMapsWorldAndTemporary_Alive_Result.AddRange(Find.WorldPawns.AllPawnsAlive);
				}
				allMapsWorldAndTemporary_Alive_Result.AddRange(PawnsFinder.Temporary_Alive);
			}
			__result = allMapsWorldAndTemporary_Alive_Result;
			return false;
		}
	}
}
