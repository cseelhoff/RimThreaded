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
		public static AccessTools.FieldRef<AttackTargetsCache, HashSet<IAttackTarget>> allTargets =
			AccessTools.FieldRefAccess<AttackTargetsCache, HashSet<IAttackTarget>>("allTargets");

		public static HashSet<IAttackTarget> emptySet = new HashSet<IAttackTarget>();


		public static bool DeregisterTarget(AttackTargetsCache __instance, IAttackTarget target)
		{
			if (!allTargets(__instance).Contains(target))
			{
				Log.Warning("Tried to deregister " + target + " but it's not in " + __instance.GetType());
				return false;
			}
			lock (allTargets(__instance))
			{
				allTargets(__instance).Remove(target);
			}
			foreach (KeyValuePair<Faction, HashSet<IAttackTarget>> item in targetsHostileToFaction(__instance))
			{
				lock (item.Value)
				{
					item.Value.Remove(target);
				}
			}

			Pawn pawn = target as Pawn;
			if (pawn != null)
			{
				lock (pawnsInAggroMentalState(__instance))
				{
					pawnsInAggroMentalState(__instance).Remove(pawn);
				}
				lock (factionlessHumanlikes(__instance)) {
					factionlessHumanlikes(__instance).Remove(pawn);
				}
			}
			return false;
		}

		public static bool RegisterTarget(AttackTargetsCache __instance, IAttackTarget target)
		{
			if (allTargets(__instance).Contains(target))
			{
				Log.Warning("Tried to register the same target twice " + target.ToStringSafe() + " in " + __instance.GetType());
				return false;
			}

			Thing thing = target.Thing;
			if (!thing.Spawned)
			{
				Log.Warning("Tried to register unspawned thing " + thing.ToStringSafe() + " in " + __instance.GetType());
				return false;
			}

			if (thing.Map != map(__instance))
			{
				Log.Warning("Tried to register attack target " + thing.ToStringSafe() + " but its Map is not this one.");
				return false;
			}
			lock (allTargets(__instance)) {
				allTargets(__instance).Add(target);
			}
			List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
			for (int i = 0; i < allFactionsListForReading.Count; i++)
			{
				if (thing.HostileTo(allFactionsListForReading[i]))
				{
					lock (targetsHostileToFaction(__instance))
					{
						if (!targetsHostileToFaction(__instance).ContainsKey(allFactionsListForReading[i]))
						{
							targetsHostileToFaction(__instance).Add(allFactionsListForReading[i], new HashSet<IAttackTarget>());
						}
						targetsHostileToFaction(__instance)[allFactionsListForReading[i]].Add(target);
					}
				}
			}

			Pawn pawn = target as Pawn;
			if (pawn != null)
			{
				if (pawn.InAggroMentalState)
				{
					lock (pawnsInAggroMentalState(__instance)) {
						pawnsInAggroMentalState(__instance).Add(pawn);
					}
				}

				if (pawn.Faction == null && pawn.RaceProps.Humanlike)
				{
					lock (factionlessHumanlikes(__instance))
					{
						factionlessHumanlikes(__instance).Add(pawn);
					}
				}
			}
			return false;

		}


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
