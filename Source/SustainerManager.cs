using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            foreach (var kv in allSustainersDict)
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

        public static bool SustainerManagerUpdate(SustainerManager __instance)
        {
            foreach (var kv in allSustainersDict)
            {
                kv.Value.SustainerUpdate();
            }
            __instance.UpdateAllSustainerScopes();
            return false;
        }

        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {            
            Dictionary<SoundDef, List<Sustainer>> playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
            foreach (var kv in allSustainersDict)
            {
                Sustainer allSustainer = kv.Value;
                if (!playingPerDef.ContainsKey(allSustainer.def))
                {
                    if (!sListStack.TryPop(out List<Sustainer> sustainerList))
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
            foreach (var kv in allSustainersDict)
            {
                if (kv.Value.info.Maker.Map == map)
                    kv.Value.End();
            }
            return false;
        }

    }

}
