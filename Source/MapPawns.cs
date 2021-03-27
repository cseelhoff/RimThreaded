using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using RimWorld.Planet;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{

    public class MapPawns_Patch
    {
        [ThreadStatic]
        static List<Pawn> allPawnsUnspawnedResult;

        [ThreadStatic]
        static List<Pawn> prisonersOfColonyResult;

        [ThreadStatic]
        static List<Pawn> freeColonistsAndPrisonersResult;

        [ThreadStatic]
        static List<Thing> tmpThings;

        [ThreadStatic]
        static List<Pawn> freeColonistsAndPrisonersSpawnedResult;

        [ThreadStatic]
        static List<Pawn> spawnedPawnsWithAnyHediffResult;

        [ThreadStatic]
        static List<Pawn> spawnedHungryPawnsResult;

        [ThreadStatic]
        static List<Pawn> spawnedDownedPawnsResult;

        [ThreadStatic]
        static List<Pawn> spawnedPawnsWhoShouldHaveSurgeryDoneNowResult;

        [ThreadStatic]
        static List<Pawn> spawnedPawnsWhoShouldHaveInventoryUnloadedResult;

        public static FieldRef<MapPawns, List<Pawn>> pawnsSpawnedFieldRef =
            FieldRefAccess<MapPawns, List<Pawn>>("pawnsSpawned");
        public static FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> pawnsInFactionResult =
            FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("pawnsInFactionResult");
        public static FieldRef<MapPawns, Map> mapFieldRef =
            FieldRefAccess<MapPawns, Map>("map");
        public static FieldRef<MapPawns, List<Pawn>> prisonersOfColonySpawned =
            FieldRefAccess<MapPawns, List<Pawn>>("prisonersOfColonySpawned");
        public static FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> pawnsInFactionSpawned =
            FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("pawnsInFactionSpawned");
        public static FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> freeHumanlikesOfFactionResult =
            FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("freeHumanlikesOfFactionResult");
        public static FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> freeHumanlikesSpawnedOfFactionResult =
            FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("freeHumanlikesSpawnedOfFactionResult");

        private static readonly MethodInfo methodDoListChangedNotifications =
            Method(typeof(MapPawns), "DoListChangedNotifications", new Type[] { });
        private static readonly Action<MapPawns> actionDoListChangedNotifications =
            (Action<MapPawns>)Delegate.CreateDelegate(typeof(Action<MapPawns>), methodDoListChangedNotifications);



        public static bool get_AllPawns(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> allPawnsUnspawned = __instance.AllPawnsUnspawned;
            if (allPawnsUnspawned.Count == 0)
            {
                __result = pawnsSpawnedFieldRef(__instance);
                return false;
            }
            List<Pawn> allPawnsResult = new List<Pawn>();
            allPawnsResult.AddRange(pawnsSpawnedFieldRef(__instance));
            allPawnsResult.AddRange(allPawnsUnspawned);
            __result = allPawnsResult;
            return false;
        }
        public static bool get_AllPawnsUnspawned(MapPawns __instance, ref List<Pawn> __result)
        {
            if (allPawnsUnspawnedResult == null)
            {
                allPawnsUnspawnedResult = new List<Pawn>();
            }
            else
            {
                allPawnsUnspawnedResult.Clear();
            }

            ThingOwnerUtility.GetAllThingsRecursively(mapFieldRef(__instance), ThingRequest.ForGroup(ThingRequestGroup.Pawn), allPawnsUnspawnedResult, allowUnreal: true, null, alsoGetSpawnedThings: false);
            for (int num = allPawnsUnspawnedResult.Count - 1; num >= 0; num--)
            {
                if (allPawnsUnspawnedResult[num].Dead)
                {
                    allPawnsUnspawnedResult.RemoveAt(num);
                }
            }

            __result = allPawnsUnspawnedResult;
            return false;
        }

        public static bool get_PrisonersOfColony(MapPawns __instance, ref List<Pawn> __result)
        {
            if(prisonersOfColonyResult == null)
            {
                prisonersOfColonyResult = new List<Pawn>();
            } else
            {
                prisonersOfColonyResult.Clear();
            }
            List<Pawn> allPawns = __instance.AllPawns;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (allPawns[i].IsPrisonerOfColony)
                {
                    prisonersOfColonyResult.Add(allPawns[i]);
                }
            }

            __result = prisonersOfColonyResult;
            return false;
        }
        public static bool get_FreeColonistsAndPrisoners(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> freeColonists = __instance.FreeColonists;
            List<Pawn> prisonersOfColony = __instance.PrisonersOfColony;
            if (prisonersOfColony.Count == 0)
            {
                __result = freeColonists;
                return false;
            }
            if (freeColonistsAndPrisonersResult == null)
            {
                freeColonistsAndPrisonersResult = new List<Pawn>();
            }
            else
            {
                freeColonistsAndPrisonersResult.Clear();
            }
            freeColonistsAndPrisonersResult.AddRange(freeColonists);
            freeColonistsAndPrisonersResult.AddRange(prisonersOfColony);
            __result = freeColonistsAndPrisonersResult;
            return false;
        }

        public static bool get_AnyPawnBlockingMapRemoval(MapPawns __instance, ref bool __result)
        {
            Faction ofPlayer = Faction.OfPlayer;
            List<Pawn> pawnsSpawned = pawnsSpawnedFieldRef(__instance);
            for (int i = 0; i < pawnsSpawned.Count; i++)
            {
                if (!pawnsSpawned[i].Downed && pawnsSpawned[i].IsColonist)
                {
                    __result = true;
                    return false;
                }

                if (pawnsSpawned[i].relations != null && pawnsSpawned[i].relations.relativeInvolvedInRescueQuest != null)
                {
                    __result = true;
                    return false;
                }

                if (pawnsSpawned[i].Faction == ofPlayer || pawnsSpawned[i].HostFaction == ofPlayer)
                {
                    Job curJob = pawnsSpawned[i].CurJob;
                    if (curJob != null && curJob.exitMapOnArrival)
                    {
                        __result = true;
                        return false;
                    }
                }

                if (CaravanExitMapUtility.FindCaravanToJoinFor(pawnsSpawned[i]) != null && !pawnsSpawned[i].Downed)
                {
                    __result = true;
                    return false;
                }
            }

            List<Thing> list = mapFieldRef(__instance).listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
            for (int j = 0; j < list.Count; j++)
            {
                if (!(list[j] is IActiveDropPod) && !(list[j] is PawnFlyer) && list[j].TryGetComp<CompTransporter>() == null)
                {
                    continue;
                }

                IThingHolder thingHolder = list[j].TryGetComp<CompTransporter>();
                IThingHolder holder = thingHolder ?? ((IThingHolder)list[j]);
                if (tmpThings == null)
                {
                    tmpThings = new List<Thing>();
                }
                else
                {
                    tmpThings.Clear();
                }
                ThingOwnerUtility.GetAllThingsRecursively(holder, tmpThings);
                for (int k = 0; k < tmpThings.Count; k++)
                {
                    Pawn pawn = tmpThings[k] as Pawn;
                    if (pawn != null && !pawn.Dead && !pawn.Downed && pawn.IsColonist)
                    {
                        tmpThings.Clear();
                        __result = true;
                        return false;
                    }
                }
            }

            //tmpThings.Clear();
            __result = false;
            return false;
            
        }

        public static bool get_FreeColonistsAndPrisonersSpawned(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> freeColonistsSpawned = __instance.FreeColonistsSpawned;
            List<Pawn> list = __instance.PrisonersOfColonySpawned;
            if (list.Count == 0)
            {
                __result = freeColonistsSpawned;
                return false;
            }
            if(freeColonistsAndPrisonersSpawnedResult == null)
            {
                freeColonistsAndPrisonersSpawnedResult = new List<Pawn>();
            } else
            {
                freeColonistsAndPrisonersSpawnedResult.Clear();
            }
            freeColonistsAndPrisonersSpawnedResult.Clear();
            freeColonistsAndPrisonersSpawnedResult.AddRange(freeColonistsSpawned);
            freeColonistsAndPrisonersSpawnedResult.AddRange(list);
            __result = freeColonistsAndPrisonersSpawnedResult;
            return false;
            
        }

        public static bool get_SpawnedPawnsWithAnyHediff(MapPawns __instance, ref List<Pawn> __result)
        {
            if (spawnedPawnsWithAnyHediffResult == null)
            {
                spawnedPawnsWithAnyHediffResult = new List<Pawn>();
            }
            else
            {
                spawnedPawnsWithAnyHediffResult.Clear();
            }
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].health.hediffSet.hediffs.Count != 0)
                {
                    spawnedPawnsWithAnyHediffResult.Add(allPawnsSpawned[i]);
                }
            }

            __result = spawnedPawnsWithAnyHediffResult;
            return false;
            
        }

        public static bool get_SpawnedHungryPawns(MapPawns __instance, ref List<Pawn> __result)
        {
            if (spawnedHungryPawnsResult == null)
            { 
                spawnedHungryPawnsResult = new List<Pawn>();
            } else
            {
                spawnedHungryPawnsResult.Clear();
            }
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (FeedPatientUtility.IsHungry(allPawnsSpawned[i]))
                {
                    spawnedHungryPawnsResult.Add(allPawnsSpawned[i]);
                }
            }

            __result = spawnedHungryPawnsResult;
            return false;
        }

        public static bool get_SpawnedDownedPawns(MapPawns __instance, ref List<Pawn> __result)
        {
            if (spawnedDownedPawnsResult == null)
            {
                spawnedDownedPawnsResult = new List<Pawn>();
            }
            else
            {
                spawnedDownedPawnsResult.Clear();
            }
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].Downed)
                {
                    spawnedDownedPawnsResult.Add(allPawnsSpawned[i]);
                }
            }

            __result = spawnedDownedPawnsResult;
            return false;
        }

        public static bool get_SpawnedPawnsWhoShouldHaveSurgeryDoneNow(MapPawns __instance, ref List<Pawn> __result)
        {
            if(spawnedPawnsWhoShouldHaveSurgeryDoneNowResult == null)
            {
                spawnedPawnsWhoShouldHaveSurgeryDoneNowResult = new List<Pawn>();
            }
            else
            {
                spawnedPawnsWhoShouldHaveSurgeryDoneNowResult.Clear();
            }
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (HealthAIUtility.ShouldHaveSurgeryDoneNow(allPawnsSpawned[i]))
                {
                    spawnedPawnsWhoShouldHaveSurgeryDoneNowResult.Add(allPawnsSpawned[i]);
                }
            }

            __result = spawnedPawnsWhoShouldHaveSurgeryDoneNowResult;
            return false;
            
        }

        public static bool get_SpawnedPawnsWhoShouldHaveInventoryUnloaded(MapPawns __instance, ref List<Pawn> __result)
        {
            if (spawnedPawnsWhoShouldHaveInventoryUnloadedResult == null)
            {
                spawnedPawnsWhoShouldHaveInventoryUnloadedResult = new List<Pawn>();
            }
            else
            {
                spawnedPawnsWhoShouldHaveInventoryUnloadedResult.Clear();
            }
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].inventory.UnloadEverything)
                {
                    spawnedPawnsWhoShouldHaveInventoryUnloadedResult.Add(allPawnsSpawned[i]);
                }
            }

            __result = spawnedPawnsWhoShouldHaveInventoryUnloadedResult;
            return false;
            
        }

        public static bool get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount(MapPawns __instance, ref int __result)
    {
        int num = 0;
        for (int i = 0; i < pawnsSpawnedFieldRef(__instance).Count; i++)
        {
            if (pawnsSpawnedFieldRef(__instance)[i].IsFreeColonist)
            {
                num++;
            }
        }

        List<Thing> list = mapFieldRef(__instance).listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
        for (int j = 0; j < list.Count; j++)
        {
            Building_CryptosleepCasket building_CryptosleepCasket = list[j] as Building_CryptosleepCasket;
            if ((building_CryptosleepCasket == null || !building_CryptosleepCasket.def.building.isPlayerEjectable) && !(list[j] is IActiveDropPod) && !(list[j] is PawnFlyer) && list[j].TryGetComp<CompTransporter>() == null)
            {
                continue;
            }

            IThingHolder thingHolder = list[j].TryGetComp<CompTransporter>();
            IThingHolder holder = thingHolder ?? ((IThingHolder)list[j]);
            if (tmpThings == null)
            {
                tmpThings = new List<Thing>();
            }
            else
            {
                tmpThings.Clear();
            }
            ThingOwnerUtility.GetAllThingsRecursively(holder, tmpThings);
            for (int k = 0; k < tmpThings.Count; k++)
            {
                if (tmpThings[k] is Pawn pawn && !pawn.Dead && pawn.IsFreeColonist)
                {
                    num++;
                }
            }
        }

        //tmpThings.Clear();
        __result = num;
        return false;
    }

        public static bool EnsureFactionsListsInit(MapPawns __instance)
        {
            List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < allFactionsListForReading.Count; i++)
            {
                if (!pawnsInFactionSpawned(__instance).ContainsKey(allFactionsListForReading[i]))
                {
                    lock (__instance) {
                        if (!pawnsInFactionSpawned(__instance).ContainsKey(allFactionsListForReading[i])) {
                            Dictionary<Faction, List<Pawn>> newPawnsInFactionSpawned =
                                new Dictionary<Faction, List<Pawn>>(pawnsInFactionSpawned(__instance))
                                {
                                    { allFactionsListForReading[i], new List<Pawn>() }
                                };
                            pawnsInFactionSpawned(__instance) = newPawnsInFactionSpawned;
                        }
                    }
                }
            }
            return false;
        }

        public static Dictionary<MapPawns, Dictionary<Faction, List<Pawn>>> freeHumanlikesSpawnedOfFactionDict = new Dictionary<MapPawns, Dictionary<Faction, List<Pawn>>>();

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
                if (allPawns[i].Faction == faction)
                {
                    value.Add(allPawns[i]);
                }
            }

            lock (__instance)
            {
                Dictionary<Faction, List<Pawn>> newPawnsInFactionResult =
                    new Dictionary<Faction, List<Pawn>>(pawnsInFactionResult(__instance))
                    {
                        [faction] = value
                    };
                pawnsInFactionResult(__instance) = newPawnsInFactionResult;                
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
                if (allPawns[i].Faction == faction && allPawns[i].HostFaction == null && allPawns[i].RaceProps.Humanlike)
                {
                    value.Add(allPawns[i]);
                }
            }

            lock (__instance)
            {
                Dictionary<Faction, List<Pawn>> newFreeHumanlikesSpawnedOfFactionResult =
                    new Dictionary<Faction, List<Pawn>>(freeHumanlikesSpawnedOfFactionResult(__instance))
                    {
                        [faction] = value
                    };
                freeHumanlikesSpawnedOfFactionResult(__instance) = newFreeHumanlikesSpawnedOfFactionResult;
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
            List<Pawn> list = __instance.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].HostFaction == null && list[i].RaceProps.Humanlike)
                {
                    value.Add(list[i]);
                }
            }

            lock (__instance)
            {
                Dictionary<Faction, List<Pawn>> newFreeHumanlikesSpawnedOfFactionResult =
                    new Dictionary<Faction, List<Pawn>>(freeHumanlikesSpawnedOfFactionResult(__instance))
                    {
                        [faction] = value
                    };
                freeHumanlikesSpawnedOfFactionResult(__instance) = newFreeHumanlikesSpawnedOfFactionResult;
            }
            __result = value;
            return false;
        }
        public static bool RegisterPawn(MapPawns __instance, Pawn p)
        {
            if (p.Dead)
            {
                Log.Warning(string.Concat("Tried to register dead pawn ", p, " in ", __instance.GetType(), "."));
            }
            else if (!p.Spawned)
            {
                Log.Warning(string.Concat("Tried to register despawned pawn ", p, " in ", __instance.GetType(), "."));
            }
            else if (p.Map != mapFieldRef(__instance))
            {
                Log.Warning(string.Concat("Tried to register pawn ", p, " but his Map is not this one."));
            }
            else
            {
                if (!p.mindState.Active)
                {
                    return false;
                }

                EnsureFactionsListsInit(__instance);
                if (!pawnsSpawnedFieldRef(__instance).Contains(p))
                {
                    lock(__instance)
                    {
                        if (!pawnsSpawnedFieldRef(__instance).Contains(p))
                        {
                            List<Pawn> newPawnsSpawned = new List<Pawn>(pawnsSpawnedFieldRef(__instance))
                            {
                                p
                            };
                            pawnsSpawnedFieldRef(__instance) = newPawnsSpawned;
                        }
                    }
                    
                }
                if (p.Faction != null && !pawnsInFactionSpawned(__instance)[p.Faction].Contains(p))
                {
                    lock (__instance)
                    {
                        if (!pawnsInFactionSpawned(__instance)[p.Faction].Contains(p))
                        {
                            List<Pawn> newPawnList = new List<Pawn>
                            {
                                p
                            };
                            if (p.Faction == Faction.OfPlayer)
                            {
                                newPawnList.InsertionSort(delegate (Pawn a, Pawn b)
                                {
                                    int num = (a.playerSettings != null) ? a.playerSettings.joinTick : 0;
                                    int value = (b.playerSettings != null) ? b.playerSettings.joinTick : 0;
                                    return num.CompareTo(value);
                                });
                            }
                            pawnsInFactionSpawned(__instance)[p.Faction] = newPawnList;
                        }
                    }
                }

                if (p.IsPrisonerOfColony && !prisonersOfColonySpawned(__instance).Contains(p))
                {
                    lock (__instance)
                    {
                        if (p.IsPrisonerOfColony && !prisonersOfColonySpawned(__instance).Contains(p))
                        {
                            List<Pawn> newPrisonersOfColonySpawned = new List<Pawn>
                            {
                                p
                            };
                            prisonersOfColonySpawned(__instance) = newPrisonersOfColonySpawned;
                        }
                    }
                }

                actionDoListChangedNotifications(__instance);
            }
            return false;
        }
        public static bool DeRegisterPawn(MapPawns __instance, Pawn p)
        {
            EnsureFactionsListsInit(__instance);
            lock (__instance)
            {
                List<Pawn> newPawnsSpawned = new List<Pawn>(pawnsSpawnedFieldRef(__instance));
                newPawnsSpawned.Remove(p);
                pawnsSpawnedFieldRef(__instance) = newPawnsSpawned;

                List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
                Dictionary<Faction, List<Pawn>> newPawnsInFactionSpawned =
                    new Dictionary<Faction, List<Pawn>>(pawnsInFactionSpawned(__instance));
                for (int i = 0; i < allFactionsListForReading.Count; i++)
                {
                    Faction key = allFactionsListForReading[i];
                    newPawnsInFactionSpawned[key].Remove(p);
                }
                pawnsInFactionSpawned(__instance) = newPawnsInFactionSpawned;
                List<Pawn> newPrisonersOfColonySpawned = new List<Pawn>(prisonersOfColonySpawned(__instance));
                newPrisonersOfColonySpawned.Remove(p);
                prisonersOfColonySpawned(__instance) = newPrisonersOfColonySpawned;
            }
            actionDoListChangedNotifications(__instance);
            
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(MapPawns);
            Type patched = typeof(MapPawns_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_AllPawns");
            RimThreadedHarmony.Prefix(original, patched, "get_AllPawnsUnspawned");
            RimThreadedHarmony.Prefix(original, patched, "get_PrisonersOfColony");
            RimThreadedHarmony.Prefix(original, patched, "get_FreeColonistsAndPrisoners");
            RimThreadedHarmony.Prefix(original, patched, "get_AnyPawnBlockingMapRemoval");
            RimThreadedHarmony.Prefix(original, patched, "get_FreeColonistsAndPrisonersSpawned");
            RimThreadedHarmony.Prefix(original, patched, "get_SpawnedPawnsWithAnyHediff");
            RimThreadedHarmony.Prefix(original, patched, "get_SpawnedHungryPawns");
            RimThreadedHarmony.Prefix(original, patched, "get_SpawnedDownedPawns");
            RimThreadedHarmony.Prefix(original, patched, "get_SpawnedPawnsWhoShouldHaveSurgeryDoneNow");
            RimThreadedHarmony.Prefix(original, patched, "get_SpawnedPawnsWhoShouldHaveInventoryUnloaded");
            RimThreadedHarmony.Prefix(original, patched, "get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount");
            RimThreadedHarmony.Prefix(original, patched, "EnsureFactionsListsInit");
            RimThreadedHarmony.Prefix(original, patched, "PawnsInFaction");
            RimThreadedHarmony.Prefix(original, patched, "FreeHumanlikesOfFaction");
            RimThreadedHarmony.Prefix(original, patched, "FreeHumanlikesSpawnedOfFaction");
            RimThreadedHarmony.Prefix(original, patched, "RegisterPawn");
            RimThreadedHarmony.Prefix(original, patched, "DeRegisterPawn");
        }
    }
}
