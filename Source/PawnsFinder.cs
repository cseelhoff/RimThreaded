using System.Collections.Generic;
using RimWorld;
using Verse;
using System;

namespace RimThreaded
{

    public class PawnsFinder_Patch
    {
        [ThreadStatic] public static List<Pawn> allMapsWorldAndTemporary_AliveOrDead_Result;
        [ThreadStatic] public static List<Pawn> allMapsWorldAndTemporary_Alive_Result;
        [ThreadStatic] public static List<Pawn> allMapsAndWorld_Alive_Result;
        [ThreadStatic] public static List<Pawn> allMaps_Result;
        [ThreadStatic] public static List<Pawn> allMaps_Spawned_Result;
        [ThreadStatic] public static List<Pawn> all_AliveOrDead_Result;
        [ThreadStatic] public static List<Pawn> temporary_Result;
        [ThreadStatic] public static List<Pawn> temporary_Alive_Result;
        [ThreadStatic] public static List<Pawn> temporary_Dead_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Result;
        [ThreadStatic] public static List<Pawn> allCaravansAndTravelingTransportPods_Alive_Result;
        [ThreadStatic] public static List<Pawn> allCaravansAndTravelingTransportPods_AliveOrDead_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result;
        [ThreadStatic] public static List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result;
        [ThreadStatic] public static List<Pawn> allMaps_PrisonersOfColonySpawned_Result;
        [ThreadStatic] public static List<Pawn> allMaps_PrisonersOfColony_Result;
        [ThreadStatic] public static List<Pawn> allMaps_FreeColonists_Result;
        [ThreadStatic] public static List<Pawn> allMaps_FreeColonistsSpawned_Result;
        [ThreadStatic] public static List<Pawn> allMaps_FreeColonistsAndPrisonersSpawned_Result;
        [ThreadStatic] public static List<Pawn> allMaps_FreeColonistsAndPrisoners_Result;
        [ThreadStatic] public static Dictionary<Faction, List<Pawn>> allMaps_SpawnedPawnsInFaction_Result;
        [ThreadStatic] public static List<Pawn> homeMaps_FreeColonistsSpawned_Result;

        public static void InitializeThreadStatics()
        {
            allMapsWorldAndTemporary_AliveOrDead_Result = new List<Pawn>();
            allMapsWorldAndTemporary_Alive_Result = new List<Pawn>();
            allMapsAndWorld_Alive_Result = new List<Pawn>();
            allMaps_Result = new List<Pawn>();
            allMaps_Spawned_Result = new List<Pawn>();
            all_AliveOrDead_Result = new List<Pawn>();
            temporary_Result = new List<Pawn>();
            temporary_Alive_Result = new List<Pawn>();
            temporary_Dead_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_Alive_Result = new List<Pawn>();
            allCaravansAndTravelingTransportPods_AliveOrDead_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_Colonists_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result = new List<Pawn>();
            allMaps_PrisonersOfColonySpawned_Result = new List<Pawn>();
            allMaps_PrisonersOfColony_Result = new List<Pawn>();
            allMaps_FreeColonists_Result = new List<Pawn>();
            allMaps_FreeColonistsSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisonersSpawned_Result = new List<Pawn>();
            allMaps_FreeColonistsAndPrisoners_Result = new List<Pawn>();
            allMaps_SpawnedPawnsInFaction_Result = new Dictionary<Faction, List<Pawn>>();
            homeMaps_FreeColonistsSpawned_Result = new List<Pawn>();
        }

    }
}