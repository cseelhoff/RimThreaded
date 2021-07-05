using RimWorld;
using System;
using System.Collections.Generic;

namespace RimThreaded
{
    class Pawn_ApparelTracker_Patch
    {
        [ThreadStatic] public static List<Apparel> tmpApparel = new List<Apparel>();
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_ApparelTracker);
            Type patched = typeof(Pawn_ApparelTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LostBodyPart));
        }
        public static bool Notify_LostBodyPart(Pawn_ApparelTracker __instance)
        {
            Pawn_ApparelTracker.tmpApparel.Clear();
            for (int index = 0; index < __instance.wornApparel.Count; ++index)
                Pawn_ApparelTracker.tmpApparel.Add(__instance.wornApparel[index]);
            for (int index = 0; index < Pawn_ApparelTracker.tmpApparel.Count; ++index)
            {
                Apparel ap = Pawn_ApparelTracker.tmpApparel[index];
                if (ap != null && !ApparelUtility.HasPartsToWear(__instance.pawn, ap.def))
                    __instance.Remove(ap);
            }
            return false;
        }
    }
}
