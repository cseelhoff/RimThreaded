using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class AttackTargetsCache_Patch
    {
		[ThreadStatic] public static List<IAttackTarget> tmpTargets;
		[ThreadStatic] public static List<IAttackTarget> tmpToUpdate;

		public static FieldRef<AttackTargetsCache, Dictionary<Faction, HashSet<IAttackTarget>>> targetsHostileToFaction =
            FieldRefAccess<AttackTargetsCache, Dictionary<Faction, HashSet<IAttackTarget>>>("targetsHostileToFaction");
		public static FieldRef<AttackTargetsCache, HashSet<Pawn>> pawnsInAggroMentalState =
            FieldRefAccess<AttackTargetsCache, HashSet<Pawn>>("pawnsInAggroMentalState");
		public static FieldRef<AttackTargetsCache, HashSet<Pawn>> factionlessHumanlikes =
            FieldRefAccess<AttackTargetsCache, HashSet<Pawn>>("factionlessHumanlikes");
		public static FieldRef<AttackTargetsCache, Map> map =
            FieldRefAccess<AttackTargetsCache, Map>("map");
		public static FieldRef<AttackTargetsCache, HashSet<IAttackTarget>> allTargets =
            FieldRefAccess<AttackTargetsCache, HashSet<IAttackTarget>>("allTargets");

		private static readonly List<IAttackTarget> emptyList = new List<IAttackTarget>();
		private static readonly HashSet<IAttackTarget> emptySet = new HashSet<IAttackTarget>();

		private static readonly Dictionary<AttackTargetsCache, Dictionary<Faction, List<IAttackTarget>>> targetsHostileToFactionDict =
			new Dictionary<AttackTargetsCache, Dictionary<Faction, List<IAttackTarget>>>();
		private static readonly Dictionary<AttackTargetsCache, List<Pawn>> pawnsInAggroMentalStateDict =
			new Dictionary<AttackTargetsCache, List<Pawn>>();
		private static readonly Dictionary<AttackTargetsCache, List<Pawn>> factionlessHumanlikesDict =
			new Dictionary<AttackTargetsCache, List<Pawn>>();
		private static readonly Dictionary<AttackTargetsCache, List<IAttackTarget>> allTargetsListDict =
			new Dictionary<AttackTargetsCache, List<IAttackTarget>>();

		public static void InitializeThreadStatics()
        {
			tmpTargets = new List<IAttackTarget>();
			tmpToUpdate = new List<IAttackTarget>();
		}

		public static void RunDestructivesPatches()
		{
			Type original = typeof(AttackTargetsCache);
			Type patched = typeof(AttackTargetsCache_Patch);
			RimThreadedHarmony.Prefix(original, patched, "GetPotentialTargetsFor");
			RimThreadedHarmony.Prefix(original, patched, "RegisterTarget");
			RimThreadedHarmony.Prefix(original, patched, "DeregisterTarget");
			RimThreadedHarmony.Prefix(original, patched, "TargetsHostileToFaction");
			RimThreadedHarmony.Prefix(original, patched, "UpdateTarget");
		}

		public static bool DeregisterTarget(AttackTargetsCache __instance, IAttackTarget target)
		{
			if (allTargetsListDict.TryGetValue(__instance, out List<IAttackTarget> snapshotAllTargets))
			{
				if (!snapshotAllTargets.Contains(target))
				{
					Log.Warning("Tried to deregister " + target + " but it's not in " + __instance.GetType());
					return false;
				}
				lock (allTargetsListDict)
				{
					List<IAttackTarget> newAllTargets;
					newAllTargets = new List<IAttackTarget>(snapshotAllTargets);
					newAllTargets.Remove(target);
					allTargetsListDict[__instance] = newAllTargets;
				}

				Dictionary<Faction, List<IAttackTarget>> targetsHostileToFaction = getTargetsHostileToFactionList(__instance);
				List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
				for (int i = 0; i < allFactionsListForReading.Count; i++)
				{
					Faction faction = allFactionsListForReading[i];
					if (targetsHostileToFaction.TryGetValue(faction, out List<IAttackTarget> hostileTargets))
					{
						if (hostileTargets.Contains(target))
						{
							lock (targetsHostileToFaction)
							{
								targetsHostileToFaction.TryGetValue(faction, out List<IAttackTarget> hostileTargets2);
								if (hostileTargets2.Contains(target))
								{
									List<IAttackTarget> newHostileTargets = new List<IAttackTarget>(hostileTargets2);
									newHostileTargets.Remove(target);
									targetsHostileToFaction[faction] = newHostileTargets;
								}
							}
						}
					}
				}

				if (target is Pawn pawn)
                {
					lock (pawnsInAggroMentalStateDict)
					{
						if (pawnsInAggroMentalStateDict.TryGetValue(__instance, out List<Pawn> pawnsInAggroMentalStateList))
						{
							if (pawnsInAggroMentalStateList.Contains(pawn))
							{
								List<Pawn> newPawnsInAggroMentalStateList = new List<Pawn>(pawnsInAggroMentalStateList);
								newPawnsInAggroMentalStateList.Remove(pawn);
								pawnsInAggroMentalStateDict[__instance] = newPawnsInAggroMentalStateList;
							}
                        }
                    }
					lock (factionlessHumanlikesDict)
					{
						if (factionlessHumanlikesDict.TryGetValue(__instance, out List<Pawn> factionlessHumanlikesList))
						{
							if (factionlessHumanlikesList.Contains(pawn))
							{
								List<Pawn> newFactionlessHumanlikesList = new List<Pawn>(factionlessHumanlikesList);
								newFactionlessHumanlikesList.Remove(pawn);
								factionlessHumanlikesDict[__instance] = newFactionlessHumanlikesList;
							}
                        }
                    }
                }
            }

			return false;
		}

        public static bool RegisterTarget(AttackTargetsCache __instance, IAttackTarget target)
		{
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

			lock (allTargetsListDict)
			{
				if (allTargetsListDict.TryGetValue(__instance, out List<IAttackTarget> snapshotAllTargets))
				{
					if (snapshotAllTargets.Contains(target))
					{
						Log.Warning("Tried to register the same target twice " + target.ToStringSafe() + " in " + __instance.GetType());
						return false;
					}
					allTargetsListDict[__instance] = new List<IAttackTarget>(snapshotAllTargets) { target };
				} else
                {
					allTargetsListDict[__instance] = new List<IAttackTarget>() { target };
				}
				
			}
			List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
            Dictionary<Faction, List<IAttackTarget>> targetsHostileToFaction = getTargetsHostileToFactionList(__instance);
			for (int i = 0; i < allFactionsListForReading.Count; i++)
			{
				Faction faction = allFactionsListForReading[i];
				if (thing.HostileTo(faction))
				{
					lock (targetsHostileToFaction)
					{
						if(targetsHostileToFaction.TryGetValue(faction, out List<IAttackTarget> hostileTargets))
						{
							targetsHostileToFaction[faction] = new List<IAttackTarget>(hostileTargets) { target };
						} else
                        {
							targetsHostileToFaction[faction] = new List<IAttackTarget>() { target };
						}
					}
				}
			}

            if (target is Pawn pawn)
			{
				if (pawn.InAggroMentalState)
				{
					lock (pawnsInAggroMentalStateDict)
					{
						if(pawnsInAggroMentalStateDict.TryGetValue(__instance, out List<Pawn> pawnsInAggroMentalStateList))
                        {
							pawnsInAggroMentalStateDict[__instance] = new List<Pawn>(pawnsInAggroMentalStateList) { pawn };
						} else
                        {
							pawnsInAggroMentalStateDict[__instance] = new List<Pawn>() { pawn };
						}
					}
				}
				if (pawn.Faction == null && pawn.RaceProps.Humanlike)
				{
					lock (factionlessHumanlikesDict)
					{
						if (factionlessHumanlikesDict.TryGetValue(__instance, out List<Pawn> factionlessHumanlikesList))
						{
							factionlessHumanlikesDict[__instance] = new List<Pawn>(factionlessHumanlikesList) { pawn };
						}
						else
						{
							factionlessHumanlikesDict[__instance] = new List<Pawn>() { pawn };
						}
					}
				}
            }

            return false;

		}

		public static bool UpdateTarget(AttackTargetsCache __instance, IAttackTarget t)
		{
			if (getAllTargets(__instance).Contains(t))
			{
				DeregisterTarget(__instance, t);
				Thing thing = t.Thing;
				if (thing.Spawned && thing.Map == map(__instance))
				{
					RegisterTarget(__instance, t);
				}
			}
			return false;
		}

		public static bool GetPotentialTargetsFor(AttackTargetsCache __instance, ref List<IAttackTarget> __result, IAttackTargetSearcher th)
		{
			Thing thing = th.Thing;
			List<IAttackTarget> targets = new List<IAttackTarget>();
			Faction faction = thing.Faction;
			if (faction != null)
			{
				List<IAttackTarget> snapshotTargetsHostileToFactionList = getTargetsHostileToFactionList(__instance, faction);
				foreach (IAttackTarget item in snapshotTargetsHostileToFactionList)
				{
					if (thing.HostileTo(item.Thing))
					{
						targets.Add(item);
					}
				}
			}
			if (pawnsInAggroMentalStateDict.TryGetValue(__instance, out List<Pawn> listPawnsInAggroMentalState))
			{
				foreach (Pawn pawn in listPawnsInAggroMentalState)
				{
					if (thing.HostileTo(pawn))
					{
						targets.Add(pawn);
					}
				}
			}

			if (factionlessHumanlikesDict.TryGetValue(__instance, out List<Pawn> listFactionlessHumanlikes)) { 
				foreach (Pawn pawn2 in listFactionlessHumanlikes)
				{
					if (thing.HostileTo(pawn2))
					{
						targets.Add(pawn2);
					}
				}
			}

            if (th is Pawn pawn3 && PrisonBreakUtility.IsPrisonBreaking(pawn3))
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

		public static bool TargetsHostileToFaction(AttackTargetsCache __instance, ref HashSet<IAttackTarget> __result, Faction f)
		{
			if (f == null)
			{
				Log.Warning("Called TargetsHostileToFaction with null faction.");
				__result = emptySet;
				return false;
			}

			__result = new HashSet<IAttackTarget>(getTargetsHostileToFactionList(__instance, f));
			return false;
		}

		private static List<IAttackTarget> getTargetsHostileToFactionList(AttackTargetsCache __instance, Faction faction)
		{
			if (faction == null)
			{
				Log.Warning("Called getTargetsHostileToFactionList with null faction.");
			}
			else
			{
				if (getTargetsHostileToFactionList(__instance).TryGetValue(faction, out List<IAttackTarget> listIAttackTargets))
				{
					return listIAttackTargets;
				}
			}
			return emptyList;
		}
		private static Dictionary<Faction, List<IAttackTarget>> getTargetsHostileToFactionList(AttackTargetsCache __instance)
		{
			if (!targetsHostileToFactionDict.TryGetValue(__instance, out Dictionary<Faction, List<IAttackTarget>> factionIAttackTargetDict))
			{
				lock (targetsHostileToFactionDict)
				{
					if (!targetsHostileToFactionDict.TryGetValue(__instance, out Dictionary<Faction, List<IAttackTarget>> factionIAttackTargetDict2))
					{
						factionIAttackTargetDict = new Dictionary<Faction, List<IAttackTarget>>();
						targetsHostileToFactionDict[__instance] = factionIAttackTargetDict;
					}
					else
					{
						factionIAttackTargetDict = factionIAttackTargetDict2;
					}
				}
			}			
			return factionIAttackTargetDict;
		}

		private static List<IAttackTarget> getAllTargets(AttackTargetsCache __instance)
		{
			if(allTargetsListDict.TryGetValue(__instance, out List<IAttackTarget> allTargetsList))
            {
				return allTargetsList;
			}
			return emptyList;
		}

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(AttackTargetsCache);
			Type patched = typeof(AttackTargetsCache_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "Notify_FactionHostilityChanged");
			RimThreadedHarmony.TranspileFieldReplacements(original, "Debug_AssertHostile");
		}
    }
}
