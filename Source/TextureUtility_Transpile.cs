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
    public class TextureUtility_Transpile
    {
        public static IEnumerable<CodeInstruction> setDrawOffset(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            bool matchFound = false;
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call &&
                    (MethodInfo)codeInstruction.operand == AccessTools.Method(RimThreadedHarmony.giddyUpCoreUtilitiesTextureUtility, "getReadableTexture"))
                {
                    matchFound = true;
                    codeInstruction.operand = AccessTools.Method(typeof(TextureUtility_Transpile), "GetReadableTexture");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if(!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }

        static readonly Func<object[], object> safeFunction2 = p => SafeGetReadableTexture((Texture2D)p[0]);
        public static Texture2D GetReadableTexture(Texture2D texture)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction2, new object[] { texture } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                return (Texture2D)safeFunctionResult;
            }
            return SafeGetReadableTexture(texture);
        }

        public static Texture2D SafeGetReadableTexture(Texture2D texture)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(texture.width, texture.height);
            texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture_Patch.ReleaseTemporary(temporary);
            return texture2D;
        }
    }
}
