using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class Resources_Patch
    {
        public static Func<object[], UnityEngine.Object> safeFunction = parameters =>
        Resources.Load(
            (string)parameters[0],
            (Type)parameters[1]);

        public static UnityEngine.Object Load(string path, Type type)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return Resources.Load(path, type);
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { path, type } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (UnityEngine.Object)threadInfo.safeFunctionResult;
        }
    }

}
