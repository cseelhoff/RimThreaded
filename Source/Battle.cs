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
        public static AccessTools.FieldRef<Battle, int> loadID =
            AccessTools.FieldRefAccess<Battle, int>("loadID");

        public static AccessTools.FieldRef<Battle, int> creationTimestamp =
            AccessTools.FieldRefAccess<Battle, int>("creationTimestamp");
        public static AccessTools.FieldRef<Battle, List<LogEntry>> entries =
            AccessTools.FieldRefAccess<Battle, List<LogEntry>>("entries");
        public static AccessTools.FieldRef<Battle, Battle> absorbedBy =
            AccessTools.FieldRefAccess<Battle, Battle>("absorbedBy");
        public static AccessTools.FieldRef<Battle, string> battleName =
            AccessTools.FieldRefAccess<Battle, string>("battleName");
        public static AccessTools.FieldRef<Battle, HashSet<Pawn>> concerns =
            AccessTools.FieldRefAccess<Battle, HashSet<Pawn>>("concerns");

        public static bool Absorb(Battle __instance, Battle battle)
        {
            creationTimestamp(__instance) = Mathf.Min(creationTimestamp(__instance), creationTimestamp(battle));
            entries(__instance).AddRange(entries(battle));
            concerns(__instance).AddRange(concerns(battle));
            
            entries(__instance) = (from e in entries(__instance)
                            orderby e.Age
                            select e).ToList<LogEntry>();
            /*
            foreach(var e in entries(__instance))
            {

            }
            */
            entries(battle).Clear();
            concerns(battle).Clear();
            absorbedBy(battle) = __instance;
            battleName(__instance) = null;
            return false;
        }

        public static bool ExposeData(Battle __instance)
        {
            Scribe_Values.Look<int>(ref loadID(__instance), "loadID", 0, false);
            Scribe_Values.Look<int>(ref creationTimestamp(__instance), "creationTimestamp", 0, false);
            Scribe_Collections.Look<LogEntry>(ref entries(__instance), "entries", LookMode.Deep, (new object[] { }));
            Scribe_References.Look<Battle>(ref absorbedBy(__instance), "absorbedBy", false);
            Scribe_Values.Look<string>(ref battleName(__instance), "battleName", (string)null, false);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return false;
            concerns(__instance).Clear();
            //foreach (Pawn pawn in entries(__instance).SelectMany<LogEntry, Thing>(e => e.GetConcerns()).OfType<Pawn>())
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
