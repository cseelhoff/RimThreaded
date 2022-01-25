using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class HistoryEventsManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(HistoryEventsManager);
            Type patched = typeof(HistoryEventsManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RecordEvent));
        }
        public static bool RecordEvent(HistoryEventsManager __instance, HistoryEvent historyEvent, bool canApplySelfTookThoughts = true)
        {
            try
            {
                IdeoUtility.Notify_HistoryEvent(historyEvent, canApplySelfTookThoughts);
            }
            catch (Exception ex)
            {
                Log.Error("Error while notifying ideos of a HistoryEvent: " + (object)ex);
            }
            int num;
            if (!historyEvent.args.TryGetArg<int>(HistoryEventArgsNames.CustomGoodwill, out num))
                num = 0;
            Pawn pawn;
            if (historyEvent.args.TryGetArg<Pawn>(HistoryEventArgsNames.Doer, out pawn) && pawn.IsColonist)
            {
                HistoryEventsManager.HistoryEventRecords colonistEvent = __instance.colonistEvents[historyEvent.def];
                if (colonistEvent.ticksGame == null)
                {
                    colonistEvent.ticksGame = new List<int>();
                    colonistEvent.customGoodwill = new List<int>();
                    __instance.colonistEvents[historyEvent.def] = colonistEvent;
                }
                colonistEvent.ticksGame.Add(Find.TickManager.TicksGame);
                colonistEvent.customGoodwill.Add(num);
                if (colonistEvent.ticksGame.Count > historyEvent.def.maxRemembered)
                {
                    colonistEvent.ticksGame.RemoveRange(0, colonistEvent.ticksGame.Count - historyEvent.def.maxRemembered);
                    colonistEvent.customGoodwill.RemoveRange(0, colonistEvent.ticksGame.Count - historyEvent.def.maxRemembered);
                }
            }
            Faction key;
            if (!historyEvent.args.TryGetArg<Faction>(HistoryEventArgsNames.AffectedFaction, out key))
                return false;
            DefMap<HistoryEventDef, HistoryEventsManager.HistoryEventRecords> defMap;
            lock (__instance.eventsAffectingFaction) //added
            {
                if (!__instance.eventsAffectingFaction.TryGetValue(key, out defMap))
                {
                    defMap = new DefMap<HistoryEventDef, HistoryEventsManager.HistoryEventRecords>();
                    __instance.eventsAffectingFaction.Add(key, defMap);
                }
            }
            HistoryEventsManager.HistoryEventRecords historyEventRecords = defMap[historyEvent.def];
            if (historyEventRecords.ticksGame == null)
            {
                historyEventRecords.ticksGame = new List<int>();
                historyEventRecords.customGoodwill = new List<int>();
                defMap[historyEvent.def] = historyEventRecords;
            }
            historyEventRecords.ticksGame.Add(Find.TickManager.TicksGame);
            historyEventRecords.customGoodwill.Add(num);
            if (historyEventRecords.ticksGame.Count <= historyEvent.def.maxRemembered)
                return false;
            historyEventRecords.ticksGame.RemoveRange(0, historyEventRecords.ticksGame.Count - historyEvent.def.maxRemembered);
            historyEventRecords.customGoodwill.RemoveRange(0, historyEventRecords.ticksGame.Count - historyEvent.def.maxRemembered);
            return false;
        }
#endif
    }
}
