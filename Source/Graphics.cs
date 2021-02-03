using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class Graphics_Patch
	{
        static readonly Action<object[]> safeFunction = p =>
            Graphics.Blit((Texture)p[0], (RenderTexture)p[1]);

        public static bool Blit(Texture source, RenderTexture dest)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { source, dest } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

        static readonly Action<object[]> safeFunctionDrawMesh = p =>
        Graphics.DrawMesh((Mesh)p[0], (Vector3)p[1], (Quaternion)p[2], (Material)p[3], (int)p[4]);

        public static bool DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunctionDrawMesh, new object[] { mesh, position, rotation, material, layer } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
    }
}
