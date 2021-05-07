using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class GameObject_Patch
    {
        private static MethodInfo methodInternal_CreateGameObject =
            Method(typeof(GameObject), "Internal_CreateGameObject", new[]
                {typeof(GameObject), typeof(string)});

        private static readonly Action<GameObject, string> ActionInternal_CreateGameObject =
            (Action<GameObject, string>)Delegate.CreateDelegate(
                typeof(Action<GameObject, string>), methodInternal_CreateGameObject);

        private static readonly Action<object[]> ActionGameObject = parameters =>
            ActionInternal_CreateGameObject((GameObject)parameters[0], (string)parameters[1]);

        private static readonly MethodInfo methodInternal_CreateGameObject_Patch =
            Method(typeof(GameObject_Patch), "Internal_CreateGameObject", new[]
                {typeof(GameObject), typeof(string)});

        private static readonly MethodInfo MethodGameObjectTransform = Method(typeof(GameObject), "get_transform");
        private static readonly MethodInfo MethodGameObject_PatchTransform = Method(typeof(GameObject_Patch), "get_transform");


        public static void Internal_CreateGameObject(GameObject gameObject, string name)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                ActionInternal_CreateGameObject(gameObject, name);
                return;
            }

            threadInfo.safeFunctionRequest = new object[] { ActionGameObject, new object[] { gameObject, name } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(GameObject);
            Type patched = typeof(GameObject_Patch);

            HarmonyMethod transpilerMethod = new HarmonyMethod(Method(patched, "TranspileGameObjectString"));
            RimThreadedHarmony.harmony.Patch(Constructor(original, 
                    new[] {typeof(string)}), transpiler: transpilerMethod);
            RimThreadedHarmony.harmony.Patch(Constructor(original, 
                    new[] {typeof(string), typeof(Type[])}), transpiler: transpilerMethod);
            RimThreadedHarmony.harmony.Patch(Constructor(original, 
                    Type.EmptyTypes), transpiler: transpilerMethod);
        }
        
        public static IEnumerable<CodeInstruction> TranspileGameObjectString(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                {
                    if (methodInfo == methodInternal_CreateGameObject)
                    {
                        codeInstruction.operand = methodInternal_CreateGameObject_Patch;
                    }
                }
                yield return codeInstruction;
            }
        }
        public static Transform get_transform(GameObject __instance)
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return __instance.transform;
            Func<object[], object> FuncTransform = parameters => __instance.transform;
            threadInfo.safeFunctionRequest = new object[] { FuncTransform, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Transform)threadInfo.safeFunctionResult;
        }

        public static IEnumerable<CodeInstruction> TranspileGameObjectTransform(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                {
                    if (methodInfo == MethodGameObjectTransform)
                    {
                        //Log.Message("RimThreaded is replacing method call: ");
                        codeInstruction.operand = MethodGameObject_PatchTransform;
                    }
                }
                yield return codeInstruction;
            }
        }

    }
}
