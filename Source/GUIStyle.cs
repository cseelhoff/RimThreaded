using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

namespace RimThreaded
{

    public class GUIStyle_Patch
    {

        public static bool CalcHeight(GUIStyle __instance, float __result, GUIContent content, float width)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.calcHeightRequests)
                {
                    RimThreaded.calcHeightRequests[tID] = new object[] { __instance, content, width };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.calcHeightResults.TryGetValue(tID, out float calcHeightResult);
                __result = calcHeightResult;
                return false;
            }
            return true;
        }
        
    }
}
