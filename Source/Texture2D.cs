using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

namespace RimThreaded
{

    public class Texture2D_Patch
	{

        public static bool Texture2DWidthHeight(Texture2D __instance, int width, int height)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.texture2dRequests)
                {
                    RimThreaded.texture2dRequests[tID] = new object[] { width, height };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.texture2dResults.TryGetValue(tID, out Texture2D texture2dResult);
                return false;
            }
            return true;
        }
        public static bool Internal_Create(Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureCreationFlags flags, IntPtr nativeTex)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.internal_CreateRequests)
                {
                    RimThreaded.internal_CreateRequests[tID] = new object[] { mono, w, h, mipCount, format, flags, nativeTex };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
        public static bool ReadPixels(Texture2D __instance, Rect source, int destX, int destY, bool recalculateMipMaps = true)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.readPixelRequests)
                {
                    RimThreaded.readPixelRequests[tID] = new object[] { __instance, source, destX, destY, recalculateMipMaps };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
        public static bool Apply(Texture2D __instance, bool updateMipmaps = true, bool makeNoLongerReadable=false)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.applyTextureRequests)
                {
                    RimThreaded.applyTextureRequests[tID] = new object[] { __instance, updateMipmaps, makeNoLongerReadable};
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
        public static Texture2D getReadableTexture(Texture2D texture)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.getReadableTextureRequests)
                {
                    RimThreaded.getReadableTextureRequests[tID] = texture;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.getReadableTextureResults.TryGetValue(tID, out Texture2D texture2dResult);
                return texture2dResult;
            }

            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(texture.width, texture.height);
            texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture_Patch.ReleaseTemporaryThreadSafe(temporary);
            return texture2D;
        }
    }
}
