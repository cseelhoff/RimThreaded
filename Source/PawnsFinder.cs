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


		public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result");
		public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsCaravansAndTravelingTransportPods_Alive_Result");
		public static List<Pawn> allMapsWorldAndTemporary_Alive_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsWorldAndTemporary_Alive_Result");

		public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result =
			AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(PawnsFinder), "allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result");

		public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive(ref List<Pawn> __result)
		{
			List<Pawn> allMaps = PawnsFinder.AllMaps;
			List<Pawn> allCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllCaravansAndTravelingTransportPods_Alive;
			lock (allMapsCaravansAndTravelingTransportPods_Alive_Result)
			{
				if (allCaravansAndTravelingTransportPods_Alive.Count == 0)
				{
					__result = allMaps;
					return false;
				}

				allMapsCaravansAndTravelingTransportPods_Alive_Result.Clear();
				allMapsCaravansAndTravelingTransportPods_Alive_Result.AddRange(allMaps);
				allMapsCaravansAndTravelingTransportPods_Alive_Result.AddRange(allCaravansAndTravelingTransportPods_Alive);
			}
			__result = allMapsCaravansAndTravelingTransportPods_Alive_Result;
			return false;
			
		}
		public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists(ref List<Pawn> __result)
		{
			lock (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result)
			{
				allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Clear();
			}
			List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
			for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
			{
                Pawn mapsCaravansAndTravelingTransportPods_Alive;
				try
                {
					mapsCaravansAndTravelingTransportPods_Alive = allMapsCaravansAndTravelingTransportPods_Alive[i];
				}
				catch (ArgumentOutOfRangeException) { break; }
				if (mapsCaravansAndTravelingTransportPods_Alive.IsFreeColonist)
				{
					lock (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result)
					{
						allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Add(mapsCaravansAndTravelingTransportPods_Alive);
					}
				}
			}

			__result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result;
			return false;
		}
		public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists(ref List<Pawn> __result)
		{
			lock (allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result)
			{
				allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Clear();
				List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
				for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
				{
					Pawn p = allMapsCaravansAndTravelingTransportPods_Alive[i];
					if (null != p)
					{
						if (p.IsColonist)
						{
							allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
						}
					}
				}
			}
			__result = allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result;
			return false;
		}
		public static bool get_AllMapsWorldAndTemporary_Alive(ref List<Pawn> __result)
		{
			lock (allMapsWorldAndTemporary_Alive_Result)
			{
				allMapsWorldAndTemporary_Alive_Result.Clear();
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
