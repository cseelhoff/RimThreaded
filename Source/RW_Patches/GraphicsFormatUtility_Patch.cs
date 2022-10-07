using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using UnityEngine.Experimental.Rendering;

namespace RimThreaded.RW_Patches
{
    class GraphicsFormatUtility_Patch
    {
        private static readonly Type original = typeof(GraphicsFormatUtility);
        private static readonly Type patched = typeof(GraphicsFormatUtility_Patch);

        private static readonly MethodInfo methodGetGraphicsFormat =
            Method(original, "GetGraphicsFormat", new Type[] { typeof(RenderTextureFormat), typeof(RenderTextureReadWrite) });

        private static readonly Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat> funcGetGraphicsFormat =
            (Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat>)Delegate.CreateDelegate(
                typeof(Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat>), methodGetGraphicsFormat);

        private static readonly Func<object[], object> funcGetGraphicsFormat2 = parameters =>
            funcGetGraphicsFormat((RenderTextureFormat)parameters[0], (RenderTextureReadWrite)parameters[1]);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.harmony.Patch(methodGetGraphicsFormat,
                prefix: new HarmonyMethod(Method(patched, nameof(GetGraphicsFormat))));
        }

        public static bool GetGraphicsFormat(ref GraphicsFormat __result, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { funcGetGraphicsFormat2, new object[] { format, readWrite } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (GraphicsFormat)threadInfo.safeFunctionResult;
            return false;
        }

    }
}
