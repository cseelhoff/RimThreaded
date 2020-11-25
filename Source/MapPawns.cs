using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{

    public class MapPawns_Patch
    {
        public static AccessTools.FieldRef<MapPawns, List<Pawn>> pawnsSpawned =
            AccessTools.FieldRefAccess<MapPawns, List<Pawn>>("pawnsSpawned");
        public static AccessTools.FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> pawnsInFactionResult =
            AccessTools.FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("pawnsInFactionResult");
        public static AccessTools.FieldRef<MapPawns, Map> map =
            AccessTools.FieldRefAccess<MapPawns, Map>("map");
        public static AccessTools.FieldRef<MapPawns, List<Pawn>> prisonersOfColonySpawned =
            AccessTools.FieldRefAccess<MapPawns, List<Pawn>>("prisonersOfColonySpawned");
        public static AccessTools.FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> pawnsInFactionSpawned =
            AccessTools.FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("pawnsInFactionSpawned");
        public static AccessTools.FieldRef<MapPawns, Dictionary<Faction, List<Pawn>>> freeHumanlikesSpawnedOfFactionResult =
            AccessTools.FieldRefAccess<MapPawns, Dictionary<Faction, List<Pawn>>>("freeHumanlikesSpawnedOfFactionResult");

        public static bool get_AllPawnsUnspawned(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> outThings = new List<Pawn>();
            List<IThingHolder> tmpMapChildHolders = new List<IThingHolder>();
            map(__instance).GetChildHolders(tmpMapChildHolders);
            List<Thing> tmpThings = new List<Thing>();
            for (int j = 0; j < tmpMapChildHolders.Count; j++)
            {
                tmpThings.Clear();                
                ThingOwnerUtility.GetAllThingsRecursively(tmpMapChildHolders[j], tmpThings, true, null);
                for (int k = 0; k < tmpThings.Count; k++)
                {
                    Pawn t2 = tmpThings[k] as Pawn;
                    if (t2 != null && ThingRequest.ForGroup(ThingRequestGroup.Pawn).Accepts(t2) && !t2.Dead)
                    {
                        outThings.Add(t2);
                    }
                }
            }
            __result = outThings;
            return false;

        }
        public static bool get_SpawnedPawnsWithAnyHediff(MapPawns __instance, ref List<Pawn> __result)
        {
            //this.spawnedPawnsWithAnyHediffResult.Clear();
            List<Pawn> spawnedPawnsWithAnyHediffResult = new List<Pawn>();
            List<Pawn> allPawnsSpawned = __instance.AllPawnsSpawned;
            for (int index = 0; index < allPawnsSpawned.Count; ++index)
            {
                if (allPawnsSpawned[index].health.hediffSet.hediffs.Count != 0)
                    spawnedPawnsWithAnyHediffResult.Add(allPawnsSpawned[index]);
            }
            __result = spawnedPawnsWithAnyHediffResult;
            return false;
        }

        public static bool FreeHumanlikesSpawnedOfFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            List<Pawn> pawnList1;
            lock (freeHumanlikesSpawnedOfFactionResult(__instance))
            {
                if (!freeHumanlikesSpawnedOfFactionResult(__instance).TryGetValue(faction, out pawnList1))
                {
                    pawnList1 = new List<Pawn>();
                    freeHumanlikesSpawnedOfFactionResult(__instance)[faction] = pawnList1;
                }
            }
            lock (pawnList1)
            {
                pawnList1.Clear();
                List<Pawn> list = __instance.SpawnedPawnsInFaction(faction);
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn;
                    try
                    {
                        pawn = list[i];
                    }
                    catch (ArgumentOutOfRangeException) { break; }
                    if (pawn.HostFaction == null && pawn.RaceProps.Humanlike)
                    {
                        pawnList1.Add(pawn);
                    }
                }
            }
            __result = pawnList1;
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

            //if (!pawnsInFactionResult.TryGetValue(faction, out List<Pawn> value))
            //{
            //pawnsInFactionResult.Add(faction, value);                
            //}

            //value.Clear();
            List<Pawn> value = new List<Pawn>();
            List<Pawn> allPawns = __instance.AllPawns;
            Pawn pawn;
            for (int i = 0; i < allPawns.Count; i++)
            {
                try
                {
                    pawn = allPawns[i];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (pawn.Faction == faction)
                {
                    value.Add(pawn);
                }
            }

            __result = value;
            return false;
        }

        public static bool get_AllPawns(MapPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> allPawnsUnspawned = __instance.AllPawnsUnspawned;
            if (allPawnsUnspawned.Count == 0)
            {
                __result = pawnsSpawned(__instance);
                return false;
            }
            List<Pawn> allPawnsResult = new List<Pawn>();
            lock (pawnsSpawned(__instance))
            {
                allPawnsResult.AddRange((IEnumerable<Pawn>)pawnsSpawned(__instance));
            }
            allPawnsResult.AddRange((IEnumerable<Pawn>)allPawnsUnspawned);
            __result = allPawnsResult;
            return false;
        }
        public static bool LogListedPawns(MapPawns __instance)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("MapPawns:");
            stringBuilder.AppendLine("pawnsSpawned");
            //lock (pawnsSpawned(__instance))
            //{
            //foreach (Pawn pawn in pawnsSpawned(__instance))
            for (int i = 0; i < pawnsSpawned(__instance).Count; i++)
            {
                Pawn pawn;
                try
                {
                    pawn = pawnsSpawned(__instance)[i];
                } catch(ArgumentOutOfRangeException)
                {
                    break;
                }
                stringBuilder.AppendLine("    " + pawn.ToString());
            }
            //}
            stringBuilder.AppendLine("AllPawnsUnspawned");
            foreach (Pawn pawn in __instance.AllPawnsUnspawned)
                stringBuilder.AppendLine("    " + pawn.ToString());
            lock (pawnsInFactionSpawned(__instance))
            {
                foreach (KeyValuePair<Faction, List<Pawn>> keyValuePair in pawnsInFactionSpawned(__instance))
                {
                    stringBuilder.AppendLine("pawnsInFactionSpawned[" + keyValuePair.Key.ToString() + "]");
                    foreach (Pawn pawn in keyValuePair.Value)
                        stringBuilder.AppendLine("    " + pawn.ToString());
                }
            }
            stringBuilder.AppendLine("prisonersOfColonySpawned");
            //lock (prisonersOfColonySpawned(__instance))
            //{
            //foreach (Pawn pawn in prisonersOfColonySpawned(__instance))
            for (int i = 0; i < prisonersOfColonySpawned(__instance).Count; i++)
            {
                Pawn pawn;
                try
                {
                    pawn = prisonersOfColonySpawned(__instance)[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                stringBuilder.AppendLine("    " + pawn.ToString());
            }
            //}
            Log.Message(stringBuilder.ToString(), false);
            return false;
        }
        public static void RegisterPawn1(MapPawns __instance, Pawn p)
        {
            lock (pawnsSpawned(__instance))
            {
                if (!pawnsSpawned(__instance).Contains(p))
                    pawnsSpawned(__instance).Add(p);
            }
        }
        public static void RegisterPawn2(MapPawns __instance, Pawn p)
        {
            lock (pawnsInFactionSpawned(__instance))
            {
                if (p.Faction != null && !pawnsInFactionSpawned(__instance)[p.Faction].Contains(p))
                {
                    pawnsInFactionSpawned(__instance)[p.Faction].Add(p);
                    if (p.Faction == Faction.OfPlayer)
                    {
                        pawnsInFactionSpawned(__instance)[Faction.OfPlayer].InsertionSort(delegate (Pawn a, Pawn b)
                        {
                            int num = (a.playerSettings != null) ? a.playerSettings.joinTick : 0;
                            int value = (b.playerSettings != null) ? b.playerSettings.joinTick : 0;
                            return num.CompareTo(value);
                        });
                    }
                }
            }
        }
        public static void RegisterPawn3(MapPawns __instance, Pawn p)
        {
            lock (prisonersOfColonySpawned(__instance))
            {
                if (p.IsPrisonerOfColony && !prisonersOfColonySpawned(__instance).Contains(p))
                    prisonersOfColonySpawned(__instance).Add(p);
            }
        }
        public static bool RegisterPawn(MapPawns __instance, Pawn p)
        {
            if (p.Dead)
                Log.Warning("Tried to register dead pawn " + (object)p + " in " + (object)__instance.GetType() + ".", false);
            else if (!p.Spawned)
                Log.Warning("Tried to register despawned pawn " + (object)p + " in " + (object)__instance.GetType() + ".", false);
            else if (p.Map != map(__instance))
            {
                Log.Warning("Tried to register pawn " + (object)p + " but his Map is not this one.", false);
            }
            else
            {
                if (!p.mindState.Active)
                    return false;
                EnsureFactionsListsInit(__instance);
                RegisterPawn1(__instance, p);
                RegisterPawn2(__instance, p);
                RegisterPawn3(__instance, p);
                DoListChangedNotifications(__instance);
            }
            return false;
        }

        public static bool get_AnyPawnBlockingMapRemoval(MapPawns __instance, ref bool __result)
        {
            Faction ofPlayer = Faction.OfPlayer;
            lock(pawnsSpawned(__instance)) {
                for (int index = 0; index < pawnsSpawned(__instance).Count; ++index)
                {
                    if (!pawnsSpawned(__instance)[index].Downed && pawnsSpawned(__instance)[index].IsColonist || pawnsSpawned(__instance)[index].relations != null && pawnsSpawned(__instance)[index].relations.relativeInvolvedInRescueQuest != null)
                    {
                        __result = true;
                        return false;
                    }
                    if (pawnsSpawned(__instance)[index].Faction == ofPlayer || pawnsSpawned(__instance)[index].HostFaction == ofPlayer)
                    {
                        Job curJob = pawnsSpawned(__instance)[index].CurJob;
                        if (curJob != null && curJob.exitMapOnArrival)
                        {
                            __result = true;
                            return false;
                        }
                    }
                    if (CaravanExitMapUtility.FindCaravanToJoinFor(pawnsSpawned(__instance)[index]) != null && !pawnsSpawned(__instance)[index].Downed)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            List<Thing> thingList = map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
            for (int index1 = 0; index1 < thingList.Count; ++index1)
            {
                if (thingList[index1] is IActiveDropPod || thingList[index1] is PawnFlyer || thingList[index1].TryGetComp<CompTransporter>() != null)
                {
                    IThingHolder holder = (IThingHolder)thingList[index1].TryGetComp<CompTransporter>() ?? (IThingHolder)thingList[index1];
                    List<Thing> tmpThings = new List<Thing>();
                    ThingOwnerUtility.GetAllThingsRecursively(holder, tmpThings, true, (Predicate<IThingHolder>)null);
                    for (int index2 = 0; index2 < tmpThings.Count; ++index2)
                    {
                        if (tmpThings[index2] is Pawn tmpThing && !tmpThing.Dead && (!tmpThing.Downed && tmpThing.IsColonist))
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            __result = false;
            return false;
            
        }

        public static bool get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount(MapPawns __instance, ref int __result)
        {            
            int num = 0;
            lock (pawnsSpawned(__instance))
            {
                for (int index = 0; index < pawnsSpawned(__instance).Count; ++index)
                {
                    if (pawnsSpawned(__instance)[index].IsFreeColonist)
                        ++num;
                }
            }
            List<Thing> thingList = map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
            for (int index1 = 0; index1 < thingList.Count; ++index1)
            {
                if (thingList[index1] is Building_CryptosleepCasket cryptosleepCasket && cryptosleepCasket.def.building.isPlayerEjectable || (thingList[index1] is IActiveDropPod || thingList[index1] is PawnFlyer) || thingList[index1].TryGetComp<CompTransporter>() != null)
                {
                    IThingHolder holder = (IThingHolder)thingList[index1].TryGetComp<CompTransporter>() ?? (IThingHolder)thingList[index1];
                    List<Thing> tmpThings = new List<Thing>();
                    ThingOwnerUtility.GetAllThingsRecursively(holder, tmpThings, true, (Predicate<IThingHolder>)null);
                    for (int index2 = 0; index2 < tmpThings.Count; ++index2)
                    {
                        if (tmpThings[index2] is Pawn tmpThing && !tmpThing.Dead && tmpThing.IsFreeColonist)
                            ++num;
                    }
                }
            }
            __result = num;
            return false;
        }
        private static void EnsureFactionsListsInit(MapPawns __instance)
        {
            List<Faction> factionsListForReading = Find.FactionManager.AllFactionsListForReading;
            for (int index = 0; index < factionsListForReading.Count; ++index)
            {
                //lock (pawnsInFactionSpawned(__instance))
                //{
                    if (!pawnsInFactionSpawned(__instance).ContainsKey(factionsListForReading[index]))
                        pawnsInFactionSpawned(__instance).Add(factionsListForReading[index], new List<Pawn>());
                //}
            }
        }
        private static void DoListChangedNotifications(MapPawns __instance)
        {
            MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged();
            if (Find.ColonistBar == null)
                return;
            Find.ColonistBar.MarkColonistsDirty();
        }

        public static bool DeRegisterPawn(MapPawns __instance, Pawn p)
        {
            EnsureFactionsListsInit(__instance);
            lock (pawnsSpawned(__instance)) {
                pawnsSpawned(__instance).Remove(p);
            }
            List<Faction> factionsListForReading = Find.FactionManager.AllFactionsListForReading;
            for (int index = 0; index < factionsListForReading.Count; ++index)
            {
                lock (pawnsInFactionSpawned(__instance))
                {
                    pawnsInFactionSpawned(__instance)[factionsListForReading[index]].Remove(p);
                }
            }
            lock (prisonersOfColonySpawned(__instance))
            {
                prisonersOfColonySpawned(__instance).Remove(p);
            }
            DoListChangedNotifications(__instance);
            return false;
        }

    }
}
