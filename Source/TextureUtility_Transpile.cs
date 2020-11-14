using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using System.Reflection;

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
                    codeInstruction.operand = AccessTools.Method(typeof(Texture2D_Patch), "getReadableTexture");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if(!matchFound)
            {
                Log.Error("IL code instructions not found");
            }
        }
    }
}
