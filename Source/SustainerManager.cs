using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class SustainerManager_Patch
    {
        public static ConcurrentDictionary<Sustainer, Sustainer> allSustainers = new ConcurrentDictionary<Sustainer, Sustainer>();

        public static bool get_AllSustainers(SustainerManager __instance, ref List<Sustainer> __result)
        {
            __result = allSustainers.Values.ToList();
            return false;
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            allSustainers.TryAdd(newSustainer, newSustainer);
            return false;
        }

        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            allSustainers.TryRemove(oldSustainer, out _);
            return false;
        }
        public static bool SustainerExists(SustainerManager __instance, ref bool __result, SoundDef def)
        {
            foreach (var kv in allSustainers)
            {
                if (kv.Value.def == def)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

        private readonly static FieldInfo playingPerDefFI = typeof(SustainerManager).GetField("playingPerDef", BindingFlags.Static | BindingFlags.NonPublic);
        public static void SustainerManagerUpdate(SustainerManager __instance)
        {
            // SustainerManagerUpdate is executed prior to Ticks so this will only ever be run on a single thread.
            Dictionary<SoundDef, List<Sustainer>> d = playingPerDefFI.GetValue(__instance) as Dictionary<SoundDef, List<Sustainer>>;
            d.Clear();
            foreach (var kv in allSustainers)
            {
                if (!d.TryGetValue(kv.Value.def, out List<Sustainer> l))
                {
                    l = new List<Sustainer>();
                    d[kv.Value.def] = l;
                }
                l.Add(kv.Value);
            }
        }

        public static bool EndAllInMap(SustainerManager __instance, Map map)
        {
            foreach (var kv in allSustainers)
            {
                if (kv.Value.info.Maker.Map == map)
                    kv.Value.End();
            }
            return false;
        }

    }

}
