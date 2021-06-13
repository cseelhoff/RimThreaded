using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Sound;

namespace RimThreaded
{
    class SustainerManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(SustainerManager);
            Type patched = typeof(SustainerManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RegisterSustainer));
            RimThreadedHarmony.Prefix(original, patched, nameof(DeregisterSustainer));
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (__instance.allSustainers)
            {
                __instance.allSustainers.Add(newSustainer);
            }
            return false;
        }
        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (__instance.allSustainers)
            {
                List<Sustainer> newSustainers = new List<Sustainer>(__instance.allSustainers);
                newSustainers.Remove(oldSustainer);
                __instance.allSustainers = newSustainers;
            }
            return false;
        }
    }
}
