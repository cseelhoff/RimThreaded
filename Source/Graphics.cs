using System;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{

    public class Graphics_Patch
	{
        static readonly Action<object[]> safeFunction = p =>
            Graphics.Blit((Texture)p[0], (RenderTexture)p[1]);

        public static bool Blit(Texture source, RenderTexture dest)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { source, dest } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

    }
}
