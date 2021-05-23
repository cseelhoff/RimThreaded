using System;
using Verse;
using Verse.Sound;

namespace RimThreaded
{

    public class Sustainer_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Sustainer);
            Type patched = typeof(Sustainer_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Cleanup");
            RimThreadedHarmony.Prefix(original, patched, "Maintain");
        }

        public static bool Cleanup(Sustainer __instance)
        {
            if (__instance.def.subSounds.Count > 0)
            {
                Find.SoundRoot.sustainerManager.DeregisterSustainer(__instance);
                lock (__instance.subSustainers)
                {
                    for (int index = 0; index < __instance.subSustainers.Count; ++index)
                        __instance.subSustainers[index].Cleanup();
                }
            }
            if (__instance.def.sustainStopSound != null)
            {
                lock (__instance.worldRootObject)
                {
                    if (__instance.worldRootObject != null)
                    {
                        Map map = __instance.info.Maker.Map;
                        if (map != null)
                            __instance.def.sustainStopSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.worldRootObject.transform.position.ToIntVec3(), map, false), MaintenanceType.None));
                    }
                    else
                        __instance.def.sustainStopSound.PlayOneShot(SoundInfo.OnCamera(MaintenanceType.None));
                }
            if (__instance.worldRootObject != null)
                UnityEngine.Object.Destroy(__instance.worldRootObject);
            }
            DebugSoundEventsLog.Notify_SustainerEnded(__instance, __instance.info);
            return false;
        }
        public static bool Maintain(Sustainer __instance)
        {
            if (__instance.Ended)
            {
                //Log.Warning("Tried to maintain ended sustainer: " + __instance.def);
            }
            else if (__instance.info.Maintenance == MaintenanceType.PerTick)
            {
                __instance.lastMaintainTick = Find.TickManager.TicksGame;
            }
            else if (__instance.info.Maintenance == MaintenanceType.PerFrame)
            {
                __instance.lastMaintainFrame = Time_Patch.get_frameCount();
            }
            return false;
        }

    }
}
