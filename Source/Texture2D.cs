using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{

    public class Texture2D_Patch
	{

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Texture2D);
            Type patched = typeof(Texture2D_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Internal_Create");
            RimThreadedHarmony.Prefix(original, patched, "ReadPixels", new Type[] { typeof(Rect), typeof(int), typeof(int), typeof(bool) });
            RimThreadedHarmony.Prefix(original, patched, "Apply", new Type[] { typeof(bool), typeof(bool) });
        }

        public static bool GetPixel(Texture2D __instance, ref Color __result, int x, int y)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            Func<object[], object> safeFunction3 = parameters =>
                __instance.GetPixel(
                    (int)parameters[0],
                    (int)parameters[1]);
            threadInfo.safeFunctionRequest = new object[] { safeFunction3, new object[] { x, y } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Color)threadInfo.safeFunctionResult;
            return false;
        }
        public static MethodInfo reflectionMethod = AccessTools.Method(typeof(Texture2D),"Internal_Create", new Type[] { typeof(Texture2D), typeof(int), typeof(int), typeof(int), typeof(GraphicsFormat), typeof(TextureCreationFlags), typeof(IntPtr) });

        static readonly Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr> internal_Create =
            (Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr>)Delegate.CreateDelegate
            (typeof(Action<Texture2D, int, int, int, GraphicsFormat, TextureCreationFlags, IntPtr>), reflectionMethod);

        static readonly Action<object[]> safeFunction = parameters =>
            internal_Create(
                (Texture2D)parameters[0], 
                (int)parameters[1], 
                (int)parameters[2], 
                (int)parameters[3], 
                (GraphicsFormat)parameters[4], 
                (TextureCreationFlags)parameters[5], 
                (IntPtr)parameters[6]);

        public static bool Internal_Create(Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureCreationFlags flags, IntPtr nativeTex)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { mono, w, h, mipCount, format, flags, nativeTex } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }


        public static bool ReadPixels(Texture2D __instance, Rect source, int destX, int destY, bool recalculateMipMaps = true)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            Action<object[]> safeFunction = p => __instance.ReadPixels((Rect)p[0], (int)p[1], (int)p[2], (bool)p[3]);
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { source, destX, destY, recalculateMipMaps } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }


        public static bool Apply(Texture2D __instance, bool updateMipmaps = true, bool makeNoLongerReadable = false)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            Action<object[]> safeFunction = p => __instance.Apply((bool)p[0], (bool)p[1]);
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { updateMipmaps, makeNoLongerReadable } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}
