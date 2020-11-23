using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using System.Reflection;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{
    public class CE_Utility_Transpile
    {
        public static IEnumerable<CodeInstruction> BlitCrop(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call &&
                    (MethodInfo)codeInstruction.operand == AccessTools.Method(RimThreadedHarmony.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> GetColorSafe(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo &&
                    methodInfo == AccessTools.Method(RimThreadedHarmony.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }


        static readonly Func<object[], object> safeFunction = p => SafeBlit((Texture2D)p[0], (Rect)p[1], (int[])p[2]);
        public static Texture2D Blit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { texture, blitRect, rtSize } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                return (Texture2D)safeFunctionResult;
            }
            return SafeBlit(texture, blitRect, rtSize);
        }

        public static Texture2D SafeBlit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            FilterMode filterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;
            RenderTexture temporary = RenderTexture.GetTemporary(rtSize[0], rtSize[1], 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            temporary.filterMode = FilterMode.Point;
            RenderTexture.active = temporary;
            Graphics.Blit(texture, temporary);
            Texture2D texture2D = new Texture2D((int)blitRect.width, (int)blitRect.height);
            texture2D.ReadPixels(blitRect, 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            texture.filterMode = filterMode;
            return texture2D;
        }
    }
}
