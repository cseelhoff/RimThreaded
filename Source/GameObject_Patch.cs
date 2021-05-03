using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.CodeDom;
using System.Reflection;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class GameObject_Patch
    {
        private ConstructorInfo MethodGameObjectString = Constructor(typeof(GameObject), new Type[] { typeof(string) });
        private ConstructorInfo MethodGameObject = Constructor(typeof(GameObject), Type.EmptyTypes);
        private ConstructorInfo MethodGameObjectStringParams = Constructor(typeof(GameObject), new Type[] { typeof(string), typeof(Type[]) });

        private static readonly Func<object[], object> FuncGameObjectString = parameters =>
            new UnityEngine.GameObject((string)parameters[0]);
        private static readonly Func<object[], object> FuncGameObject = parameters =>
            new UnityEngine.GameObject();
        private static readonly Func<object[], object> FuncGameObjectStringParams = parameters =>
            new UnityEngine.GameObject((string)parameters[0], (Type[])parameters[1]);

        public static bool GameObjectString(GameObject __instance, string name)
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return true;
            threadInfo.safeFunctionRequest = new object[] { FuncGameObjectString, new object[] { name } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool GameObject(GameObject __instance)
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return true;
            threadInfo.safeFunctionRequest = new object[] { FuncGameObject, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        public static bool GameObjectStringParams(GameObject __instance, string name, params Type[] components)
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return true;
            threadInfo.safeFunctionRequest = new object[] { FuncGameObjectStringParams, new object[] { name, components } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        public static IEnumerable<CodeInstruction> TranspileGameObjectString(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.operand is ConstructorInfo constructorInfo)
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
