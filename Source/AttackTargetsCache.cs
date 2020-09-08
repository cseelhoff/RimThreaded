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

    public class AttackTargetsCache_Patch
    {
		public static AccessTools.FieldRef<AttackTargetsCache, Dictionary<Faction, HashSet<IAttackTarget>>> targetsHostileToFaction =
			AccessTools.FieldRefAccess<AttackTargetsCache, Dictionary<Faction, HashSet<IAttackTarget>>>("targetsHostileToFaction");
		public static AccessTools.FieldRef<AttackTargetsCache, HashSet<Pawn>> pawnsInAggroMentalState =
			AccessTools.FieldRefAccess<AttackTargetsCache, HashSet<Pawn>>("pawnsInAggroMentalState");
		public static AccessTools.FieldRef<AttackTargetsCache, HashSet<Pawn>> factionlessHumanlikes =
			AccessTools.FieldRefAccess<AttackTargetsCache, HashSet<Pawn>>("factionlessHumanlikes");
		public static AccessTools.FieldRef<AttackTargetsCache, Map> map =
			AccessTools.FieldRefAccess<AttackTargetsCache, Map>("map");

		public static HashSet<IAttackTarget> emptySet = new HashSet<IAttackTarget>();

		public static HashSet<IAttackTarget> TargetsHostileToFaction2(AttackTargetsCache __instance, Faction f)
		{
			if (f == null)
			{
				Log.Warning("Called TargetsHostileToFaction with null faction.", false);
				return emptySet;
			}
			if (targetsHostileToFaction(__instance).ContainsKey(f))
			{
				return targetsHostileToFaction(__instance)[f];
			}
			return emptySet;
		}

		public static bool GetPotentialTargetsFor(AttackTargetsCache __instance, ref List<IAttackTarget> __result, IAttackTargetSearcher th)
		{
			Thing thing = th.Thing;
			List<IAttackTarget> targets = new List<IAttackTarget>();
			Faction faction = thing.Faction;
			if (faction != null)
			{
				foreach (IAttackTarget attackTarget in TargetsHostileToFaction2(__instance, faction))
				{
					if (thing.HostileTo(attackTarget.Thing))
					{
						targets.Add(attackTarget);
					}
				}
			}
			foreach (Pawn pawn in pawnsInAggroMentalState(__instance))
			{
				if (thing.HostileTo(pawn))
				{
					targets.Add(pawn);
				}
			}
			foreach (Pawn pawn2 in factionlessHumanlikes(__instance))
			{
				if (thing.HostileTo(pawn2))
				{
					targets.Add(pawn2);
				}
			}
			Pawn pawn3 = th as Pawn;
			if (pawn3 != null && PrisonBreakUtility.IsPrisonBreaking(pawn3))
			{
				Faction hostFaction = pawn3.guest.HostFaction;
				List<Pawn> list = map(__instance).mapPawns.SpawnedPawnsInFaction(hostFaction);
				for (int i = 0; i < list.Count; i++)
				{
					if (thing.HostileTo(list[i]))
					{
						targets.Add(list[i]);
					}
				}
			}
			__result = targets;
			return false;
		}
	}
}
