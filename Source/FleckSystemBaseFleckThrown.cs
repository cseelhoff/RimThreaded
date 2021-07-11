using System;
using Verse;

namespace RimThreaded
{
    class FleckSystemBaseFleckThrown_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FleckSystemBase<FleckThrown>);
            Type patched = typeof(FleckSystemBaseFleckThrown_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CreateFleck));
        }
        public static bool CreateFleck(FleckSystemBase<FleckThrown> __instance, FleckCreationData creationData)
        {
            FleckThrown fleck = new FleckThrown();
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
