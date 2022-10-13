using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class FleckStatic_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(FleckStatic);
            Type patched = typeof(FleckStatic_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_EndOfLife));
            RimThreadedHarmony.Prefix(original, patched, nameof(Draw), new Type[] { typeof(DrawBatch) });
        }

        public static bool get_EndOfLife(FleckStatic __instance, ref bool __result)
        {
            FleckDef def = __instance.def;
            if (def == null)
            {
                __result = true;
                return false;
            }
            __result = __instance.ageSecs >= def.Lifespan;
            return false;
        }
        public static bool Draw(FleckStatic __instance, DrawBatch batch)
        {
            FleckDef def = __instance.def;
            if (def == null)
                return false;
            __instance.Draw(def.altitudeLayer.AltitudeFor(def.altitudeLayerIncOffset), batch);
            return false;
        }
    }
}
