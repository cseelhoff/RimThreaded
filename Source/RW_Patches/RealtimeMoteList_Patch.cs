using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class RealtimeMoteList_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(RealtimeMoteList);
            Type patched = typeof(RealtimeMoteList_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Clear");
            RimThreadedHarmony.Prefix(original, patched, "MoteSpawned");
            RimThreadedHarmony.Prefix(original, patched, "MoteDespawned");
        }

        public static bool Clear(RealtimeMoteList __instance)
        {
            lock (__instance)
            {
                __instance.allMotes = new List<Mote>();
            }
            return false;
        }

        public static bool MoteSpawned(RealtimeMoteList __instance, Mote newMote)
        {
            lock (__instance)
            {
                __instance.allMotes.Add(newMote);
            }
            return false;
        }

        public static bool MoteDespawned(RealtimeMoteList __instance, Mote oldMote)
        {
            lock (__instance)
            {
                List<Mote> newMotes = new List<Mote>(__instance.allMotes);
                newMotes.Remove(oldMote);
                __instance.allMotes = newMotes;
            }
            return false;
        }


    }

}
