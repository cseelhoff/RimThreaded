using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class BuildableDef_Patch
    {
        public static bool get_PlaceWorkers(BuildableDef __instance, ref List<PlaceWorker> __result)
        {
            if (__instance.placeWorkers == null)
            {
                __result = null;
                return false;
            }

            List<PlaceWorker> tmpPlaceWorkersInstantiatedInt = new List<PlaceWorker>();
            foreach (Type placeWorker in __instance.placeWorkers)
            {
                tmpPlaceWorkersInstantiatedInt.Add((PlaceWorker)Activator.CreateInstance(placeWorker));
            }

            __result = tmpPlaceWorkersInstantiatedInt;
            return false;
            
        }

    }
}
