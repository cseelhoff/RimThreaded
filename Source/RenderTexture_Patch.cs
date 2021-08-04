using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class RenderTexture_Patch
    {
        public static MethodInfo reflectionMethod = AccessTools.Method(typeof(RenderTexture), "GetTemporaryImpl", new Type[] { typeof(int), typeof(int), typeof(int), typeof(GraphicsFormat), typeof(int), typeof(RenderTextureMemoryless), typeof(VRTextureUsage), typeof(bool) });

        static Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture> getTemporaryImpl =
            (Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture>)Delegate.CreateDelegate
            (typeof(Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture>), reflectionMethod);

        static readonly Func<object[], object> safeFunction = parameters =>
            getTemporaryImpl(
                (int)parameters[0],
                (int)parameters[1],
                (int)parameters[2],
                (GraphicsFormat)parameters[3],
                (int)parameters[4],
                (RenderTextureMemoryless)parameters[5],
                (VRTextureUsage)parameters[6],
                (bool)parameters[7]);

        internal static void RunDestructivePatches()
        {
            Type original = typeof(RenderTexture);
            Type patched = typeof(RenderTexture_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetTemporaryImpl");

            RimThreadedHarmony.Prefix(original, patched, "set_active");
            RimThreadedHarmony.Prefix(original, patched, "get_active");
        }

        public static bool GetTemporaryImpl(ref RenderTexture __result, int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None, VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, useDynamicScale } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (RenderTexture)threadInfo.safeFunctionResult;
            return false;
        }


        private static readonly Action<RenderTexture> ActionReleaseTemporary =
            (Action<RenderTexture>)Delegate.CreateDelegate(
                typeof(Action<RenderTexture>),
                AccessTools.Method(typeof(RenderTexture), "ReleaseTemporary"));

        private static readonly Action<object[]> ActionReleaseTemporary2 = parameters =>
            ActionReleaseTemporary((RenderTexture)parameters[0]);


        public static void ReleaseTemporary(RenderTexture temp)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                RenderTexture.ReleaseTemporary(temp);
                return;
            }

            threadInfo.safeFunctionRequest = new object[] { ActionReleaseTemporary2, new object[] { temp } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        static readonly Action<object[]> FuncSetActive = p =>
            RenderTexture.active = (RenderTexture)p[0];

        public static bool set_active(RenderTexture value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { FuncSetActive, new object[] { value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        static readonly Func<RenderTexture> FuncGetActive =
            (Func<RenderTexture>)Delegate.CreateDelegate(
                typeof(Func<RenderTexture>), AccessTools.Method(typeof(RenderTexture), "get_active"));

        static readonly Func<object> FuncGetActive2 = () =>
            FuncGetActive();

        public static bool get_active(ref RenderTexture __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;

            threadInfo.safeFunctionRequest = new object[] { FuncGetActive2, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (RenderTexture)threadInfo.safeFunctionResult;
            return false;
        }


    }
}
