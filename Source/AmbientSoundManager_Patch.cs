using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    class AmbientSoundManager_Patch
    {
        static readonly PropertyInfo altitudeWindSoundCreatedPI = typeof(AmbientSoundManager).GetProperty("AltitudeWindSoundCreated", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly FieldInfo biomeAmbientSustainersFI = typeof(AmbientSoundManager).GetField("biomeAmbientSustainers", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool RecreateMapSustainers()
        {
            if (!(bool)altitudeWindSoundCreatedPI.GetValue(null))
            {
                SoundDefOf.Ambient_AltitudeWind.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.None));
            }
            SustainerManager sustainerManager = Find.SoundRoot.sustainerManager;
            List<Sustainer> sustainers = (biomeAmbientSustainersFI.GetValue(null) as List<Sustainer>);
            //lock (sustainerManager.AllSustainers)
            //{
                foreach (Sustainer s in sustainers)
                {
                    if (sustainerManager.AllSustainers.Contains(s) && !s.Ended)
                    {
                        s.End();
                    }
                }
                sustainers.Clear();
                if (Find.CurrentMap != null)
                {
                    List<SoundDef> soundsAmbient = Find.CurrentMap.Biome.soundsAmbient;
                    for (int j = 0; j < soundsAmbient.Count; j++)
                    {
                        Sustainer item = soundsAmbient[j].TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.None));
                        try
                        {
                            lock (sustainerManager.AllSustainers)
                            {
                                sustainerManager.AllSustainers.Add(item);
                            }
                        }
                        catch (Exception)
                        {
                            item.End();
                        }
                    }
                }
            //}
            return false;
        }
        public static bool EnsureWorldAmbientSoundCreated()
        {
            SoundDef aSpace = null;
            SoundRoot soundRoot = Find.SoundRoot;
            if (null != soundRoot)
            {
                SustainerManager sManager = soundRoot.sustainerManager;
                if (null != sManager)
                {
                    aSpace = SoundDefOf.Ambient_Space;
                    if (null != aSpace)
                    {
                        if (sManager.SustainerExists(aSpace))
                        {
                            return false;
                        }
                    }
                }
            }
            aSpace.TrySpawnSustainer(SoundInfo.OnCamera());
            return false;
        }
    }
}
