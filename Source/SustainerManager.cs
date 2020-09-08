using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class SustainerManager_Patch
    {
        public static ConcurrentDictionary<Sustainer, Sustainer> allSustainersDict = new ConcurrentDictionary<Sustainer, Sustainer>();
        public static ConcurrentStack<List<Sustainer>> sListStack = new ConcurrentStack<List<Sustainer>>();

        public static bool get_AllSustainers(SustainerManager __instance, ref List<Sustainer> __result)
        {
            __result = allSustainersDict.Values.ToList();
            return false;
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            allSustainersDict.TryAdd(newSustainer, newSustainer);
            return false;
        }

        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            allSustainersDict.TryRemove(oldSustainer, out _);
            return false;
        }
        public static bool SustainerExists(SustainerManager __instance, ref bool __result, SoundDef def)
        {
            Sustainer[] arraySustainers;
            lock (allSustainersDict)
            {
                arraySustainers = allSustainersDict.Values.ToArray();
            }
            for (int index = 0; index < arraySustainers.Length; ++index)
            {
                if (arraySustainers[index].def == def)
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
            
            Sustainer[] arraySustainers;
            lock (allSustainersDict)
            {
                arraySustainers = allSustainersDict.Values.ToArray();
            }
            //test - uncommenting causes strange unity crash
            /*
            for (int index = arraySustainers.Length - 1; index >= 0; --index)
                arraySustainers[index].SustainerUpdate();
            */
            __instance.UpdateAllSustainerScopes();
            
            return false;
        }

        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {            
            Dictionary<SoundDef, List<Sustainer>> playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
            Sustainer[] arraySustainers;
            lock (allSustainersDict)
            {
                arraySustainers = allSustainersDict.Values.ToArray();
            }
            for (int index = 0; index < arraySustainers.Length; ++index)
            {
                Sustainer allSustainer = arraySustainers[index];
                if (!playingPerDef.ContainsKey(allSustainer.def))
                {
                    // List<Sustainer> sustainerList = SimplePool<List<Sustainer>>.Get();
                    List<Sustainer> sustainerList;
                    if (!sListStack.TryPop(out sustainerList))
                        sustainerList = new List<Sustainer>();
                    sustainerList.Add(allSustainer);
                    playingPerDef.Add(allSustainer.def, sustainerList);
                }
                else
                    playingPerDef[allSustainer.def].Add(allSustainer);
            }
            foreach (KeyValuePair<SoundDef, List<Sustainer>> keyValuePair in playingPerDef)
            {
                SoundDef key = keyValuePair.Key;
                List<Sustainer> sustainerList = keyValuePair.Value;
                if (sustainerList.Count - key.maxVoices < 0)
                {
                    for (int index = 0; index < sustainerList.Count; ++index)
                        sustainerList[index].scopeFader.inScope = true;
                }
                else
                {
                    for (int index = 0; index < sustainerList.Count; ++index)
                        sustainerList[index].scopeFader.inScope = false;
                    sustainerList.Sort((a, b) => a.CameraDistanceSquared.CompareTo(b.CameraDistanceSquared));
                    int num = 0;
                    for (int index = 0; index < sustainerList.Count; ++index)
                    {
                        sustainerList[index].scopeFader.inScope = true;
                        ++num;
                        if (num >= key.maxVoices)
                            break;
                    }
                    for (int index = 0; index < sustainerList.Count; ++index)
                    {
                        if (!sustainerList[index].scopeFader.inScope)
                            sustainerList[index].scopeFader.inScopePercent = 0.0f;
                    }
                }
            }
            foreach (KeyValuePair<SoundDef, List<Sustainer>> keyValuePair in playingPerDef)
            {
                keyValuePair.Value.Clear();
                //SimplePool<List<Sustainer>>.Return(keyValuePair.Value);
                sListStack.Push(keyValuePair.Value);
            }
            //playingPerDef().Clear();
            
            return false;
        }
        public static bool EndAllInMap(SustainerManager __instance, Map map)
        {
            Sustainer[] arraySustainers;
            lock (allSustainersDict)
            {
                arraySustainers = allSustainersDict.Values.ToArray();
            }
            for (int index = arraySustainers.Length - 1; index >= 0; --index)
            {
                if (arraySustainers[index].info.Maker.Map == map)
                    arraySustainers[index].End();
            }
            return false;
        }

    }

}
