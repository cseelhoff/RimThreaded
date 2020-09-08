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
            placeWorkersInstantiatedInt(__instance) = new List<PlaceWorker>();
            foreach (System.Type placeWorker in __instance.placeWorkers)
                placeWorkersInstantiatedInt(__instance).Add((PlaceWorker)Activator.CreateInstance(placeWorker));
            return placeWorkersInstantiatedInt(__instance);
            
        }
        public static bool ForceAllowPlaceOver(BuildableDef __instance, ref bool __result, BuildableDef other)
        {
            if (get_PlaceWorkers(__instance) == null)
            {
                __result = false;
                return false;
            }
            PlaceWorker[] arrayPlaceWorkers;
            lock (get_PlaceWorkers(__instance))
            {
                arrayPlaceWorkers = get_PlaceWorkers(__instance).ToArray();
            }
            for (int index = 0; index < arrayPlaceWorkers.Length; ++index)
            {
                if (arrayPlaceWorkers[index].ForceAllowPlaceOver(other))
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

    }
}
