﻿using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{    
    public class ContentFinder_Texture2D_Patch
    {
        static readonly Func<object[], object> safeFunction = parameters =>
         ContentFinder<Texture2D>.Get(
             (string)parameters[0], 
             (bool)parameters[1]);

        public static bool Get(ref Texture2D __result, string itemPath, bool reportFailure = true)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { itemPath, reportFailure } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (Texture2D)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }
        

	}

}
