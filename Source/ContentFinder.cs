using System.Threading;
using UnityEngine;

namespace RimThreaded
{    
    public class ContentFinder_Texture2D_Patch
    {
		public static bool Get(ref Texture2D __result, string itemPath, bool reportFailure = true)
		{
			Texture2D texture2d;
			//if (Ticklist_Patch.texture2DResults.TryGetValue(itemPath, out texture2d))
			//{
			//__result = texture2d;
			//return false;
			//}
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (Ticklist_Patch.texture2DWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart)) {
				Ticklist_Patch.texture2DRequests.TryAdd(tID, itemPath);
				Ticklist_Patch.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				Ticklist_Patch.texture2DResults.TryGetValue(itemPath, out texture2d);
				__result = texture2d;
				return false;
			}
			return true;
		}

	}

}
