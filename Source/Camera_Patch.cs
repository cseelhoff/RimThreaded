using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class Camera_Patch
    {
        public static MethodInfo methodSet_targetTexture = Method(typeof(Camera), "set_targetTexture", new Type[] { typeof(RenderTexture) });

        static readonly Action<Camera, RenderTexture> actionSet_targetTexture =
            (Action<Camera, RenderTexture>)Delegate.CreateDelegate
            (typeof(Action<Camera, RenderTexture>), methodSet_targetTexture);

        static readonly Action<object[]> safeActionSet_targetTexture = p =>
            actionSet_targetTexture((Camera)p[0], (RenderTexture)p[1]);



        public static bool set_targetTexture(Camera __instance, RenderTexture value)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeActionSet_targetTexture, new object[] { __instance, value } };
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


        public static MethodInfo methodRender = Method(typeof(Camera), "Render", new Type[] { });

        static readonly Action<Camera> actionRender =
            (Action<Camera>)Delegate.CreateDelegate
            (typeof(Action<Camera>), methodRender);

        static readonly Action<object[]> safeActionRender = p =>
            actionRender((Camera)p[0]);
        public static bool Render(Camera __instance)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeActionRender, new object[] { __instance } };
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
