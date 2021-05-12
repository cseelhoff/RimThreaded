using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace RimThreaded
{
    class UnityEngine_Input_Patch
    {
        static Type original = typeof(Input);
        static Type patched = typeof(UnityEngine_Input_Patch);
        private static readonly MethodInfo MethodInputGetMousePosition = Method(original, "get_mousePosition");
        private static readonly MethodInfo MethodInputGetMousePosition_Patched = Method(patched, "get_mousePosition");
        private static readonly Func<object[], object> FuncGetMousePosition = parameters => Input.mousePosition;

        public static Vector3 get_mousePosition()
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return Input.mousePosition;
            threadInfo.safeFunctionRequest = new object[] { FuncGetMousePosition, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Vector3)threadInfo.safeFunctionResult;
        }
        public static IEnumerable<CodeInstruction> TranspileInputGetMousePosition(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                {
                    if (methodInfo == MethodInputGetMousePosition)
                    {
                        //Log.Message("RimThreaded is replacing method call: ");
                        codeInstruction.operand = MethodInputGetMousePosition_Patched;
                    }
                }
                yield return codeInstruction;
            }
        }
    }
}
