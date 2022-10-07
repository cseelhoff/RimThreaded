namespace RimThreaded.Mod_Patches
{
    public class TextureUtility_Transpile
    {
        //public static IEnumerable<CodeInstruction> setDrawOffset(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    List<CodeInstruction> instructionsList = instructions.ToList();
        //    int currentInstructionIndex = 0;
        //    bool matchFound = false;
        //    while (currentInstructionIndex < instructionsList.Count)
        //    {
        //        CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
        //        if (codeInstruction.opcode == OpCodes.Call &&
        //            (MethodInfo)codeInstruction.operand == AccessTools.Method(GiddyUpCore_Patch.giddyUpCoreUtilitiesTextureUtility, "getReadableTexture"))
        //        {
        //            matchFound = true;
        //            codeInstruction.operand = AccessTools.Method(typeof(TextureUtility_Transpile), "GetReadableTexture");
        //        }
        //        yield return codeInstruction;
        //        currentInstructionIndex++;
        //    }
        //    if(!matchFound)
        //    {
        //        Log.Error("IL code instructions not found");
        //    }
        //}

        //static readonly Func<object[], object> safeFunction2 = parameters => 
        //    SafeGetReadableTexture((Texture2D)parameters[0]);
        //public static Texture2D GetReadableTexture(Texture2D texture)
        //{
        //    if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
        //        return SafeGetReadableTexture(texture);
        //    threadInfo.safeFunctionRequest = new object[] { safeFunction2, new object[] { texture } };
        //    mainThreadWaitHandle.Set();
        //    threadInfo.eventWaitStart.WaitOne();
        //    return (Texture2D)threadInfo.safeFunctionResult;
        //}

        //public static Texture2D SafeGetReadableTexture(Texture2D texture)
        //{
        //    RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        //    Graphics.Blit(texture, temporary);
        //    RenderTexture active = RenderTexture.active;
        //    RenderTexture.active = temporary;
        //    Texture2D texture2D = new Texture2D(texture.width, texture.height);
        //    texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
        //    texture2D.Apply();
        //    RenderTexture.active = active;
        //    RenderTexture_Patch.ReleaseTemporary(temporary);
        //    return texture2D;
        //}
    }
}
