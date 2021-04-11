using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Battle_Patch
    {
        public static FieldRef<Battle, int> loadID =
            FieldRefAccess<Battle, int>("loadID");

        public static FieldRef<Battle, int> creationTimestamp = FieldRefAccess<Battle, int>("creationTimestamp");
        public static FieldRef<Battle, List<LogEntry>> entries = FieldRefAccess<Battle, List<LogEntry>>("entries");
        public static FieldRef<Battle, Battle> absorbedBy = FieldRefAccess<Battle, Battle>("absorbedBy");
        public static FieldRef<Battle, string> battleName = FieldRefAccess<Battle, string>("battleName");
        public static FieldRef<Battle, HashSet<Pawn>> concerns = FieldRefAccess<Battle, HashSet<Pawn>>("concerns");

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Battle);
            Type patched = typeof(Battle_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ExposeData");
            RimThreadedHarmony.Prefix(original, patched, "Absorb");
        }

        public static bool Absorb(Battle __instance, Battle battle)
        {
            creationTimestamp(__instance) = Mathf.Min(creationTimestamp(__instance), creationTimestamp(battle));
            entries(__instance).AddRange(entries(battle));
            concerns(__instance).AddRange(concerns(battle));
            
            entries(__instance) = (from e in entries(__instance)
                            orderby e.Age
                            select e).ToList();
            entries(battle).Clear();
            concerns(battle).Clear();
            absorbedBy(battle) = __instance;
            battleName(__instance) = null;
            return false;
        }

        public static bool ExposeData(Battle __instance)
        {
            Scribe_Values.Look(ref loadID(__instance), "loadID", 0, false);
            Scribe_Values.Look(ref creationTimestamp(__instance), "creationTimestamp", 0, false);
            Scribe_Collections.Look(ref entries(__instance), "entries", LookMode.Deep, (new object[] { }));
            Scribe_References.Look(ref absorbedBy(__instance), "absorbedBy", false);
            Scribe_Values.Look(ref battleName(__instance), "battleName", null, false);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return false;
            concerns(__instance).Clear();

            foreach (LogEntry logEntry in entries(__instance))
            {
                if (null != logEntry)
                {
                    IEnumerable<Thing> concerns2 = logEntry.GetConcerns();
                    foreach (Thing thing in concerns2)
                    {
                        if (thing is Pawn pawn)
                        {
                            concerns(__instance).Add(pawn);
                        }
                    }
                }
            }
            return false;
        }

    }
}
