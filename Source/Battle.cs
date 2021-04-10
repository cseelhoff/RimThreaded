using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class Battle_Patch
    {

        [ThreadStatic] public static int creationTimestamp;
        [ThreadStatic] public static List<LogEntry> entries;
        [ThreadStatic] public static HashSet<Pawn> concerns;
        [ThreadStatic] public static Battle absorbedBy;
        [ThreadStatic] public static string battleName;
        [ThreadStatic] public static int loadID;

        public static void InitializeThreadStatics()
        {
        entries = new List<LogEntry>();
        concerns = new HashSet<Pawn>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Battle);
            Type patched = typeof(Battle_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "ExposeData");
            RimThreadedHarmony.TranspileFieldReplacements(original, "Absorb");
        }
    }
}
