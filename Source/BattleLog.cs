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

    public class BattleLog_Patch
    {
        public static AccessTools.FieldRef<BattleLog, List<Battle>> battles =
            AccessTools.FieldRefAccess<BattleLog, List<Battle>>("battles");
        public static AccessTools.FieldRef<BattleLog, HashSet<LogEntry>> activeEntries =
            AccessTools.FieldRefAccess<BattleLog, HashSet<LogEntry>>("activeEntries");
        public static object addLogEntryLock = new object();
        private static void ReduceToCapacity(BattleLog __instance)
        {
            int num = battles(__instance).Count((Battle btl) => btl.AbsorbedBy == null);
            while (num > 20 && battles(__instance)[battles(__instance).Count - 1].LastEntryTimestamp + Mathf.Max(420000, 5000) < Find.TickManager.TicksGame)
            {
                if (battles(__instance)[battles(__instance).Count - 1].AbsorbedBy == null)
                {
                    num--;
                }

                battles(__instance).RemoveAt(battles(__instance).Count - 1);
                activeEntries = null;
            }
        }
        public static bool Add(BattleLog __instance, LogEntry entry)
        {
            lock (addLogEntryLock) {
                Battle battle = null;
                foreach (Pawn concern in entry.GetConcerns())
                {
                    Battle battleActive = concern.records.BattleActive;
                    if (battle == null)
                    {
                        battle = battleActive;
                    }
                    else if (battleActive != null)
                    {
                        battle = ((battle.Importance > battleActive.Importance) ? battle : battleActive);
                    }
                }

                if (battle == null)
                {
                    battle = Battle.Create();
                    battles(__instance).Insert(0, battle);
                }

                foreach (Pawn concern2 in entry.GetConcerns())
                {
                    Battle battleActive2 = concern2.records.BattleActive;
                    if (battleActive2 != null && battleActive2 != battle)
                    {
                        battle.Absorb(battleActive2);
                        battles(__instance).Remove(battleActive2);
                    }

                    concern2.records.EnterBattle(battle);
                }

                battle.Add(entry);
                activeEntries = null;
                ReduceToCapacity(__instance);
            }
            return false;
        }

    }
}
