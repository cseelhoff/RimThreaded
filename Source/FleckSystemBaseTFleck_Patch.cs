using System;
using Verse;

namespace RimThreaded
{
    class FleckSystemBaseTFleck_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FleckSystemBase<FleckStatic>);
            Type patched = typeof(FleckSystemBaseTFleck_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CreateFleck));
        }
        public static bool CreateFleck(FleckSystemBase<FleckStatic> __instance, FleckCreationData creationData)
        {
            FleckStatic fleck = new FleckStatic();
            fleck.Setup(creationData);
            if (creationData.def.realTime) {
                lock (__instance.dataGametime)
                {
                    __instance.dataRealtime.Add(fleck);
                }
            }
            else {
                lock (__instance.dataGametime)
                {
                    __instance.dataGametime.Add(fleck);
                }
            }
            return false;
        }
    }
}
