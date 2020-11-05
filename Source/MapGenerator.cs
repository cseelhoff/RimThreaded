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

        public static bool GenerateMap(ref Map __result, IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs = null, Action<Map> extraInitBeforeContentGen = null)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.generateMapRequests)
                {
                    RimThreaded.generateMapRequests[tID] = new object[] { mapSize, parent, mapGenerator, extraGenStepDefs, extraInitBeforeContentGen };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.generateMapResults.TryGetValue(tID, out Map map_result);
                __result = map_result;
                return false;
            }
            return true;
        }
    }
}
