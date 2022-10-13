using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class Texture_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Texture);
            Type patched = typeof(Texture_Patch);
            RimThreadedHarmony.harmony.Patch(Method(original, "get_width", Type.EmptyTypes),
                prefix: new HarmonyMethod(Method(patched, nameof(get_width))));
            RimThreadedHarmony.harmony.Patch(Method(original, "get_height", Type.EmptyTypes),
                prefix: new HarmonyMethod(Method(patched, nameof(get_height))));
        }

        private static readonly MethodInfo methodget_width =
            Method(typeof(Texture), "get_width", Type.EmptyTypes);
        private static readonly Func<Texture, int> funcget_width =
            (Func<Texture, int>)Delegate.CreateDelegate(
                typeof(Func<Texture, int>), methodget_width);

        private static readonly Func<object[], object> funcget_width2 = parameters => funcget_width((Texture)parameters[0]);

        public static bool get_width(Texture __instance, ref int __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { funcget_width2, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (int)threadInfo.safeFunctionResult;
            return false;
        }



        private static readonly MethodInfo methodget_height =
            Method(typeof(Texture), "get_height", Type.EmptyTypes);
        private static readonly Func<Texture, int> funcget_height =
            (Func<Texture, int>)Delegate.CreateDelegate(
                typeof(Func<Texture, int>), methodget_height);

        private static readonly Func<object[], object> funcget_height2 = parameters => funcget_height((Texture)parameters[0]);

        public static bool get_height(Texture __instance, ref int __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { funcget_height2, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (int)threadInfo.safeFunctionResult;
            return false;
        }
    }
}
