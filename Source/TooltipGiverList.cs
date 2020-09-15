using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class TooltipGiverList_Patch
    {
		public static AccessTools.FieldRef<TooltipGiverList, List<Thing>> givers =
			AccessTools.FieldRefAccess<TooltipGiverList, List<Thing>>("givers");
        public void DispenseAllThingTooltips(TooltipGiverList __instance)
        {
            if (Event.current.type != EventType.Repaint || Find.WindowStack.FloatMenu != null)
            {
                return;
            }

            CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
            float cellSizePixels = Find.CameraDriver.CellSizePixels;
            Vector2 vector = new Vector2(cellSizePixels, cellSizePixels);
            Rect rect = new Rect(0f, 0f, vector.x, vector.y);
            int num = 0;
            for (int i = 0; i < givers(__instance).Count; i++)
            {
                Thing thing = givers(__instance)[i];
                if (!currentViewRect.Contains(thing.Position) || thing.Position.Fogged(thing.Map))
                {
                    continue;
                }

                Vector2 vector2 = thing.DrawPos.MapToUIPosition();
                rect.x = vector2.x - vector.x / 2f;
                rect.y = vector2.y - vector.y / 2f;
                if (rect.Contains(Event.current.mousePosition))
                {
                    string text = null;//ShouldShowShotReport(thing) ? TooltipUtility.ShotCalculationTipString(thing) : null;
                    if (thing.def.hasTooltip || !text.NullOrEmpty())
                    {
                        TipSignal tooltip = thing.GetTooltip();
                        if (!text.NullOrEmpty())
                        {
                            ref string text2 = ref tooltip.text;
                            text2 = text2 + "\n\n" + text;
                        }

                        TooltipHandler.TipRegion(rect, tooltip);
                    }
                }

                num++;
            }
        }

    }
}
