using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class SustainerAggregatorUtility_Patch
	{
        public static float AggregateRadius =
            AccessTools.StaticFieldRefAccess<float>(typeof(SustainerAggregatorUtility), "AggregateRadius");
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
                if (allSustainer != null && allSustainer.def == def && allSustainer.info.Maker.Map == info.Maker.Map && allSustainer.info.Maker.Cell.InHorDistOf(info.Maker.Cell, AggregateRadius))
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
