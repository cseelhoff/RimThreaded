using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class Sustainer_Patch
    {
        public static AccessTools.FieldRef<Sustainer, List<SubSustainer>> subSustainers =
            AccessTools.FieldRefAccess<Sustainer, List<SubSustainer>>("subSustainers");
        public static AccessTools.FieldRef<Sustainer, GameObject> worldRootObject =
            AccessTools.FieldRefAccess<Sustainer, GameObject>("worldRootObject");
        public static bool Cleanup(Sustainer __instance)
        {
            if (__instance.def.subSounds.Count > 0)
            {
                Find.SoundRoot.sustainerManager.DeregisterSustainer(__instance);
                lock (subSustainers(__instance))
                {
                    for (int index = 0; index < subSustainers(__instance).Count; ++index)
                        subSustainers(__instance)[index].Cleanup();
                }
            }
            if (__instance.def.sustainStopSound != null)
            {
                lock (worldRootObject(__instance))
                {
                    if ((UnityEngine.Object)worldRootObject(__instance) != (UnityEngine.Object)null)
                    {
                        Map map = __instance.info.Maker.Map;
                        if (map != null)
                            __instance.def.sustainStopSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(worldRootObject(__instance).transform.position.ToIntVec3(), map, false), MaintenanceType.None));
                    }
                    else
                        __instance.def.sustainStopSound.PlayOneShot(SoundInfo.OnCamera(MaintenanceType.None));
                }
            if ((UnityEngine.Object)worldRootObject(__instance) != (UnityEngine.Object)null)
                UnityEngine.Object.Destroy((UnityEngine.Object)worldRootObject(__instance));
            }
            DebugSoundEventsLog.Notify_SustainerEnded(__instance, __instance.info);
            return false;
        }

    }
}
