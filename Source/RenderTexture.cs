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
        public static MethodInfo reflectionMethod = AccessTools.Method(typeof(RenderTexture), "GetTemporaryImpl", new Type[] { typeof(int), typeof(int), typeof(int), typeof(GraphicsFormat), typeof(int) , typeof(RenderTextureMemoryless), typeof(VRTextureUsage), typeof(bool)  });

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
        }

        public static bool GetTemporaryImpl(ref RenderTexture __result, int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None, VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, useDynamicScale } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (RenderTexture)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }

        static readonly Action<object[]> safeFunction2 = parameters =>
            RenderTexture.ReleaseTemporary((RenderTexture)parameters[0]);

        public static bool ReleaseTemporary(RenderTexture temp)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction2, new object[] { temp } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

    }
}
