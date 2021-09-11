using System;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class RitualObligationTargetWorker_GraveWithTarget_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RitualObligationTargetWorker_GraveWithTarget);
            Type patched = typeof(RitualObligationTargetWorker_GraveWithTarget_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(LabelExtraPart), new Type[] { typeof(RitualObligation) });
        }
        public static bool LabelExtraPart(RitualObligationTargetWorker_GraveWithTarget __instance, ref string __result, RitualObligation obligation)
        {
            __result = string.Empty;
            if (obligation == null || obligation.targetA == null || ((Corpse)obligation.targetA.Thing) == null || ((Corpse)obligation.targetA.Thing).InnerPawn == null)
            {
                return false;
            }
            __result = ((Corpse)obligation.targetA.Thing).InnerPawn.LabelShort;
            return false;
        }
    }
}
