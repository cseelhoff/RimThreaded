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

    public class AutoUndrafter_Patch
    {
		public static AccessTools.FieldRef<AutoUndrafter, Pawn> pawn =
			AccessTools.FieldRefAccess<AutoUndrafter, Pawn>("pawn");
		public static bool AnyHostilePreventingAutoUndraft(AutoUndrafter __instance, ref bool __result)
		{
			IAttackTarget[] potentialTargetsArrayFor;
			List<IAttackTarget> potentialTargetsFor = pawn(__instance).Map.attackTargetsCache.GetPotentialTargetsFor(pawn(__instance));
			lock (potentialTargetsFor)
            {
                potentialTargetsArrayFor = potentialTargetsFor.ToArray();
            }
			for (int i = 0; i < potentialTargetsArrayFor.Length; i++)
			{
				if (GenHostility.IsActiveThreatToPlayer(potentialTargetsArrayFor[i]))
				{
					__result = true;
					return false;
				}
			}
			__result = false;
			return false;
		}

	}
}
