using System;
using System.CodeDom;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    internal class Material_Patch
    {

        private static readonly Action<Material, string, float> ActionSetStringFloat =
            (Action<Material, string, float>)Delegate.CreateDelegate(
                typeof(Action<Material, string, float>),
                Method(typeof(Material), "SetFloat", new[]
                    { typeof(string), typeof(float) }));

        private static readonly Action<object[]> ActionSetStringFloat2 = parameters =>
            ActionSetStringFloat((Material)parameters[0], (string)parameters[1], (float)parameters[2]);

        private static readonly Action<Material, int, float> ActionSetIntFloat =
            (Action<Material, int, float>)Delegate.CreateDelegate(
                typeof(Action<Material, int, float>),
                Method(typeof(Material), "SetFloat", new[]
                    { typeof(int), typeof(float) }));

        private static readonly Action<object[]> ActionSetIntFloat2 = parameters =>
            ActionSetIntFloat((Material)parameters[0], (int)parameters[1], (float)parameters[2]);

        private static readonly Action<Material, string, int> ActionSetStringInt =
            (Action<Material, string, int>)Delegate.CreateDelegate(
                typeof(Action<Material, string, int>),
                Method(typeof(Material), "SetInt", new[]
                    { typeof(string), typeof(int) }));

        private static readonly Action<object[]> ActionSetStringInt2 = parameters =>
            ActionSetStringInt((Material)parameters[0], (string)parameters[1], (int)parameters[2]);

        private static readonly Action<Material, int, int> ActionSetIntInt =
            (Action<Material, int, int>)Delegate.CreateDelegate(
                typeof(Action<Material, int, int>),
                Method(typeof(Material), "SetInt", new[]
                    { typeof(int), typeof(int) }));

        private static readonly Action<object[]> ActionSetIntInt2 = parameters =>
            ActionSetIntInt((Material)parameters[0], (int)parameters[1], (int)parameters[2]);


        private static readonly MethodInfo methodGetTextureScaleAndOffsetImpl =
            Method(typeof(Material), "GetTextureScaleAndOffsetImpl", new Type[] { typeof(int) });
        private static readonly Func<Material, int, Vector4> funcGetTextureScaleAndOffsetImpl =
            (Func<Material, int, Vector4>)Delegate.CreateDelegate(
                typeof(Func<Material, int, Vector4>), methodGetTextureScaleAndOffsetImpl);

        private static readonly Func<object[], object> funcGetTextureScaleAndOffsetImpl2 = parameters => funcGetTextureScaleAndOffsetImpl((Material)parameters[0], (int)parameters[1]);


        private static readonly MethodInfo methodHasProperty =
            Method(typeof(Material), "HasProperty", new Type[] { typeof(string) });
        private static readonly Func<Material, string, bool> funcHasProperty =
            (Func<Material, string, bool>)Delegate.CreateDelegate(
                typeof(Func<Material, string, bool>), methodHasProperty);

        private static readonly Func<object[], object> funcHasProperty2 = parameters => funcHasProperty((Material)parameters[0], (string)parameters[1]);


        private static readonly Action<Material, int, Vector2> ActionSetTextureOffsetImpl =
            (Action<Material, int, Vector2>)Delegate.CreateDelegate(
                typeof(Action<Material, int, Vector2>),
                Method(typeof(Material), "SetTextureOffsetImpl", new[]
                    { typeof(int), typeof(Vector2) }));

        private static readonly Action<object[]> ActionSetTextureOffsetImpl2 = parameters =>
            ActionSetTextureOffsetImpl((Material)parameters[0], (int)parameters[1], (Vector2)parameters[2]);
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Material);
            Type patched = typeof(Material_Patch);
            RimThreadedHarmony.harmony.Patch(Method(original, "SetFloat", new[] { typeof(string), typeof(float) }),
                prefix: new HarmonyMethod(Method(patched, "SetStringFloat")));
            RimThreadedHarmony.harmony.Patch(Method(original, "SetFloat", new[] { typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(Method(patched, "SetIntFloat")));
            RimThreadedHarmony.harmony.Patch(Method(original, "SetInt", new[] { typeof(string), typeof(int) }),
                prefix: new HarmonyMethod(Method(patched, "SetStringInt")));
            RimThreadedHarmony.harmony.Patch(Method(original, "SetInt", new[] { typeof(int), typeof(int) }),
                prefix: new HarmonyMethod(Method(patched, "SetIntInt")));
            RimThreadedHarmony.harmony.Patch(Method(original, "GetTextureScaleAndOffsetImpl", new[] { typeof(int) }),
                prefix: new HarmonyMethod(Method(patched, "GetTextureScaleAndOffsetImpl")));
            RimThreadedHarmony.harmony.Patch(Method(original, "HasProperty", new[] { typeof(string) }),
                prefix: new HarmonyMethod(Method(patched, "HasProperty")));
            RimThreadedHarmony.harmony.Patch(Method(original, "SetTextureOffsetImpl", new[] { typeof(int), typeof(Vector2) }),
                prefix: new HarmonyMethod(Method(patched, "SetTextureOffsetImpl")));
        }



        public static bool HasProperty(Material __instance, ref bool __result, string name)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { funcHasProperty2, new object[] { __instance, name } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (bool)threadInfo.safeFunctionResult;
            return false;
        }
        public static bool GetTextureScaleAndOffsetImpl(Material __instance, ref Vector4 __result, int name)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { funcGetTextureScaleAndOffsetImpl2, new object[] { __instance, name } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Vector4)threadInfo.safeFunctionResult;
            return false;
        }
        public static bool SetTextureOffsetImpl(Material __instance, int name, Vector2 offset)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionSetTextureOffsetImpl2, new object[] { __instance, name, offset } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool SetStringFloat(Material __instance, string name, float value)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionSetStringFloat2, new object[] { __instance, name, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool SetIntFloat(Material __instance, int nameID, float value)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionSetIntFloat2, new object[] { __instance, nameID, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool SetStringInt(Material __instance, string name, int value)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionSetStringInt2, new object[] { __instance, name, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool SetIntInt(Material __instance, int nameID, int value)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionSetIntInt2, new object[] { __instance, nameID, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}
