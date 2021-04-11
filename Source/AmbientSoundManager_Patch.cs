using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    class AmbientSoundManager_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(AmbientSoundManager);
            Type patched = typeof(AmbientSoundManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "EnsureWorldAmbientSoundCreated");
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
