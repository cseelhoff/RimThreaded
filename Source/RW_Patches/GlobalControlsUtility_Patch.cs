using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class GlobalControlsUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GlobalControlsUtility);
            Type patched = typeof(GlobalControlsUtility_Patch);
            RimThreadedHarmony.Postfix(original, patched, "DoTimespeedControls");
        }

        public static void DoTimespeedControls(float leftX, float width, ref float curBaseY)
        {
            DateTime now = DateTime.Now;
            if (now.Second != RimThreaded.lastTicksCheck.Second)
            {
                RimThreaded.lastTicksCheck = now;
                RimThreaded.ticksPerSecond = GenTicks.TicksAbs - RimThreaded.lastTicksAbs;
                RimThreaded.lastTicksAbs = GenTicks.TicksAbs;
            }

            Rect rect = new Rect(leftX - 20f, curBaseY - 26f, (float)(width + 20.0 - 7.0), 26f);
            Text.Anchor = TextAnchor.MiddleRight;
            string label = "TPS: " + RimThreaded.ticksPerSecond + "(" + (int)(Find.TickManager.TickRateMultiplier * 60f) + ")";
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            curBaseY -= 26f;
        }

    }
}
