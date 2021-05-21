using HarmonyLib;
using System;
using Verse;
using Verse.Sound;

namespace RimThreaded
{

    public class SustainerAggregatorUtility_Patch
	{
        internal static void RunDestructivePatches()
        {
            Type original = typeof(SustainerAggregatorUtility);
            Type patched = typeof(SustainerAggregatorUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AggregateOrSpawnSustainerFor");
        }
        public static bool AggregateOrSpawnSustainerFor(ref Sustainer __result, ISizeReporter reporter, SoundDef def, SoundInfo info)
        {
            Sustainer sustainer = null;
            Sustainer allSustainer;
            for (int i = 0; i < Find.SoundRoot.sustainerManager.AllSustainers.Count; i++)
            {
                try
                {
                    allSustainer = Find.SoundRoot.sustainerManager.AllSustainers[i];
                } catch (ArgumentOutOfRangeException) { break; }
                if (allSustainer != null && allSustainer.def == def && allSustainer.info.Maker.Map == info.Maker.Map && allSustainer.info.Maker.Cell.InHorDistOf(info.Maker.Cell, SustainerAggregatorUtility.AggregateRadius))
                {
                    sustainer = allSustainer;
                    break;
                }
            }

            if (sustainer == null)
            {
                sustainer = def.TrySpawnSustainer(info);
            }
            else
            {
                sustainer.Maintain();
            }

            if (sustainer.externalParams.sizeAggregator == null)
            {
                sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
            }

            sustainer.externalParams.sizeAggregator.RegisterReporter(reporter);
            __result = sustainer;
            return false;
        }

    }
}
