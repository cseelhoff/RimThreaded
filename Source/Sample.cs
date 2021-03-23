using Verse;
using Verse.Sound;

namespace RimThreaded
{

    public class Sample_Patch
    {
        public static bool Update(Sample __instance)
        {
            if (null != __instance.source)
            {
                __instance.source.pitch = __instance.SanitizedPitch;
                __instance.ApplyMappedParameters();
                __instance.source.volume = __instance.SanitizedVolume;
                __instance.source.mute = __instance.source.volume < 1.0 / 1000.0;
                if (!__instance.subDef.tempoAffectedByGameSpeed || __instance.Info.testPlay)
                    return false;
                if (Current.ProgramState == ProgramState.Playing && Find.TickManager.Paused)
                {
                    if (!__instance.source.isPlaying)
                        return false;
                    __instance.source.Pause();
                }
                else
                {
                    if (__instance.source.isPlaying)
                        return false;
                    __instance.source.UnPause();
                }
            }

            return false;
        }

    }
}
