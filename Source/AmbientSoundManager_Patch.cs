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
            foreach (Sustainer s in sustainers)
            {
                if (SustainerManager_Patch.allSustainers(sustainerManager).Contains(s) && !s.Ended)
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
                        SustainerManager_Patch.allSustainers(sustainerManager).Add(item);
                    } catch (Exception)
                    {
                        item.End();
                    }
                }
            }
            return false;
        }
    }
}
