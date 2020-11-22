using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Threading;
using System.Reflection;
using UnityEngine.Experimental.Rendering;

namespace RimThreaded
{

    public class RenderTexture_Patch
    {
        public static MethodInfo reflectionMethod = typeof(RenderTexture).GetMethod("GetTemporaryImpl", new Type[] { typeof(int), typeof(int), typeof(int), typeof(GraphicsFormat), typeof(int) , typeof(RenderTextureMemoryless), typeof(VRTextureUsage), typeof(bool)  });

        static Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture> getTemporaryImpl = 
            (Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture>)Delegate.CreateDelegate
            (typeof(Func<int, int, int, GraphicsFormat, int, RenderTextureMemoryless, VRTextureUsage, bool, RenderTexture>), reflectionMethod);

        static Func<object[], object> safeFunction = p =>
            getTemporaryImpl((int)p[0], (int)p[1], (int)p[2], (GraphicsFormat)p[3], (int)p[4], (RenderTextureMemoryless)p[5], (VRTextureUsage)p[6], (bool)p[7]);

        public static bool GetTemporaryImpl(ref RenderTexture __result, int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None, VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, useDynamicScale } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (RenderTexture)safeFunctionResult;
                return false;
            }
            return true;
        }

        static readonly Action<object[]> safeFunction2 = p =>
            RenderTexture.ReleaseTemporary((RenderTexture)p[0]);

        public static bool ReleaseTemporary(RenderTexture temp)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction2, new object[] { temp } };
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
        /*
        public static void set_Active(RenderTexture value)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.setActiveTextureRequests)
                {
                    RimThreaded.setActiveTextureRequests[tID] = value;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
            }
        }
        public static bool get_active(RenderTexture __instance, ref RenderTexture __result)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.renderTextureGetActiveRequests)
                {
                    RimThreaded.renderTextureGetActiveRequests[tID] = __instance;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.renderTextureGetActiveResults.TryGetValue(tID, out RenderTexture renderTexture_result);
                __result = renderTexture_result;
                return false;
            }
            return true;
        }

        public static bool set_active(RenderTexture __instance, RenderTexture value)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.renderTextureSetActiveRequests)
                {
                    RimThreaded.renderTextureSetActiveRequests[tID] = value;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                //RimThreaded.renderTextureSetActiveResults.TryGetValue(tID, out RenderTexture renderTexture_result);
                //__result = renderTexture_result;
                return false;
            }
            return true;
        }
        */

    }
}
