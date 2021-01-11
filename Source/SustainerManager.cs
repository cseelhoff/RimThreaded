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

        //public static AccessTools.FieldRef<SustainerManager, List<Sustainer>> allSustainers =
        //AccessTools.FieldRefAccess< SustainerManager, List<Sustainer>>("allSustainers");
        //public static ConcurrentDictionary<Sustainer, Sustainer> allSustainers = new ConcurrentDictionary<Sustainer, Sustainer>();

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (__instance.AllSustainers)
            {
                __instance.AllSustainers.Add(newSustainer);
            }
            return false;
        }
        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (__instance.AllSustainers)
            {
                __instance.AllSustainers.Remove(oldSustainer);
            }
            return false;
        }
        public static bool SustainerExists(SustainerManager __instance, ref bool __result, SoundDef def)
        {
            //foreach (Sustainer sust in allSustainers(__instance))
            Sustainer sust;
            for (int index = 0; index < __instance.AllSustainers.Count; index++)
            {
                try
                {
                    sust = __instance.AllSustainers[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (null == sust)
                {
                    continue;
                }
                if (sust.def == def)
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
            Sustainer sNum;
            for (int num = __instance.AllSustainers.Count - 1; num >= 0; num--)
            {
                try
                {
                    sNum = __instance.AllSustainers[num];
                } catch(ArgumentOutOfRangeException) { break; }
                if (null != sNum) { 
                    sNum.SustainerUpdate();
                }
            }

            __instance.UpdateAllSustainerScopes();
            return false;
        }

        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {
            //playingPerDef.Clear();
            Dictionary<SoundDef, List<Sustainer>> playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
            Sustainer sust;
            for (int index = 0; index < __instance.AllSustainers.Count; index++)
            {
                try
                {
                    sust = __instance.AllSustainers[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (null == sust || sust.def == null)
                {
                    continue;
                }
                if (!playingPerDef.ContainsKey(sust.def))
                {
                    List<Sustainer> list = new List<Sustainer>();
                    list.Add(sust);
                    playingPerDef.Add(sust.def, list);
                }
                else
                {
                    playingPerDef[sust.def].Add(sust);
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
            Sustainer sust;
            for (int index = 0; index < __instance.AllSustainers.Count; index++)
            {
                try
                {
                    sust = __instance.AllSustainers[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (null == sust)
                {
                    continue;
                }
                if (sust.info.Maker.Map == map)
                    sust.End();
            }
            return false;
        }

    }

}
