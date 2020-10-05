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

        public static bool SustainerManagerUpdate(SustainerManager __instance)
        {
            Sustainer sNum;
            for (int num = allSustainers(__instance).Count - 1; num >= 0; num--)
            {
                try
                {
                    sNum = allSustainers(__instance)[num];
                } catch(ArgumentOutOfRangeException) { break; }
                if (null != sNum) { 
                    sNum.SustainerUpdate();
                }
            }

            __instance.UpdateAllSustainerScopes();
            return false;
        }

        public static bool get_AllSustainers(SustainerManager __instance, ref List<Sustainer> __result)
        {
            __result = allSustainers(__instance);
            return false;
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (allSustainers(__instance))
            {
                allSustainers(__instance).Add(newSustainer);
            }
            return false;
        }

        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (allSustainers(__instance))
            {
                allSustainers(__instance).Remove(oldSustainer);
            }
            return false;
        }
        public static bool SustainerExists(SustainerManager __instance, ref bool __result, SoundDef def)
        {
            //foreach (Sustainer sust in allSustainers(__instance))
            Sustainer sust;
            for(int index = 0; index < allSustainers(__instance).Count; index++)
            {
                try
                {
                    sust = allSustainers(__instance)[index];
                } catch (ArgumentOutOfRangeException) { break; }
                if(null == sust)
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

        private readonly static FieldInfo playingPerDefFI = typeof(SustainerManager).GetField("playingPerDef", BindingFlags.Static | BindingFlags.NonPublic);
        public static void SustainerManagerUpdate2(SustainerManager __instance)
        {
            // SustainerManagerUpdate is executed prior to Ticks so this will only ever be run on a single thread.
            Dictionary<SoundDef, List<Sustainer>> d = playingPerDefFI.GetValue(__instance) as Dictionary<SoundDef, List<Sustainer>>;
            d.Clear();
            Sustainer sust;
            for (int index = 0; index < allSustainers(__instance).Count; index++)
            {
                try
                {
                    sust = allSustainers(__instance)[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (null == sust)
                {
                    continue;
                }
                if (!d.TryGetValue(sust.def, out List<Sustainer> l))
                {
                    l = new List<Sustainer>();
                    d[sust.def] = l;
                }
                l.Add(sust);
            }
        }
        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {
            //playingPerDef.Clear();
            Dictionary<SoundDef, List<Sustainer>> playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
            Sustainer sust;
            for (int index = 0; index < allSustainers(__instance).Count; index++)
            {
                try
                {
                    sust = allSustainers(__instance)[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (null == sust)
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
                }
                else
                {
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
            }

            foreach (KeyValuePair<SoundDef, List<Sustainer>> item2 in playingPerDef)
            {
                item2.Value.Clear();
                SimplePool<List<Sustainer>>.Return(item2.Value);
            }

            //playingPerDef.Clear();
            return false;
        }

        public static bool EndAllInMap(SustainerManager __instance, Map map)
        {
            Sustainer sust;
            for (int index = 0; index < allSustainers(__instance).Count; index++)
            {
                try
                {
                    sust = allSustainers(__instance)[index];
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
