using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class MapPawns_Patch
    {
        [ThreadStatic] public static List<Pawn> allPawnsResult;
        [ThreadStatic] public static List<Pawn> allPawnsUnspawnedResult;
        [ThreadStatic] public static List<Pawn> prisonersOfColonyResult;
        [ThreadStatic] public static List<Pawn> freeColonistsAndPrisonersResult;
        [ThreadStatic] public static List<Thing> tmpThings;
        [ThreadStatic] public static List<Pawn> freeColonistsAndPrisonersSpawnedResult;
        [ThreadStatic] public static List<Pawn> spawnedPawnsWithAnyHediffResult;
        [ThreadStatic] public static List<Pawn> spawnedHungryPawnsResult;
        [ThreadStatic] public static List<Pawn> spawnedDownedPawnsResult;
        [ThreadStatic] public static List<Pawn> spawnedPawnsWhoShouldHaveSurgeryDoneNowResult;
        [ThreadStatic] public static List<Pawn> spawnedPawnsWhoShouldHaveInventoryUnloadedResult;
        
        public static void InitializeThreadStatics()
        {
            allPawnsResult = new List<Pawn>();
            allPawnsUnspawnedResult = new List<Pawn>();
            prisonersOfColonyResult = new List<Pawn>();
            freeColonistsAndPrisonersResult = new List<Pawn>();
            tmpThings = new List<Thing>();
            freeColonistsAndPrisonersSpawnedResult = new List<Pawn>();
            spawnedPawnsWithAnyHediffResult = new List<Pawn>();
            spawnedHungryPawnsResult = new List<Pawn>();
            spawnedDownedPawnsResult = new List<Pawn>();
            spawnedPawnsWhoShouldHaveSurgeryDoneNowResult = new List<Pawn>();
            spawnedPawnsWhoShouldHaveInventoryUnloadedResult = new List<Pawn>();
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(MapPawns);
            Type patched = typeof(MapPawns_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched, false);
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllPawns");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllPawnsUnspawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_PrisonersOfColony");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_FreeColonistsAndPrisoners");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AnyPawnBlockingMapRemoval");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_FreeColonistsAndPrisonersSpawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_SpawnedPawnsWithAnyHediff");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_SpawnedHungryPawns");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_SpawnedDownedPawns");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_SpawnedPawnsWhoShouldHaveSurgeryDoneNow");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_SpawnedPawnsWhoShouldHaveInventoryUnloaded");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount");
            RimThreadedHarmony.Prefix(original, patched, "EnsureFactionsListsInit");
            RimThreadedHarmony.Prefix(original, patched, "PawnsInFaction");
            RimThreadedHarmony.Prefix(original, patched, "FreeHumanlikesOfFaction");
            RimThreadedHarmony.Prefix(original, patched, "FreeHumanlikesSpawnedOfFaction");
            RimThreadedHarmony.Prefix(original, patched, "RegisterPawn");
            RimThreadedHarmony.Prefix(original, patched, "DeRegisterPawn");
        }



        public static bool EnsureFactionsListsInit(MapPawns __instance)
        {
            List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
            Dictionary<Faction, List<Pawn>> pawnsInFactionSpawned = __instance.pawnsInFactionSpawned;
            for (int i = 0; i < allFactionsListForReading.Count; i++)
            {
                Faction faction = allFactionsListForReading[i];
                if (pawnsInFactionSpawned.ContainsKey(faction)) continue;
                lock (__instance) {
                    if (!pawnsInFactionSpawned.ContainsKey(faction)) {
                        pawnsInFactionSpawned.Add(faction, new List<Pawn>());
                    }
                }
            }
            return false;
        }


        public static bool PawnsInFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called PawnsInFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            List<Pawn> value = new List<Pawn>(); 
            List<Pawn> allPawns = __instance.AllPawns;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn.Faction == faction)
                {
                    value.Add(pawn);
                }
            }

            lock (__instance)
            {
                __instance.pawnsInFactionResult[faction] = value;                
            }
            __result = value;
            return false;
        }

        public static bool FreeHumanlikesOfFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called FreeHumanlikesOfFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            List<Pawn> value = new List<Pawn>();
            List<Pawn> allPawns = __instance.AllPawns;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn.Faction == faction && pawn.HostFaction == null && pawn.RaceProps.Humanlike)
                {
                    value.Add(pawn);
                }
            }

            lock (__instance)
            {
                __instance.freeHumanlikesSpawnedOfFactionResult[faction] = value;
            }
            __result = value;
            return false;
        }
        public static bool FreeHumanlikesSpawnedOfFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called FreeHumanlikesSpawnedOfFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            List<Pawn> value = new List<Pawn>();
            List<Pawn> pawnList = __instance.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < pawnList.Count; i++)
            {
                Pawn pawn = pawnList[i];
                if (pawn.HostFaction == null && pawn.RaceProps.Humanlike)
                {
                    value.Add(pawn);
                }
            }

            lock (__instance)
            {
                __instance.freeHumanlikesSpawnedOfFactionResult[faction] = value;
            }
            __result = value;
            return false;
        }
        public static bool RegisterPawn(MapPawns __instance, Pawn p)
        {
            if (p.Dead)
            {
                Log.Warning(string.Concat("Tried to register dead pawn ", p, " in ", __instance.GetType(), "."));
                return false;
            }
            if (!p.Spawned)
            {
                Log.Warning(string.Concat("Tried to register despawned pawn ", p, " in ", __instance.GetType(), "."));
                return false;
            }
            if (p.Map != __instance.map)
            {
                Log.Warning(string.Concat("Tried to register pawn ", p, " but his Map is not this one."));
                return false;
            }

            if (!p.mindState.Active)
            {
                return false;
            }

            EnsureFactionsListsInit(__instance);
            List<Pawn> pawnList = __instance.pawnsSpawned;
            if (!pawnList.Contains(p))
            {
                lock(__instance)
                {
                    if (!pawnList.Contains(p))
                    {
                        pawnList.Add(p);
                    }
                }
                
            }

            Dictionary<Faction, List<Pawn>> pawnsInFactionSpawned = __instance.pawnsInFactionSpawned;
            if (p.Faction != null)
            {
                List<Pawn> pawnList2 = pawnsInFactionSpawned[p.Faction];
                if (!pawnList2.Contains(p))
                {
                    lock (__instance)
                    {
                        if (!pawnList2.Contains(p))
                        {
                            pawnList2.Add(p);
                            if (p.Faction == Faction.OfPlayer)
                            {
                                pawnList2.InsertionSort(delegate(Pawn a, Pawn b)
                                {
                                    int num = a.playerSettings?.joinTick ?? 0;
                                    int value = b.playerSettings?.joinTick ?? 0;
                                    return num.CompareTo(value);
                                });
                            }
                        }
                    }
                }
            }

            if (p.IsPrisonerOfColony)
            {
                List<Pawn> pawnList3 = __instance.prisonersOfColonySpawned;
                if (!pawnList3.Contains(p))
                {
                    lock (__instance)
                    {
                        if (!pawnList3.Contains(p))
                        {
                            pawnList3.Add(p);
                        }
                    }
                }
            }

            __instance.DoListChangedNotifications();
            
            return false;
        }
        public static bool DeRegisterPawn(MapPawns __instance, Pawn p)
        {
            EnsureFactionsListsInit(__instance);
            lock (__instance)
            {
                List<Pawn> newPawnsSpawned = new List<Pawn>(__instance.pawnsSpawned);
                newPawnsSpawned.Remove(p);
                __instance.pawnsSpawned = newPawnsSpawned;

                List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
                Dictionary<Faction, List<Pawn>> newPawnsInFactionSpawned =
                    new Dictionary<Faction, List<Pawn>>(__instance.pawnsInFactionSpawned);
                for (int i = 0; i < allFactionsListForReading.Count; i++)
                {
                    Faction key = allFactionsListForReading[i];
                    newPawnsInFactionSpawned[key].Remove(p);
                }
                __instance.pawnsInFactionSpawned = newPawnsInFactionSpawned;
                List<Pawn> newPrisonersOfColonySpawned = new List<Pawn>(__instance.prisonersOfColonySpawned);
                newPrisonersOfColonySpawned.Remove(p);
                __instance.prisonersOfColonySpawned = newPrisonersOfColonySpawned;
            }
            __instance.DoListChangedNotifications();
            
            return false;
        }

    }
}
