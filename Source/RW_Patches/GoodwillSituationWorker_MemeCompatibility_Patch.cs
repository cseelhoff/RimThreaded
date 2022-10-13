using RimWorld;
using System;

namespace RimThreaded.RW_Patches
{
    class GoodwillSituationWorker_MemeCompatibility_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(GoodwillSituationWorker_MemeCompatibility);
            Type patched = typeof(GoodwillSituationWorker_MemeCompatibility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Applies), new Type[] { typeof(Faction), typeof(Faction) });
        }

        public static bool Applies(GoodwillSituationWorker_MemeCompatibility __instance, ref bool __result, Faction a, Faction b)
        {
            Ideo primaryIdeo1 = a.ideos.PrimaryIdeo;
            if (primaryIdeo1 == null)
                return false;
            GoodwillSituationDef def = __instance.def;
            if (def == null)
            {
                __result = false;
                return false;
            }
            if (def.versusAll)
                return primaryIdeo1.memes.Contains(def.meme);
            Ideo primaryIdeo2 = b.ideos.PrimaryIdeo;
            return primaryIdeo2 != null && primaryIdeo1.memes.Contains(def.meme) && primaryIdeo2.memes.Contains(def.otherMeme);
        }
    }
}
