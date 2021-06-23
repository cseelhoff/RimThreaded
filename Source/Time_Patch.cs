using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    internal class Time_Patch
    {
        private static readonly MethodInfo MethodTimeGetTime = Method(typeof(Time), "get_time");
        private static readonly MethodInfo MethodTime_PatchedGetTime = Method(typeof(Time_Patch), "get_time");
        private static readonly Func<object[], object> FuncGetTime = parameters => Time.time;

        private static readonly MethodInfo MethodTimeFrameCount = Method(typeof(Time), "get_frameCount");
        private static readonly MethodInfo MethodTime_PatchedFrameCount = Method(typeof(Time_Patch), "get_frameCount");
        private static readonly Func<object[], object> FuncFrameCount = parameters => Time.frameCount;

        private static readonly MethodInfo MethodTimeRealtimeSinceStartup = Method(typeof(Time), "get_realtimeSinceStartup");
        private static readonly MethodInfo MethodTime_PatchedRealtimeSinceStartup = Method(typeof(Time_Patch), "get_realtimeSinceStartup");
        private static readonly Func<object[], object> FuncRealtimeSinceStartup = parameters => Time.realtimeSinceStartup;

        public static float get_time()
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return Time.time;
            threadInfo.safeFunctionRequest = new object[] { FuncGetTime, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (float)threadInfo.safeFunctionResult;
        }
        public static int get_frameCount()
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return Time.frameCount;
            return RimThreaded.frameCount;
        }
        public static float get_realtimeSinceStartup()
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return Time.realtimeSinceStartup;
            threadInfo.safeFunctionRequest = new object[] { FuncRealtimeSinceStartup, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (float)threadInfo.safeFunctionResult;
        }

        //public static IEnumerable<CodeInstruction> TranspileTimeGetTime(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.operand is MethodInfo methodInfo)
        //        {
        //            if (methodInfo == MethodTimeGetTime)
        //            {
        //                //Log.Message("RimThreaded is replacing method call: ");
        //                codeInstruction.operand = MethodTime_PatchedGetTime;
        //            }
        //        }
        //        yield return codeInstruction;
        //    }
        //}
        //public static IEnumerable<CodeInstruction> TranspileTimeFrameCount(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.operand is MethodInfo methodInfo)
        //        {
        //            if (methodInfo == MethodTimeFrameCount)
        //            {
        //                //Log.Message("RimThreaded is replacing method call: ");
        //                codeInstruction.operand = MethodTime_PatchedFrameCount;
        //            }
        //        }
        //        yield return codeInstruction;
        //    }
        //}
        //public static IEnumerable<CodeInstruction> TranspileRealtimeSinceStartup(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.operand is MethodInfo methodInfo)
        //        {
        //            if (methodInfo == MethodTimeRealtimeSinceStartup)
        //            {
        //                //Log.Message("RimThreaded is replacing method call: ");
        //                codeInstruction.operand = MethodTime_PatchedRealtimeSinceStartup;
        //            }
        //        }
        //        yield return codeInstruction;
        //    }
        //}
    }
}
