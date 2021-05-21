using RimWorld;
using System;
using UnityEngine;
using Verse;
using static RimThreaded.Camera_Patch;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    public class PortraitRenderer_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(PortraitRenderer);
            Type patched = typeof(PortraitRenderer_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RenderPortrait");
        }
        public static bool RenderPortrait(PortraitRenderer __instance, Pawn pawn, RenderTexture renderTexture, Vector3 cameraOffset, float cameraZoom)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            Camera portraitCamera = Find.PortraitCamera;
            //portraitCamera.targetTexture = renderTexture;
            set_targetTexture(portraitCamera, renderTexture);
            Vector3 position = Component_Patch.get_transform(portraitCamera).position;
            float orthographicSize = portraitCamera.orthographicSize;
            Component_Patch.get_transform(portraitCamera).position += cameraOffset;
            portraitCamera.orthographicSize = 1f / cameraZoom;
            __instance.pawn = pawn;
            //portraitCamera.Render();
            Render(portraitCamera);
            __instance.pawn = null;
            Component_Patch.get_transform(portraitCamera).position = position;
            portraitCamera.orthographicSize = orthographicSize;
            //portraitCamera.targetTexture = null;
            set_targetTexture(portraitCamera, null);
            return false;
        }
    }
}
