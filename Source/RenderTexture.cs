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
using System.Threading;

namespace RimThreaded
{

    public class RenderTexture_Patch
	{
        public static bool GetTemporary(ref RenderTexture __result, int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                RimThreaded.renderTextureRequests.TryAdd(tID, new object[] { width, height, depthBuffer, format, readWrite });
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.renderTextureResults.TryGetValue(tID, out RenderTexture renderTexture_result);
                __result = renderTexture_result;
                return false;
            }
            return true;
        }



    }
}
