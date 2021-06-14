using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class Battle_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Battle);
            Type patched = typeof(Battle_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Absorb));
        }
        public static bool Absorb(Battle __instance, Battle battle)
        {
            lock (__instance)
            {
                __instance.creationTimestamp = Mathf.Min(__instance.creationTimestamp, battle.creationTimestamp);
                __instance.entries.AddRange(battle.entries);
                __instance.concerns.AddRange(battle.concerns);
                __instance.entries = __instance.entries.OrderBy(e => e.Age).ToList();
            }
            battle.entries.Clear();
            battle.concerns.Clear();
            battle.absorbedBy = __instance;
            __instance.battleName = null;
            return false;
        }
    }
}
