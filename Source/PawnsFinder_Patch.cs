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

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(PawnsFinder);
            RimThreadedHarmony.AddAllMatchingFields(original, typeof(PawnsFinder_Patch));
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsWorldAndTemporary_AliveOrDead");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsWorldAndTemporary_Alive");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsAndWorld_Alive");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_Spawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_All_AliveOrDead");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_Temporary");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_Temporary_Alive");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_Temporary_Dead");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllCaravansAndTravelingTransportPods_Alive");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllCaravansAndTravelingTransportPods_AliveOrDead");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_Colonists");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_PrisonersOfColony");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_PrisonersOfColonySpawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_PrisonersOfColony");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_FreeColonists");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_FreeColonistsSpawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_FreeColonistsAndPrisonersSpawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_AllMaps_FreeColonistsAndPrisoners");
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_HomeMaps_FreeColonistsSpawned");
            RimThreadedHarmony.TranspileFieldReplacements(original, "AllMaps_SpawnedPawnsInFaction");
            RimThreadedHarmony.TranspileFieldReplacements(original, "Clear");
        }
    }
}