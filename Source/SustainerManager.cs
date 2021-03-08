using HarmonyLib;
using System;
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
        public static Comparison<Sustainer> SortSustainersByCameraDistanceCached =
            AccessTools.StaticFieldRefAccess<Comparison<Sustainer>>(typeof(SustainerManager), "SortSustainersByCameraDistanceCached");

        public static AccessTools.FieldRef<SustainerManager, List<Sustainer>> allSustainers =
            AccessTools.FieldRefAccess< SustainerManager, List<Sustainer>>("allSustainers");
        //public static ConcurrentDictionary<Sustainer, Sustainer> allSustainers = new ConcurrentDictionary<Sustainer, Sustainer>();

        [ThreadStatic]
        public static Dictionary<SoundDef, List<Sustainer>> playingPerDef;

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (RimThreaded.allSustainersLock)
            {
                List<Sustainer> newAllSustainers = __instance.AllSustainers.ListFullCopy();
                newAllSustainers.Add(newSustainer);
                allSustainers(__instance) = newAllSustainers;
            }
            return false;
        }
        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (RimThreaded.allSustainersLock)
            {
                List<Sustainer> newAllSustainers = __instance.AllSustainers.ListFullCopy();
                newAllSustainers.Remove(oldSustainer);
                allSustainers(__instance) = newAllSustainers;
            }
            return false;
        }
        public static bool SustainerExists(SustainerManager __instance, ref bool __result, SoundDef def)
        {
            List<Sustainer> snapshotAllSustainers = __instance.AllSustainers;
            for (int i = 0; i < snapshotAllSustainers.Count; i++)
            {
                if (snapshotAllSustainers[i].def == def)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
        public static bool SustainerManagerUpdate(SustainerManager __instance)
        {
            List<Sustainer> snapshotAllSustainers = __instance.AllSustainers;
            for (int num = snapshotAllSustainers.Count - 1; num >= 0; num--)
            {
                snapshotAllSustainers[num].SustainerUpdate();
            }
            __instance.UpdateAllSustainerScopes();
            return false;
        }

        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {
            if (playingPerDef == null)
                playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
            else
                playingPerDef.Clear();
            List<Sustainer> snapshotAllSustainers = __instance.AllSustainers;

            for (int index = 0; index < snapshotAllSustainers.Count; index++)
            {
                Sustainer sustainer = snapshotAllSustainers[index];
                if (!playingPerDef.ContainsKey(sustainer.def))
                {
                    List<Sustainer> list = new List<Sustainer>
                    {
                        sustainer
                    };
                    playingPerDef.Add(sustainer.def, list);
                }
                else
                {
                    playingPerDef[sustainer.def].Add(sustainer);
                }
            }

            foreach (KeyValuePair<SoundDef, List<Sustainer>> item in playingPerDef)
            {
                SoundDef key = item.Key;
                List<Sustainer> value = item.Value;
                if (value.Count - key.maxVoices < 0)
                {
                    for (int j = 0; j < value.Count; j++)
                    {
                        value[j].scopeFader.inScope = true;
                    }
                    continue;
                }

                for (int k = 0; k < value.Count; k++)
                {
                    value[k].scopeFader.inScope = false;
                }

                value.Sort(SortSustainersByCameraDistanceCached);
                int num = 0;
                for (int l = 0; l < value.Count; l++)
                {
                    value[l].scopeFader.inScope = true;
                    num++;
                    if (num >= key.maxVoices)
                    {
                        break;
                    }
                }

                for (int m = 0; m < value.Count; m++)
                {
                    if (!value[m].scopeFader.inScope)
                    {
                        value[m].scopeFader.inScopePercent = 0f;
                    }
                }
            }

            foreach (KeyValuePair<SoundDef, List<Sustainer>> item2 in playingPerDef)
            {
                item2.Value.Clear();
                //SimplePool<List<Sustainer>>.Return(item2.Value);
            }

            //playingPerDef.Clear();
            return false;
        }

        public static bool EndAllInMap(SustainerManager __instance, Map map)
        {
            List<Sustainer> snapshotAllSustainers = __instance.AllSustainers;
            for (int index = 0; index < snapshotAllSustainers.Count; index++)
            {
                Sustainer sustainer = snapshotAllSustainers[index];
                if (sustainer.info.Maker.Map == map)
                    sustainer.End();
            }
            return false;
        }

    }

}
