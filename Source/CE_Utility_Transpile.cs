using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using System.Reflection;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

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

        static readonly Func<object[], object> safeFunction = parameters => 
            SafeBlit(
                (Texture2D)parameters[0], 
                (Rect)parameters[1], 
                (int[])parameters[2]);

        public static Texture2D Blit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { texture, blitRect, rtSize } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return (Texture2D)threadInfo.safeFunctionResult;
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
