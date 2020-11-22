using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded
{    
    public class ContentFinder_Texture2D_Patch
    {
        static readonly Func<object[], object> safeFunction = p =>
         ContentFinder<Texture2D>.Get((string)p[0], (bool)p[1]);

        public static bool Get(ref Texture2D __result, string itemPath, bool reportFailure = true)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { itemPath, reportFailure } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (Texture2D)safeFunctionResult;
                return false;
            }
            return true;
        }
        

	}

}
