using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class WorkGiver_ConstructDeliverResources_Patch
    {
        public static string MissingMaterialsTranslated =
            AccessTools.StaticFieldRefAccess<string>(typeof(WorkGiver_ConstructDeliverResources), "MissingMaterialsTranslated");
        public static string ForbiddenLowerTranslated =
            AccessTools.StaticFieldRefAccess<string>(typeof(WorkGiver_ConstructDeliverResources), "ForbiddenLowerTranslated");
        public static string NoPathTranslated =
            AccessTools.StaticFieldRefAccess<string>(typeof(WorkGiver_ConstructDeliverResources), "NoPathTranslated");
        private static Job InstallJob(Pawn pawn, Blueprint_Install install)
        {
            Thing miniToInstallOrBuildingToReinstall = install.MiniToInstallOrBuildingToReinstall;
            if (miniToInstallOrBuildingToReinstall.IsForbidden(pawn))
            {
                JobFailReason.Is(ForbiddenLowerTranslated);
                return null;
            }

            if (!pawn.CanReach(miniToInstallOrBuildingToReinstall, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
            {
                JobFailReason.Is(NoPathTranslated);
                return null;
            }

            if (!pawn.CanReserve(miniToInstallOrBuildingToReinstall))
            {
                Pawn pawn2 = pawn.Map.reservationManager.FirstRespectedReserver(miniToInstallOrBuildingToReinstall, pawn);
                if (pawn2 != null)
                {
                    JobFailReason.Is("ReservedBy".Translate(pawn2.LabelShort, pawn2));
                }

                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer);
            job.targetA = miniToInstallOrBuildingToReinstall;
            job.targetB = install;
            job.count = 1;
            job.haulMode = HaulMode.ToContainer;
            return job;
        }
        private static void FindAvailableNearbyResources2(Thing firstFoundResource, Pawn pawn, out int resTotalAvailable, List<Thing> resourcesAvailable)
        {
            int num = Mathf.Min(firstFoundResource.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(firstFoundResource.def));
            resTotalAvailable = 0;
            //resourcesAvailable.Clear();
            resourcesAvailable.Add(firstFoundResource);
            resTotalAvailable += firstFoundResource.stackCount;
            if (resTotalAvailable < num)
            {
                foreach (Thing item in GenRadial.RadialDistinctThingsAround(firstFoundResource.Position, firstFoundResource.Map, 5f, useCenter: false))
                {
                    if (resTotalAvailable >= num)
                    {
                        break;
                    }

                    if (item.def == firstFoundResource.def && GenAI.CanUseItemForWork(pawn, item))
                    {
                        resourcesAvailable.Add(item);
                        resTotalAvailable += item.stackCount;
                    }
                }
            }
        }
        private static bool IsNewValidNearbyNeeder(Thing t, HashSet<Thing> nearbyNeeders, IConstructible constructible, Pawn pawn)
        {
            if (!(t is IConstructible) || t == constructible || t is Blueprint_Install || t.Faction != pawn.Faction || t.IsForbidden(pawn) || nearbyNeeders.Contains(t) || !GenConstruct.CanConstruct(t, pawn, checkSkills: false))
            {
                return false;
            }

            return true;
        }

        protected static bool ShouldRemoveExistingFloorFirst(Pawn pawn, Blueprint blue)
        {
            if (!(blue.def.entityDefToBuild is TerrainDef))
            {
                return false;
            }

            if (!pawn.Map.terrainGrid.CanRemoveTopLayerAt(blue.Position))
            {
                return false;
            }

            return true;
        }


        private static HashSet<Thing> FindNearbyNeeders(Pawn pawn, ThingDefCountClass need, IConstructible c, int resTotalAvailable, bool canRemoveExistingFloorUnderNearbyNeeders, out int neededTotal, out Job jobToMakeNeederAvailable)
        {
            neededTotal = need.count;
            HashSet<Thing> hashSet = new HashSet<Thing>();
            Thing thing = (Thing)c;
            foreach (Thing item in GenRadial.RadialDistinctThingsAround(thing.Position, thing.Map, 8f, useCenter: true))
            {
                if (neededTotal >= resTotalAvailable)
                {
                    break;
                }

                if (IsNewValidNearbyNeeder(item, hashSet, c, pawn))
                {
                    Blueprint blueprint = item as Blueprint;
                    if (blueprint == null || !ShouldRemoveExistingFloorFirst(pawn, blueprint))
                    {
                        int num = GenConstruct.AmountNeededByOf((IConstructible)item, need.thingDef);
                        if (num > 0)
                        {
                            hashSet.Add(item);
                            neededTotal += num;
                        }
                    }
                }
            }

            Blueprint blueprint2 = c as Blueprint;
            if (((blueprint2 != null && blueprint2.def.entityDefToBuild is TerrainDef) & canRemoveExistingFloorUnderNearbyNeeders) && neededTotal < resTotalAvailable)
            {
                foreach (Thing item2 in GenRadial.RadialDistinctThingsAround(thing.Position, thing.Map, 3f, useCenter: false))
                {
                    if (IsNewValidNearbyNeeder(item2, hashSet, c, pawn))
                    {
                        Blueprint blueprint3 = item2 as Blueprint;
                        if (blueprint3 != null)
                        {
                            Job job = RemoveExistingFloorJob(pawn, blueprint3);
                            if (job != null)
                            {
                                jobToMakeNeederAvailable = job;
                                return hashSet;
                            }
                        }
                    }
                }
            }

            jobToMakeNeederAvailable = null;
            return hashSet;
        }
        protected static Job RemoveExistingFloorJob(Pawn pawn, Blueprint blue)
        {
            if (!ShouldRemoveExistingFloorFirst(pawn, blue))
            {
                return null;
            }

            if (!pawn.CanReserve(blue.Position, 1, -1, ReservationLayerDefOf.Floor))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.RemoveFloor, blue.Position);
            job.ignoreDesignations = true;
            return job;
        }

        private static bool ResourceValidator(Pawn pawn, ThingDefCountClass need, Thing th)
        {
            if (th.def != need.thingDef)
            {
                return false;
            }

            if (th.IsForbidden(pawn))
            {
                return false;
            }

            if (!pawn.CanReserve(th))
            {
                return false;
            }

            return true;
        }



        public static bool ResourceDeliverJobFor(WorkGiver_ConstructDeliverResources __instance, ref Job __result, Pawn pawn, IConstructible c, bool canRemoveExistingFloorUnderNearbyNeeders = true)
        {
            Blueprint_Install blueprint_Install = c as Blueprint_Install;
            if (blueprint_Install != null)
            {
                __result = InstallJob(pawn, blueprint_Install);
                return false;
            }

            bool flag = false;
            ThingDefCountClass thingDefCountClass = null;
            List<ThingDefCountClass> list = c.MaterialsNeeded();
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                ThingDefCountClass need = list[i];
                if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(need, pawn))
                {
                    flag = true;
                    thingDefCountClass = need;
                    break;
                }

                Thing foundRes = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(need.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => ResourceValidator(pawn, need, r));
                if (foundRes != null)
                {
                    List<Thing> resourcesAvailable = new List<Thing>();
                    FindAvailableNearbyResources2(foundRes, pawn, out int resTotalAvailable, resourcesAvailable);
                    int num0 = Mathf.Min(foundRes.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(foundRes.def));
                    resTotalAvailable = 0;
                    resourcesAvailable.Add(foundRes);
                    resTotalAvailable += foundRes.stackCount;
                    if (resTotalAvailable < num0)
                    {
                        foreach (Thing item in GenRadial.RadialDistinctThingsAround(foundRes.Position, foundRes.Map, 5f, useCenter: false))
                        {
                            if (resTotalAvailable >= num0)
                            {
                                break;
                            }
                            if (item.def == foundRes.def && GenAI.CanUseItemForWork(pawn, item))
                            {
                                resourcesAvailable.Add(item);
                                resTotalAvailable += item.stackCount;
                            }
                        }
                    }
                    int neededTotal;
                    Job jobToMakeNeederAvailable;
                    HashSet<Thing> hashSet = FindNearbyNeeders(pawn, need, c, resTotalAvailable, canRemoveExistingFloorUnderNearbyNeeders, out neededTotal, out jobToMakeNeederAvailable);
                    if (jobToMakeNeederAvailable != null)
                    {
                        __result = jobToMakeNeederAvailable;
                        return false;
                    }

                    hashSet.Add((Thing)c);
                    Thing thing = hashSet.MinBy((Thing nee) => IntVec3Utility.ManhattanDistanceFlat(foundRes.Position, nee.Position));
                    hashSet.Remove(thing);
                    int num = 0;
                    int num2 = 0;
                    do
                    {
                        num += resourcesAvailable[num2].stackCount;
                        num2++;
                    }
                    while (num < neededTotal && num2 < resourcesAvailable.Count);
                    resourcesAvailable.RemoveRange(num2, resourcesAvailable.Count - num2);
                    resourcesAvailable.Remove(foundRes);
                    Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer);
                    job.targetA = foundRes;
                    job.targetQueueA = new List<LocalTargetInfo>();
                    for (num2 = 0; num2 < resourcesAvailable.Count; num2++)
                    {
                        job.targetQueueA.Add(resourcesAvailable[num2]);
                    }

                    job.targetB = thing;
                    if (hashSet.Count > 0)
                    {
                        job.targetQueueB = new List<LocalTargetInfo>();
                        foreach (Thing item in hashSet)
                        {
                            job.targetQueueB.Add(item);
                        }
                    }

                    job.targetC = (Thing)c;
                    job.count = neededTotal;
                    job.haulMode = HaulMode.ToContainer;
                    __result = job;
                    return false;
                }

                flag = true;
                thingDefCountClass = need;
            }

            if (flag)
            {
                JobFailReason.Is($"{MissingMaterialsTranslated}: {thingDefCountClass.thingDef.label}");
            }

            __result = null;
            return false;
        }



    }
}
