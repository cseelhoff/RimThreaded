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
