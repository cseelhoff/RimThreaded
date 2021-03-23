using System;
using Verse;
using UnityEngine;

namespace RimThreaded
{

    public class GlobalControlsUtility_Patch
	{
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
            string label = "TPS: " + RimThreaded.ticksPerSecond.ToString() + "(" + ((int)(Find.TickManager.TickRateMultiplier * 60f)).ToString() + ")";
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            curBaseY -= 26f;
		}

	}
}
