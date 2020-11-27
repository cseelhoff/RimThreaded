using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimThreaded
{

    public class Texture2D_Patch
	{

        public static bool GetPixel(Texture2D __instance, ref Color __result, int x, int y)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Func<object[], object> safeFunction3 = p => __instance.GetPixel((int)p[0], (int)p[1]);
                object[] functionAndParameters = new object[] { safeFunction3, new object[] { x, y } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (Color)safeFunctionResult;
                return false;
            }
            return true;
        }

        public static bool Texture2DWidthHeight(Texture2D __instance, int width, int height)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.texture2dRequests)
                {
                    RimThreaded.texture2dRequests[tID] = new object[] { width, height };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.texture2dResults.TryGetValue(tID, out Texture2D texture2dResult);
                return false;
            }
            return true;
        }

        public static MethodInfo reflectionMethod = AccessTools.Method(typeof(Texture2D),"Internal_Create", new Type[] { typeof(Texture2D), typeof(int), typeof(int), typeof(int), typeof(GraphicsFormat), typeof(TextureCreationFlags), typeof(IntPtr) });

        static readonly Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr> internal_Create =
            (Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr>)Delegate.CreateDelegate
            (typeof(Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr>), reflectionMethod);

        static readonly Action<object[]> safeFunction = p =>
            internal_Create((Texture2D)p[0], (int)p[1], (int)p[2], (int)p[3], (GraphicsFormat)p[4], (TextureCreationFlags)p[5], (IntPtr)p[6]);

        public static bool Internal_Create(Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureCreationFlags flags, IntPtr nativeTex)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { mono, w, h, mipCount, format, flags, nativeTex } };
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


        public static bool ReadPixels(Texture2D __instance, Rect source, int destX, int destY, bool recalculateMipMaps = true)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Action<object[]> safeFunction = p => __instance.ReadPixels((Rect)p[0], (int)p[1], (int)p[2], (bool)p[3]);
                object[] functionAndParameters = new object[] { safeFunction, new object[] { source, destX, destY, recalculateMipMaps } };
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


        public static bool Apply(Texture2D __instance, bool updateMipmaps = true, bool makeNoLongerReadable = false)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Action<object[]> safeFunction = p => __instance.Apply((bool)p[0], (bool)p[1]);
                object[] functionAndParameters = new object[] { safeFunction, new object[] { updateMipmaps, makeNoLongerReadable } };
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
