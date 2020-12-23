using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class Resources_Patch
    {
        public static Func<object[], UnityEngine.Object> safeFunction = p => Resources.Load((string)p[0], (Type)p[1]);

        public static UnityEngine.Object Load(string path, Type type)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { path, type } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                return (UnityEngine.Object) safeFunctionResult;
            }
            Log.Error("Could not load Resource of type " + type.ToString() + " at path " + path);
            return null;
        }
    }

}
