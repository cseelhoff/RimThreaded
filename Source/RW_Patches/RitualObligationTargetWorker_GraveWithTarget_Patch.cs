using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace RimThreaded.RW_Patches
{
    class RitualObligationTargetWorker_GraveWithTarget_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RitualObligationTargetWorker_GraveWithTarget);
            Type patched = typeof(RitualObligationTargetWorker_GraveWithTarget_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(LabelExtraPart), new Type[] { typeof(RitualObligation) });
            RimThreadedHarmony.Prefix(original, patched, nameof(GetTargetInfos));
        }
        internal static IEnumerable<string> GetTargetInfos_E(RitualObligationTargetWorker_GraveWithTarget __instance, RitualObligation obligation)
        {
            if (obligation == null)
            {
                if (Find.IdeoManager.classicMode)
                {
                    yield return "RitualTargetGraveInfoAbstractNoIdeo".Translate();
                }
                else
                {
                    yield return "RitualTargetGraveInfoAbstract".Translate(__instance.parent.ideo.Named("IDEO"));
                }
                yield break;
            }
            TargetInfo targetA = obligation.targetA;
            if (targetA == null)
            {
                yield return "RitualTargetGraveInfoAbstract".Translate(__instance.parent.ideo.Named("IDEO"));
                yield break;
            }
            Thing thing = targetA.Thing;

            Pawn pawn; 
            if (thing is Pawn)
                pawn = thing as Pawn;
            else if (thing is Corpse corpse)
                pawn = corpse.InnerPawn;            
            else
            {
                yield return "RitualTargetGraveInfoAbstract".Translate(__instance.parent.ideo.Named("IDEO"));
                yield break;
            }
            yield return "RitualTargetGraveInfo".Translate(pawn.Named("PAWN"));
        }

        public static bool GetTargetInfos(RitualObligationTargetWorker_GraveWithTarget __instance, ref IEnumerable<string> __result, RitualObligation obligation)
        {
            __result = GetTargetInfos_E(__instance, obligation);
            return false;
        }
        public static bool LabelExtraPart(RitualObligationTargetWorker_GraveWithTarget __instance, ref string __result, RitualObligation obligation)
        {
            __result = string.Empty;
            if (obligation == null)
                return false;
            TargetInfo targetA = obligation.targetA;
            if (targetA == null)
                return false;
            Thing thing = targetA.Thing;
            if (thing is Pawn pawn)
            {
                __result = pawn.LabelShort;
                return false;
            }
            if (thing is Corpse corpse)
            {
                Pawn innerPawn = corpse.InnerPawn;
                if (innerPawn == null)
                    return false;
                __result = corpse.InnerPawn.LabelShort;
                return false;
            }
            return false;
        }
    }
}
