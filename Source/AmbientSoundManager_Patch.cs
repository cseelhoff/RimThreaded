using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class AmbientSoundManager_Patch
    {
        //private static readonly PropertyInfo AltitudeWindSoundCreated = Property(typeof(AmbientSoundManager), "AltitudeWindSoundCreated");
        private static List<Sustainer> biomeAmbientSustainers = StaticFieldRefAccess<List<Sustainer>>(typeof(AmbientSoundManager), "biomeAmbientSustainers");
        

        public static bool RecreateMapSustainers()
        {
            if (!Find.SoundRoot.sustainerManager.SustainerExists(SoundDefOf.Ambient_AltitudeWind))
            {
                SoundDefOf.Ambient_AltitudeWind.TrySpawnSustainer(SoundInfo.OnCamera());
            }
            SustainerManager sustainerManager = Find.SoundRoot.sustainerManager;
            for (int i = 0; i < biomeAmbientSustainers.Count; i++)
            {
                Sustainer sustainer = biomeAmbientSustainers[i];
                if (sustainerManager.AllSustainers.Contains(sustainer) && !sustainer.Ended)
                {
                    sustainer.End();
                }
            }
            lock (RimThreaded.biomeAmbientSustainersLock)
            {
                List<Sustainer> newBiomeAmbientSustainers = new List<Sustainer>();
                if (Find.CurrentMap != null)
                {
                    List<SoundDef> soundsAmbient = Find.CurrentMap.Biome.soundsAmbient;
                    for (int j = 0; j < soundsAmbient.Count; j++)
                    {
                        Sustainer item = soundsAmbient[j].TrySpawnSustainer(SoundInfo.OnCamera());
                        newBiomeAmbientSustainers.Add(item);
                    }
                }
                biomeAmbientSustainers = newBiomeAmbientSustainers;
            }
            return false;
        }
        public static bool EnsureWorldAmbientSoundCreated()
        {
            SoundRoot soundRoot = Find.SoundRoot;
            if (null != soundRoot)
            {
                SustainerManager sManager = soundRoot.sustainerManager;
                if (null != sManager)
                {
                    SoundDef aSpace = SoundDefOf.Ambient_Space;
                    if (null != aSpace)
                    {
                        lock (sManager.AllSustainers)
                        {
                            if (sManager.SustainerExists(aSpace))
                            {
                                return false;
                            } else
                            {
                                aSpace.TrySpawnSustainer(SoundInfo.OnCamera());                                
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
