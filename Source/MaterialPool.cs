using System;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded
{    
    public class MaterialPool_Patch
    {
		static readonly Func<object[], object> safeFunction = p =>
			MaterialPool.MatFrom((MaterialRequest)p[0]);

		public static bool MatFrom(ref Material __result, MaterialRequest req)
		{
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
			{
				object[] functionAndParameters = new object[] { safeFunction, new object[] { req } };
				lock (RimThreaded.safeFunctionRequests)
				{
					RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
				}
				RimThreaded.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
				__result = (Material)safeFunctionResult;
				return false;
			}
			return true;
		}
	}

}
