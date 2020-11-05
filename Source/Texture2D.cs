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

    public class Texture2D_Patch
	{
        public static bool Texture2D(ref Texture2D __result, int width, int height)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.texture2dRequests)
                {
                    RimThreaded.texture2dRequests[tID] = new object[] { width, height };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.texture2dResults.TryGetValue(tID, out Texture2D texture2dResult);
                __result = texture2dResult;
                return false;
            }
            return true;
        }

    }
}
