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
    class Component_Patch
    {
        //private static readonly MethodInfo MethodComponentTransform = Method(typeof(Component), "get_transform");
        //private static readonly MethodInfo MethodComponent_PatchTransform = Method(typeof(Component_Patch), "get_transform");
        
        public static Transform get_transform(Component __instance)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return __instance.transform;
            Func<object[], object> FuncTransform = parameters => __instance.transform;
            threadInfo.safeFunctionRequest = new object[] { FuncTransform, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Transform)threadInfo.safeFunctionResult;
        }

        //public static IEnumerable<CodeInstruction> TranspileComponentTransform(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.operand is MethodInfo methodInfo)
        //        {
        //            if (methodInfo == MethodComponentTransform)
        //            {
        //                //Log.Message("RimThreaded is replacing method call: ");
        //                codeInstruction.operand = MethodComponent_PatchTransform;
        //            }
        //        }
        //        yield return codeInstruction;
        //    }
        //}
    }
}
