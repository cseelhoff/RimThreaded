using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;
using static HarmonyLib.AccessTools;
using System.Threading;

namespace RimThreaded
{

    public class MapGenerator_Patch
	{
        static readonly Func<object[], object> safeFunction = p =>
            MapGenerator.GenerateMap((IntVec3)p[0], (MapParent)p[1], (MapGeneratorDef)p[2], (IEnumerable<GenStepWithParams>)p[3], (Action<Map>)p[4]);

        public static bool GenerateMap(ref Map __result, IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs = null, Action<Map> extraInitBeforeContentGen = null)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { mapSize, parent, mapGenerator, extraGenStepDefs, extraInitBeforeContentGen } };
                lock (RimThreaded.timeoutExemptThreads2)
                {
                    RimThreaded.timeoutExemptThreads2.Add(tID, 60000); //60 sec timeout
                }
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                lock (RimThreaded.timeoutExemptThreads2)
                {
                    if (RimThreaded.timeoutExemptThreads2.ContainsKey(tID))
                    {
                        RimThreaded.timeoutExemptThreads2.Remove(tID);
                    }
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (Map)safeFunctionResult;
                return false;
            }
            return true;
        }
        
    }
}
