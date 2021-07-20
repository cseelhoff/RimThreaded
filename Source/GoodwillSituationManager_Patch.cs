using RimWorld;
using System;
using System.Collections.Generic;

namespace RimThreaded
{
    class GoodwillSituationManager_Patch
    {

        internal static void RunDestructivePatches()
        {
#if RW13
            Type original = typeof(GoodwillSituationManager);
            Type patched = typeof(GoodwillSituationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Recalculate), new Type[] { typeof(Faction), typeof(bool) });
#endif
        }

#if RW13
        public static bool Recalculate(GoodwillSituationManager __instance, Faction other, bool canSendHostilityChangedLetter)
        {
            List<GoodwillSituationManager.CachedSituation> outSituations1;
            if (__instance.cachedData.TryGetValue(other, out outSituations1))
            {
                __instance.Recalculate(other, outSituations1);
            }
            else
            {
                List<GoodwillSituationManager.CachedSituation> outSituations2 = new List<GoodwillSituationManager.CachedSituation>();
                __instance.Recalculate(other, outSituations2);
                lock (__instance.cachedData)
                {
                    __instance.cachedData.Add(other, outSituations2);
                }
            }
            __instance.CheckHostilityChanged(other, canSendHostilityChangedLetter);
            return false;
        }
#endif
    }
}
