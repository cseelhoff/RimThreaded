using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimThreaded
{

    public class GraphicDatabaseHeadRecords_Patch
    {
        public static MethodInfo reflectionMethod = AccessTools.Method(typeof(GraphicDatabaseHeadRecords), "BuildDatabaseIfNecessary");

        static readonly Action buildDatabaseIfNecessary =
            (Action)Delegate.CreateDelegate
            (typeof(Action), reflectionMethod);

        static readonly Action<object[]> safeFunction = p =>
            buildDatabaseIfNecessary();

        public static bool BuildDatabaseIfNecessary()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

    }
}
