using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class Resources_Patch
    {
        public static Func<object[], UnityEngine.Object> safeFunction = parameters => 
        Resources.Load(
            (string)parameters[0], 
            (Type)parameters[1]);

        public static UnityEngine.Object Load(string path, Type type)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { path, type } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return (UnityEngine.Object)threadInfo.safeFunctionResult;
            }
            Log.Error("Could not load Resource of type " + type.ToString() + " at path " + path);
            return null;
        }
    }

}
