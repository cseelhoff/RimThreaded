using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded
{    
    public class MaterialPool_Patch
    {
		public static bool MatFrom(ref Material __result, MaterialRequest req)
		{
			Material material;
			//if (Ticklist_Patch.materialResults.TryGetValue(req, out material))
			//{
			//__result = material;
			//return false;
			//}
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (Ticklist_Patch.eventWaitStarts.TryGetValue(tID, out EventWaitHandle eventWaitStart)) {
				Ticklist_Patch.materialRequests.TryAdd(tID, req);
				Ticklist_Patch.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				Ticklist_Patch.materialResults.TryGetValue(req, out material);
				__result = material;
				return false;
			}
			return true;
		}

	}

}
