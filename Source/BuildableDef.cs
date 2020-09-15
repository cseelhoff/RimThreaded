using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class BuildableDef_Patch
    {
        public static AccessTools.FieldRef<BuildableDef, List<PlaceWorker>> placeWorkersInstantiatedInt =
            AccessTools.FieldRefAccess<BuildableDef, List<PlaceWorker>>("placeWorkersInstantiatedInt");

        public static List<PlaceWorker> get_PlaceWorkers(BuildableDef __instance)
        {
            if (__instance.placeWorkers == null)
                return null;
            if(placeWorkersInstantiatedInt(__instance) == null)
                placeWorkersInstantiatedInt(__instance) = new List<PlaceWorker>();
            lock (placeWorkersInstantiatedInt(__instance))
            {
                placeWorkersInstantiatedInt(__instance).Clear(); // = new List<PlaceWorker>();
                foreach (Type placeWorkerType in __instance.placeWorkers)
                {
                    //PlaceWorker placeWorker = (PlaceWorker)Activator.CreateInstance(placeWorkerType);
                    placeWorkersInstantiatedInt(__instance).Add((PlaceWorker)Activator.CreateInstance(placeWorkerType));
                }
            }
            return placeWorkersInstantiatedInt(__instance);
            
        }
        public static bool ForceAllowPlaceOver(BuildableDef __instance, ref bool __result, BuildableDef other)
        {
            if (get_PlaceWorkers(__instance) == null)
            {
                __result = false;
                return false;
            }
            PlaceWorker placeWorker;
            List<PlaceWorker> placeWorkersReturn = get_PlaceWorkers(__instance);
            for (int index = 0; index < placeWorkersReturn.Count; ++index)
            {
                try
                {
                    placeWorker = placeWorkersReturn[index];
                } catch (ArgumentOutOfRangeException) { break; }
                if (null != placeWorker)
                {
                    if (placeWorker.ForceAllowPlaceOver(other))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }

    }
}
