using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System;

namespace RimThreaded
{

    public class PawnsFinder_Patch
    {
        [ThreadStatic] private static List<Pawn> allMapsWorldAndTemporary_AliveOrDead_Result;
        [ThreadStatic] private static List<Pawn> allMapsWorldAndTemporary_Alive_Result;
        [ThreadStatic] private static List<Pawn> allMapsAndWorld_Alive_Result;
        [ThreadStatic] private static List<Pawn> allMaps_Result;
        [ThreadStatic] private static List<Pawn> allMaps_Spawned_Result;
        [ThreadStatic] private static List<Pawn> all_AliveOrDead_Result;
        [ThreadStatic] private static List<Pawn> temporary_Result;
        [ThreadStatic] private static List<Pawn> temporary_Alive_Result;
        [ThreadStatic] private static List<Pawn> temporary_Dead_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Result;
        [ThreadStatic] private static List<Pawn> allCaravansAndTravelingTransportPods_Alive_Result;
        [ThreadStatic] private static List<Pawn> allCaravansAndTravelingTransportPods_AliveOrDead_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result;
        [ThreadStatic] private static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result;
        [ThreadStatic] private static List<Pawn> allMaps_PrisonersOfColonySpawned_Result;
        [ThreadStatic] private static List<Pawn> allMaps_PrisonersOfColony_Result;
        [ThreadStatic] private static List<Pawn> allMaps_FreeColonists_Result;
        [ThreadStatic] private static List<Pawn> allMaps_FreeColonistsSpawned_Result;
        [ThreadStatic] private static List<Pawn> allMaps_FreeColonistsAndPrisonersSpawned_Result;
        [ThreadStatic] private static List<Pawn> allMaps_FreeColonistsAndPrisoners_Result;
        [ThreadStatic] private static Dictionary<Faction, List<Pawn>> allMaps_SpawnedPawnsInFaction_Result;
        [ThreadStatic] private static List<Pawn> homeMaps_FreeColonistsSpawned_Result;

        public static bool get_AllMapsWorldAndTemporary_AliveOrDead(ref List<Pawn> __result)
        {
            if (allMapsWorldAndTemporary_AliveOrDead_Result == null)
                allMapsWorldAndTemporary_AliveOrDead_Result = new List<Pawn>();
            allMapsWorldAndTemporary_AliveOrDead_Result.Clear();
            allMapsWorldAndTemporary_AliveOrDead_Result.AddRange(PawnsFinder.AllMapsWorldAndTemporary_Alive);
            if (Find.World != null)
            {
                allMapsWorldAndTemporary_AliveOrDead_Result.AddRange(Find.WorldPawns.AllPawnsDead);
            }

            allMapsWorldAndTemporary_AliveOrDead_Result.AddRange(PawnsFinder.Temporary_Dead);
            __result = allMapsWorldAndTemporary_AliveOrDead_Result;
            return false;
        }
        public static bool get_AllMapsWorldAndTemporary_Alive(ref List<Pawn> __result)
        {
            if (allMapsWorldAndTemporary_Alive_Result == null)
                allMapsWorldAndTemporary_Alive_Result = new List<Pawn>();
            allMapsWorldAndTemporary_Alive_Result.Clear();
            allMapsWorldAndTemporary_Alive_Result.AddRange(PawnsFinder.AllMaps);
            if (Find.World != null)
            {
                allMapsWorldAndTemporary_Alive_Result.AddRange(Find.WorldPawns.AllPawnsAlive);
            }

            allMapsWorldAndTemporary_Alive_Result.AddRange(PawnsFinder.Temporary_Alive);
            __result = allMapsWorldAndTemporary_Alive_Result;
            return false;
        }

        public static bool get_AllMapsAndWorld_Alive(ref List<Pawn> __result)
        {
            if (allMapsAndWorld_Alive_Result == null)
                allMapsAndWorld_Alive_Result = new List<Pawn>();
            allMapsAndWorld_Alive_Result.Clear();
            allMapsAndWorld_Alive_Result.AddRange(PawnsFinder.AllMaps);
            if (Find.World != null)
            {
                allMapsAndWorld_Alive_Result.AddRange(Find.WorldPawns.AllPawnsAlive);
            }

            __result = allMapsAndWorld_Alive_Result;
            return false;
        }

        public static bool get_AllMaps(ref List<Pawn> __result)
        {
            if (allMaps_Result == null)
                allMaps_Result = new List<Pawn>();
            allMaps_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.AllPawns;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_Result.AddRange(maps[i].mapPawns.AllPawns);
                }
            }

            __result = allMaps_Result;
            return false;
        }

        public static bool get_AllMaps_Spawned(ref List<Pawn> __result)
        {
            if (allMaps_Spawned_Result == null)
                allMaps_Spawned_Result = new List<Pawn>();
            allMaps_Spawned_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.AllPawnsSpawned;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_Spawned_Result.AddRange(maps[i].mapPawns.AllPawnsSpawned);
                }
            }

            __result = allMaps_Spawned_Result;
            return false;
        }

        public static bool get_All_AliveOrDead(ref List<Pawn> __result)
        {
            List<Pawn> allMapsWorldAndTemporary_AliveOrDead = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            List<Pawn> allCaravansAndTravelingTransportPods_AliveOrDead = PawnsFinder.AllCaravansAndTravelingTransportPods_AliveOrDead;
            if (allCaravansAndTravelingTransportPods_AliveOrDead.Count == 0)
            {
                __result = allMapsWorldAndTemporary_AliveOrDead;
                return false;
            }

            if (all_AliveOrDead_Result == null)
                all_AliveOrDead_Result = new List<Pawn>();
            all_AliveOrDead_Result.Clear();
            all_AliveOrDead_Result.AddRange(allMapsWorldAndTemporary_AliveOrDead);
            all_AliveOrDead_Result.AddRange(allCaravansAndTravelingTransportPods_AliveOrDead);
            __result = all_AliveOrDead_Result;
            return false;
        }

        public static bool get_Temporary(ref List<Pawn> __result)
        {
            if (temporary_Result == null)
                temporary_Result = new List<Pawn>();
            temporary_Result.Clear();
            List<List<Pawn>> pawnsBeingGeneratedNow = PawnGroupKindWorker.pawnsBeingGeneratedNow;
            for (int i = 0; i < pawnsBeingGeneratedNow.Count; i++)
            {
                temporary_Result.AddRange(pawnsBeingGeneratedNow[i]);
            }

            List<List<Thing>> thingsBeingGeneratedNow = ThingSetMaker.thingsBeingGeneratedNow;
            for (int j = 0; j < thingsBeingGeneratedNow.Count; j++)
            {
                List<Thing> list = thingsBeingGeneratedNow[j];
                for (int k = 0; k < list.Count; k++)
                {
                    Pawn pawn = list[k] as Pawn;
                    if (pawn != null)
                    {
                        temporary_Result.Add(pawn);
                    }
                }
            }

            if (Current.ProgramState != ProgramState.Playing && Find.GameInitData != null)
            {
                List<Pawn> startingAndOptionalPawns = Find.GameInitData.startingAndOptionalPawns;
                for (int l = 0; l < startingAndOptionalPawns.Count; l++)
                {
                    if (startingAndOptionalPawns[l] != null)
                    {
                        temporary_Result.Add(startingAndOptionalPawns[l]);
                    }
                }
            }

            if (Find.World != null)
            {
                List<Site> sites = Find.WorldObjects.Sites;
                for (int m = 0; m < sites.Count; m++)
                {
                    for (int n = 0; n < sites[m].parts.Count; n++)
                    {
                        if (sites[m].parts[n].things == null || sites[m].parts[n].things.contentsLookMode != LookMode.Deep)
                        {
                            continue;
                        }

                        ThingOwner things = sites[m].parts[n].things;
                        for (int num = 0; num < things.Count; num++)
                        {
                            Pawn pawn2 = things[num] as Pawn;
                            if (pawn2 != null)
                            {
                                temporary_Result.Add(pawn2);
                            }
                        }
                    }
                }
            }

            if (Find.World != null)
            {
                List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
                for (int num2 = 0; num2 < allWorldObjects.Count; num2++)
                {
                    DownedRefugeeComp component = allWorldObjects[num2].GetComponent<DownedRefugeeComp>();
                    if (component != null && component.pawn != null && component.pawn.Any)
                    {
                        temporary_Result.Add(component.pawn[0]);
                    }

                    PrisonerWillingToJoinComp component2 = allWorldObjects[num2].GetComponent<PrisonerWillingToJoinComp>();
                    if (component2 != null && component2.pawn != null && component2.pawn.Any)
                    {
                        temporary_Result.Add(component2.pawn[0]);
                    }
                }
            }

            __result = temporary_Result;
            return false;
        }

        public static bool get_Temporary_Alive(ref List<Pawn> __result)
        {
            if (temporary_Alive_Result == null)
                temporary_Alive_Result = new List<Pawn>();
            temporary_Alive_Result.Clear();
            List<Pawn> temporary = PawnsFinder.Temporary;
            for (int i = 0; i < temporary.Count; i++)
            {
                if (!temporary[i].Dead)
                {
                    temporary_Alive_Result.Add(temporary[i]);
                }
            }

            __result = temporary_Alive_Result;
            return false;
        }

        public static bool get_Temporary_Dead(ref List<Pawn> __result)
        {
            if (temporary_Dead_Result == null)
                temporary_Dead_Result = new List<Pawn>();
            temporary_Dead_Result.Clear();
            List<Pawn> temporary = PawnsFinder.Temporary;
            for (int i = 0; i < temporary.Count; i++)
            {
                if (temporary[i].Dead)
                {
                    temporary_Dead_Result.Add(temporary[i]);
                }
            }

            __result = temporary_Dead_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive(ref List<Pawn> __result)
        {
            List<Pawn> allMaps = PawnsFinder.AllMaps;
            List<Pawn> allCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllCaravansAndTravelingTransportPods_Alive;
            if (allCaravansAndTravelingTransportPods_Alive.Count == 0)
            {
                __result = allMaps;
                return false;
            }

            if (allMapsCaravansAndTravelingTransportPods_Alive_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Result.Clear();
            allMapsCaravansAndTravelingTransportPods_Alive_Result.AddRange(allMaps);
            allMapsCaravansAndTravelingTransportPods_Alive_Result.AddRange(allCaravansAndTravelingTransportPods_Alive);
            __result = allMapsCaravansAndTravelingTransportPods_Alive_Result;
            return false;
        }

        public static bool get_AllCaravansAndTravelingTransportPods_Alive(ref List<Pawn> __result)
        {
            if (allCaravansAndTravelingTransportPods_Alive_Result == null)
                allCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_Alive_Result.Clear();
            List<Pawn> allCaravansAndTravelingTransportPods_AliveOrDead = PawnsFinder.AllCaravansAndTravelingTransportPods_AliveOrDead;
            for (int i = 0; i < allCaravansAndTravelingTransportPods_AliveOrDead.Count; i++)
            {
                if (!allCaravansAndTravelingTransportPods_AliveOrDead[i].Dead)
                {
                    allCaravansAndTravelingTransportPods_Alive_Result.Add(allCaravansAndTravelingTransportPods_AliveOrDead[i]);
                }
            }

            __result = allCaravansAndTravelingTransportPods_Alive_Result;
            return false;
        }

        public static bool get_AllCaravansAndTravelingTransportPods_AliveOrDead(ref List<Pawn> __result)
        {
            if (allCaravansAndTravelingTransportPods_AliveOrDead_Result == null)
                allCaravansAndTravelingTransportPods_AliveOrDead_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_AliveOrDead_Result.Clear();
            if (Find.World != null)
            {
                List<Caravan> caravans = Find.WorldObjects.Caravans;
                for (int i = 0; i < caravans.Count; i++)
                {
                    allCaravansAndTravelingTransportPods_AliveOrDead_Result.AddRange(caravans[i].PawnsListForReading);
                }

                List<TravelingTransportPods> travelingTransportPods = Find.WorldObjects.TravelingTransportPods;
                for (int j = 0; j < travelingTransportPods.Count; j++)
                {
                    allCaravansAndTravelingTransportPods_AliveOrDead_Result.AddRange(travelingTransportPods[j].Pawns);
                }
            }

            __result = allCaravansAndTravelingTransportPods_AliveOrDead_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsColonist)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsFreeColonist)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsFreeColonist && !allMapsCaravansAndTravelingTransportPods_Alive[i].IsQuestLodger())
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsFreeColonist && !allMapsCaravansAndTravelingTransportPods_Alive[i].Suspended)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result.Clear();
            Faction ofPlayer = Faction.OfPlayer;
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].Faction == ofPlayer)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result.Clear();
            Faction ofPlayer = Faction.OfPlayer;
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].Faction == ofPlayer && !allMapsCaravansAndTravelingTransportPods_Alive[i].Suspended)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive.Count; i++)
            {
                if (allMapsCaravansAndTravelingTransportPods_Alive[i].IsPrisonerOfColony)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners(ref List<Pawn> __result)
        {
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony;
            if (allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony.Count == 0)
            {
                __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;
                return false;
            }

            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result.Clear();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result.AddRange(allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists);
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result.AddRange(allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony);
            __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result;
            return false;
        }

        public static bool get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep(ref List<Pawn> __result)
        {
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result.Clear();
            List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners;
            for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners.Count; i++)
            {
                if (!allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners[i].Suspended)
                {
                    allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result.Add(allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners[i]);
                }
            }

            __result = allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result;
            return false;
        }

        public static bool get_AllMaps_PrisonersOfColonySpawned(ref List<Pawn> __result)
        {
            if (allMaps_PrisonersOfColonySpawned_Result == null)
                allMaps_PrisonersOfColonySpawned_Result = new List<Pawn>();
            allMaps_PrisonersOfColonySpawned_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.PrisonersOfColonySpawned;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_PrisonersOfColonySpawned_Result.AddRange(maps[i].mapPawns.PrisonersOfColonySpawned);
                }
            }

            __result = allMaps_PrisonersOfColonySpawned_Result;
            return false;
        }

        public static bool get_AllMaps_PrisonersOfColony(ref List<Pawn> __result)
        {
            if (allMaps_PrisonersOfColony_Result == null)
                allMaps_PrisonersOfColony_Result = new List<Pawn>();
            allMaps_PrisonersOfColony_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.PrisonersOfColony;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_PrisonersOfColony_Result.AddRange(maps[i].mapPawns.PrisonersOfColony);
                }
            }

            __result = allMaps_PrisonersOfColony_Result;
            return false;
        }

        public static bool get_AllMaps_FreeColonists(ref List<Pawn> __result)
        {
            if (allMaps_FreeColonists_Result == null)
                allMaps_FreeColonists_Result = new List<Pawn>();
            allMaps_FreeColonists_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.FreeColonists;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_FreeColonists_Result.AddRange(maps[i].mapPawns.FreeColonists);
                }
            }

            __result = allMaps_FreeColonists_Result;
            return false;
        }

        public static bool get_AllMaps_FreeColonistsSpawned(ref List<Pawn> __result)
        {
            if (allMaps_FreeColonistsSpawned_Result == null)
                allMaps_FreeColonistsSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsSpawned_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.FreeColonistsSpawned;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_FreeColonistsSpawned_Result.AddRange(maps[i].mapPawns.FreeColonistsSpawned);
                }
            }

            __result = allMaps_FreeColonistsSpawned_Result;
            return false;
        }

        public static bool get_AllMaps_FreeColonistsAndPrisonersSpawned(ref List<Pawn> __result)
        {
            if (allMaps_FreeColonistsAndPrisonersSpawned_Result == null)
                allMaps_FreeColonistsAndPrisonersSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisonersSpawned_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.FreeColonistsAndPrisonersSpawned;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_FreeColonistsAndPrisonersSpawned_Result.AddRange(maps[i].mapPawns.FreeColonistsAndPrisonersSpawned);
                }
            }

            __result = allMaps_FreeColonistsAndPrisonersSpawned_Result;
            return false;
        }

        public static bool get_AllMaps_FreeColonistsAndPrisoners(ref List<Pawn> __result)
        {
            if (allMaps_FreeColonistsAndPrisoners_Result == null)
                allMaps_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisoners_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.FreeColonistsAndPrisoners;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    allMaps_FreeColonistsAndPrisoners_Result.AddRange(maps[i].mapPawns.FreeColonistsAndPrisoners);
                }
            }

            __result = allMaps_FreeColonistsAndPrisoners_Result;
            return false;
        }

        public static bool get_HomeMaps_FreeColonistsSpawned(ref List<Pawn> __result)
        {
            if (homeMaps_FreeColonistsSpawned_Result == null)
                homeMaps_FreeColonistsSpawned_Result = new List<Pawn>();
            homeMaps_FreeColonistsSpawned_Result.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    if (!maps[0].IsPlayerHome)
                    {
                        __result = homeMaps_FreeColonistsSpawned_Result;
                        return false;
                    }

                    __result = maps[0].mapPawns.FreeColonistsSpawned;
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].IsPlayerHome)
                    {
                        homeMaps_FreeColonistsSpawned_Result.AddRange(maps[i].mapPawns.FreeColonistsSpawned);
                    }
                }
            }

            __result = homeMaps_FreeColonistsSpawned_Result;
            return false;
        }

        public static bool AllMaps_SpawnedPawnsInFaction(ref List<Pawn> __result, Faction faction)
        {
            if (allMaps_SpawnedPawnsInFaction_Result == null)
                allMaps_SpawnedPawnsInFaction_Result = new Dictionary<Faction, List<Pawn>>();
            if (!allMaps_SpawnedPawnsInFaction_Result.TryGetValue(faction, out List<Pawn> value))
            {
                value = new List<Pawn>();
                allMaps_SpawnedPawnsInFaction_Result.Add(faction, value);
            }

            value.Clear();
            if (Current.ProgramState != 0)
            {
                List<Map> maps = Find.Maps;
                if (maps.Count == 1)
                {
                    __result = maps[0].mapPawns.SpawnedPawnsInFaction(faction);
                    return false;
                }

                for (int i = 0; i < maps.Count; i++)
                {
                    value.AddRange(maps[i].mapPawns.SpawnedPawnsInFaction(faction));
                }
            }

            __result = value;
            return false;
        }

        public static void Clear()
        {
            if (allMapsWorldAndTemporary_AliveOrDead_Result == null)
                allMapsWorldAndTemporary_AliveOrDead_Result = new List<Pawn>();
            allMapsWorldAndTemporary_AliveOrDead_Result.Clear();
            if (allMapsWorldAndTemporary_Alive_Result == null)
                allMapsWorldAndTemporary_Alive_Result = new List<Pawn>();
            allMapsWorldAndTemporary_Alive_Result.Clear();
            if (allMapsAndWorld_Alive_Result == null)
                allMapsAndWorld_Alive_Result = new List<Pawn>();
            allMapsAndWorld_Alive_Result.Clear();
            if (allMaps_Result == null)
                allMaps_Result = new List<Pawn>();
            allMaps_Result.Clear();
            if (allMaps_Spawned_Result == null)
                allMaps_Spawned_Result = new List<Pawn>();
            allMaps_Spawned_Result.Clear();
            if (all_AliveOrDead_Result == null)
                all_AliveOrDead_Result = new List<Pawn>();
            all_AliveOrDead_Result.Clear();
            if (temporary_Result == null)
                temporary_Result = new List<Pawn>();
            temporary_Result.Clear();
            if (temporary_Alive_Result == null)
                temporary_Alive_Result = new List<Pawn>();
            temporary_Alive_Result.Clear();
            if (temporary_Dead_Result == null)
                temporary_Dead_Result = new List<Pawn>();
            temporary_Dead_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Result.Clear();
            if (allCaravansAndTravelingTransportPods_Alive_Result == null)
                allCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_Alive_Result.Clear();
            if (allCaravansAndTravelingTransportPods_AliveOrDead_Result == null)
                allCaravansAndTravelingTransportPods_AliveOrDead_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_AliveOrDead_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result = new List<Pawn>(); 
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result = new List<Pawn>(); 
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result.Clear();
            if (allMaps_PrisonersOfColonySpawned_Result == null)
                allMaps_PrisonersOfColonySpawned_Result = new List<Pawn>();
            allMaps_PrisonersOfColonySpawned_Result.Clear();
            if (allMaps_PrisonersOfColony_Result == null)
                allMaps_PrisonersOfColony_Result = new List<Pawn>();
            allMaps_PrisonersOfColony_Result.Clear();
            if (allMaps_FreeColonists_Result == null)
                allMaps_FreeColonists_Result = new List<Pawn>();
            allMaps_FreeColonists_Result.Clear();
            if (allMaps_FreeColonistsSpawned_Result == null)
                allMaps_FreeColonistsSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsSpawned_Result.Clear();
            if (allMaps_FreeColonistsAndPrisonersSpawned_Result == null)
                allMaps_FreeColonistsAndPrisonersSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisonersSpawned_Result.Clear();
            if (allMaps_FreeColonistsAndPrisoners_Result == null)
                allMaps_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisoners_Result.Clear();
            if (allMaps_SpawnedPawnsInFaction_Result == null)
                allMaps_SpawnedPawnsInFaction_Result = new Dictionary<Faction, List<Pawn>>();
            allMaps_SpawnedPawnsInFaction_Result.Clear();
            if (homeMaps_FreeColonistsSpawned_Result == null)
                homeMaps_FreeColonistsSpawned_Result = new List<Pawn>();
            homeMaps_FreeColonistsSpawned_Result.Clear();
            if (allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result == null)
                allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result.Clear();
        }

    }
}