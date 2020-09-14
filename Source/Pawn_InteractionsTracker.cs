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

    public class Pawn_InteractionsTracker_Patch
	{
		public static AccessTools.FieldRef<Pawn_InteractionsTracker, Pawn> pawn =
			AccessTools.FieldRefAccess<Pawn_InteractionsTracker, Pawn>("pawn");
		public static bool TryInteractRandomly(Pawn_InteractionsTracker __instance, ref bool __result)
		{
			if (__instance.InteractedTooRecentlyToInteract())
			{
				__result = false;
				return false;
			}
			if (!InteractionUtility.CanInitiateRandomInteraction(pawn(__instance)))
			{
				__result = false;
				return false;
			}
			List<Pawn> collection = pawn(__instance).Map.mapPawns.SpawnedPawnsInFaction(pawn(__instance).Faction);
			//Pawn_InteractionsTracker.workingList.Clear();
			List<Pawn> workingList = new List<Pawn>();
			workingList.AddRange(collection);
			workingList.Shuffle<Pawn>();
			List<InteractionDef> allDefsListForReading = DefDatabase<InteractionDef>.AllDefsListForReading;
			for (int i = 0; i < workingList.Count; i++)
			{
				Pawn p = workingList[i];
				InteractionDef intDef;
				if (p != pawn(__instance) && __instance.CanInteractNowWith(p, null) && InteractionUtility.CanReceiveRandomInteraction(p) && !pawn(__instance).HostileTo(p) && allDefsListForReading.TryRandomElementByWeight(delegate (InteractionDef x)
				{
					if (!__instance.CanInteractNowWith(p, x))
					{
						return 0f;
					}
					return x.Worker.RandomSelectionWeight(pawn(__instance), p);
				}, out intDef))
				{
					if (__instance.TryInteractWith(p, intDef))
					{
						workingList.Clear();
						__result = true;
						return false;
					}
					Log.Error(pawn(__instance) + " failed to interact with " + p, false);
				}
			}
			//Pawn_InteractionsTracker.workingList.Clear();
			__result = false;
			return false;
		}

	}
}
