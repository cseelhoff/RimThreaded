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
			//if (TickList_Patch.materialResults.TryGetValue(req, out material))
			//{
			//__result = material;
			//return false;
			//}
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (TickList_Patch.materialWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart)) {
				TickList_Patch.materialRequests.TryAdd(tID, req);
				TickList_Patch.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				TickList_Patch.materialResults.TryGetValue(req, out material);
				__result = material;
				return false;
			}
			return true;
		}

	}

}
