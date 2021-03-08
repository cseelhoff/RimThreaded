using System;
using System.Reflection;
using System.Threading;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    public class Camera_Patch
    {
        static readonly MethodInfo methodSet_targetTexture =
            Method(typeof(Camera), "set_targetTexture", new Type[] { typeof(RenderTexture) });

        static readonly Action<Camera, RenderTexture> actionSet_targetTexture =
            (Action<Camera, RenderTexture>)Delegate.CreateDelegate
            (typeof(Action<Camera, RenderTexture>), methodSet_targetTexture);

        static readonly Action<object[]> safeActionSet_targetTexture = parameters =>
            actionSet_targetTexture(
                (Camera)parameters[0],
                (RenderTexture)parameters[1]);

        public static bool set_targetTexture(Camera __instance, RenderTexture value)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] {
                    safeActionSet_targetTexture, new object[] { __instance, value } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

        static MethodInfo methodRender =
            Method(typeof(Camera), "Render", new Type[] { });

        static readonly Action<Camera> actionRender =
            (Action<Camera>)Delegate.CreateDelegate
            (typeof(Action<Camera>), methodRender);

        static readonly Action<object[]> safeActionRender = p =>
            actionRender((Camera)p[0]);
        public static bool Render(Camera __instance)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeActionRender, new object[] { __instance } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
    }
}