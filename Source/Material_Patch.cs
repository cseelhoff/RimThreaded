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
