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
        private static readonly Func<int> FuncFrameCount =
            (Func<int>)Delegate.CreateDelegate(
                typeof(Func<int>), 
                Method(typeof(Time), "get_frameCount", new Type[] { }));

        private static readonly MethodInfo MethodTimeFrameCount = Method(typeof(Time), "get_frameCount");
        private static readonly MethodInfo MethodTime_PatchedFrameCount = Method(typeof(Time_Patch), "get_frameCount");

        public static int get_frameCount()
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return Time.frameCount;
            threadInfo.safeFunctionRequest = new object[] { FuncFrameCount, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (int)threadInfo.safeFunctionResult;
        }
        
        public static IEnumerable<CodeInstruction> TranspileTimeFrameCount(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                {
                    if (methodInfo == MethodTimeFrameCount)
                    {
                        //Log.Message("RimThreaded is replacing method call: ");
                        codeInstruction.operand = MethodTime_PatchedFrameCount;
                    }
                }
                yield return codeInstruction;
            }
        }
    }
}
