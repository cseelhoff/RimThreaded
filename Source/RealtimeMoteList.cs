using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;

namespace RimThreaded
{
    
    public class RealtimeMoteList_Patch
    {
        public static ConcurrentDictionary<Mote, Mote> allMotes = new ConcurrentDictionary<Mote, Mote>();

        public static bool Clear(RealtimeMoteList __instance)
        {
            allMotes.Clear();
            return false;
        }

        public static bool MoteSpawned(RealtimeMoteList __instance, Mote newMote)
        {
            allMotes.TryAdd(newMote, newMote);
            return false;
        }

        public static bool MoteDespawned(RealtimeMoteList __instance, Mote oldMote)
        {
            allMotes.TryRemove(oldMote, out _);
            return false;
        }

        public static bool MoteListUpdate(RealtimeMoteList __instance)
        {
            foreach (Mote mote in allMotes.Values.ToList())
            {
                mote.RealtimeUpdate();
            }
            return false;
        }

    }

}
