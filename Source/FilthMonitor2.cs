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

    public class FilthMonitor2
    {
        private static int lastUpdate;

        private static int filthAccumulated;

        private static int filthDropped;

        private static int filthAnimalGenerated;

        private static int filthHumanGenerated;

        private static int filthSpawned;

        private const int SampleDuration = 2500;

        public static void FilthMonitorTick()
        {
            if (DebugViewSettings.logFilthSummary && lastUpdate + 2500 <= Find.TickManager.TicksAbs)
            {
                int num = PawnsFinder.AllMaps_Spawned.Count((Pawn pawn) => pawn.Faction == Faction.OfPlayer);
                int num2 = PawnsFinder.AllMaps_Spawned.Count((Pawn pawn) => pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike);
                int num3 = PawnsFinder.AllMaps_Spawned.Count((Pawn pawn) => pawn.Faction == Faction.OfPlayer && !pawn.RaceProps.Humanlike);
                Log.Message($"Filth data, per day:\n  {(float)filthSpawned / (float)num / 2500f * 60000f} filth spawned per pawn\n  {(float)filthHumanGenerated / (float)num2 / 2500f * 60000f} filth human-generated per human\n  {(float)filthAnimalGenerated / (float)num3 / 2500f * 60000f} filth animal-generated per animal\n  {(float)filthAccumulated / (float)num / 2500f * 60000f} filth accumulated per pawn\n  {(float)filthDropped / (float)num / 2500f * 60000f} filth dropped per pawn");
                filthSpawned = 0;
                filthAnimalGenerated = 0;
                filthHumanGenerated = 0;
                filthAccumulated = 0;
                filthDropped = 0;
                lastUpdate = Find.TickManager.TicksAbs;
            }
        }

        public static void Notify_FilthAccumulated()
        {
            if (DebugViewSettings.logFilthSummary)
            {
                filthAccumulated++;
            }
        }

        public static void Notify_FilthDropped()
        {
            if (DebugViewSettings.logFilthSummary)
            {
                filthDropped++;
            }
        }

        public static void Notify_FilthAnimalGenerated()
        {
            if (DebugViewSettings.logFilthSummary)
            {
                filthAnimalGenerated++;
            }
        }

        public static void Notify_FilthHumanGenerated()
        {
            if (DebugViewSettings.logFilthSummary)
            {
                filthHumanGenerated++;
            }
        }

        public static void Notify_FilthSpawned()
        {
            if (DebugViewSettings.logFilthSummary)
            {
                filthSpawned++;
            }
        }
    }
}
