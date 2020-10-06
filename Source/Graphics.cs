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

    public class Graphics_Patch
	{
        public static bool Blit(Texture source, RenderTexture dest)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                RimThreaded.blitRequests.TryAdd(tID, new object[] { source, dest });
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

    }
}
