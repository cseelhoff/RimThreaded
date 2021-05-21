using System;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    public class Camera_Patch
    {
        static readonly MethodInfo MethodSetTargetTexture =
            Method(typeof(Camera), "set_targetTexture", new Type[] { typeof(RenderTexture) });

        static readonly Action<Camera, RenderTexture> ActionSetTargetTexture =
            (Action<Camera, RenderTexture>)Delegate.CreateDelegate
            (typeof(Action<Camera, RenderTexture>), MethodSetTargetTexture);

        static readonly Action<object[]> SafeActionSetTargetTexture = parameters =>
            ActionSetTargetTexture(
                (Camera)parameters[0],
                (RenderTexture)parameters[1]);

        public static bool set_targetTexture(Camera __instance, RenderTexture value)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] {
                SafeActionSetTargetTexture, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        static readonly MethodInfo MethodRender =
            Method(typeof(Camera), "Render", new Type[] { });

        static readonly Action<Camera> ActionRender =
            (Action<Camera>)Delegate.CreateDelegate
            (typeof(Action<Camera>), MethodRender);

        static readonly Action<object[]> SafeActionRender = p =>
            ActionRender((Camera)p[0]);
        public static bool Render(Camera __instance)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { SafeActionRender, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}