using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.Camera_Patch;

namespace RimThreaded
{
    public class PortraitRenderer_Patch
    {
        public static FieldRef<PortraitRenderer, Pawn> pawnFR = FieldRefAccess<PortraitRenderer, Pawn>("pawn");
        public static bool RenderPortrait(PortraitRenderer __instance, Pawn pawn, RenderTexture renderTexture, Vector3 cameraOffset, float cameraZoom)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Camera portraitCamera = Find.PortraitCamera;
                //portraitCamera.targetTexture = renderTexture;
                set_targetTexture(portraitCamera, renderTexture);
                Vector3 position = portraitCamera.transform.position;
                float orthographicSize = portraitCamera.orthographicSize;
                portraitCamera.transform.position += cameraOffset;
                portraitCamera.orthographicSize = 1f / cameraZoom;
                pawnFR(__instance) = pawn;
                //portraitCamera.Render();
                Render(portraitCamera);
                pawnFR(__instance) = null;
                portraitCamera.transform.position = position;
                portraitCamera.orthographicSize = orthographicSize;
                //portraitCamera.targetTexture = null;
                set_targetTexture(portraitCamera, null);
                return false;
            }
            return true;
        }
    }
}
