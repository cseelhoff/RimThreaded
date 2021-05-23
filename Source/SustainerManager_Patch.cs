using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class SustainerManager_Patch
    {
        [ThreadStatic] public static Dictionary<SoundDef, List<Sustainer>> playingPerDef;

        public static void RunDestructivePatches()
        {	
            Type original = typeof(SustainerManager);
            Type patched = typeof(SustainerManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegisterSustainer");
            RimThreadedHarmony.Prefix(original, patched, "DeregisterSustainer");
            RimThreadedHarmony.Prefix(original, patched, "SustainerManagerUpdate");
            RimThreadedHarmony.Prefix(original, patched, "UpdateAllSustainerScopes");
            RimThreadedHarmony.Prefix(original, patched, "SustainerExists");
            RimThreadedHarmony.Prefix(original, patched, "EndAllInMap");
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (RimThreaded.allSustainersLock)
            {
                List<Sustainer> newAllSustainers = __instance.AllSustainers.ListFullCopy();
                newAllSustainers.Add(newSustainer);
                __instance.allSustainers = newAllSustainers;
            }
            return false;
        }
        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (RimThreaded.allSustainersLock)
            {
                List<Sustainer> newAllSustainers = __instance.AllSustainers.ListFullCopy();
                newAllSustainers.Remove(oldSustainer);
                __instance.allSustainers = newAllSustainers;
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

            int count = snapshotAllSustainers.Count;
            for (int index = 0; index < count; index++)
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
                int valueCount = value.Count;
                if (valueCount - key.maxVoices < 0)
                {
                    for (int j = 0; j < valueCount; j++)
                    {
                        value[j].scopeFader.inScope = true;
                    }
                    continue;
                }

                for (int k = 0; k < valueCount; k++)
                {
                    value[k].scopeFader.inScope = false;
                }

                value.Sort(SustainerManager.SortSustainersByCameraDistanceCached);
                int num = 0;
                for (int l = 0; l < valueCount; l++)
                {
                    value[l].scopeFader.inScope = true;
                    num++;
                    if (num >= key.maxVoices)
                    {
                        break;
                    }
                }

                for (int m = 0; m < valueCount; m++)
                {
                    if (!value[m].scopeFader.inScope)
                    {
                        value[m].scopeFader.inScopePercent = 0f;
                    }
                }
            }

            //foreach (KeyValuePair<SoundDef, List<Sustainer>> item2 in playingPerDef)
            //{
                //item2.Value.Clear();
                //SimplePool<List<Sustainer>>.Return(item2.Value);
            //}

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
