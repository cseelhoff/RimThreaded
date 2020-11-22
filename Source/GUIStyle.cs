using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

namespace RimThreaded
{

    public class GUIStyle_Patch
    {
        public static bool CalcHeight(GUIStyle __instance, ref float __result, GUIContent content, float width)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Func<object[], object> safeFunction = p => __instance.CalcHeight((GUIContent)p[0], (float)p[1]);
                object[] functionAndParameters = new object[] { safeFunction, new object[] { content, width } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (float)safeFunctionResult;
                return false;
            }
            return true;
        }

    }
}
