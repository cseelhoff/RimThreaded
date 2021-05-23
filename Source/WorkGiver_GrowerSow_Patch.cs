using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class WorkGiver_GrowerSow_Patch
    {

        public static void RunDestructivePatches()
        {
            Type original = typeof(WorkGiver_GrowerSow);
            Type patched = typeof(WorkGiver_GrowerSow_Patch);
            RimThreadedHarmony.Prefix(original, patched, "JobOnCell"); //WorkGiver_Grower.wantedPlantDef replaced with local var for thread overwrite
        }


        public static bool JobOnCell(WorkGiver_GrowerSow __instance, ref Job __result, Pawn pawn, IntVec3 c, bool forced = false)
        {
            Map map = pawn.Map;
            if (c.IsForbidden(pawn))
            {
                __result = null;
                return false;
            }

            if (!PlantUtility.GrowthSeasonNow(c, map, forSowing: true))
            {
                __result = null;
                return false;
            }
            ThingDef localWantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
            WorkGiver_GrowerSow.wantedPlantDef = localWantedPlantDef;
            if (localWantedPlantDef == null)
            {
                __result = null;
                return false;
            }

            List<Thing> thingList = c.GetThingList(map);
            bool flag = false;
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing.def == localWantedPlantDef)
                {
                    __result = null;
                    return false;
                }

                if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
                {
                    flag = true;
                }
            }

            if (flag)
            {
                Thing edifice = c.GetEdifice(map);
                if (edifice == null || edifice.def.fertility < 0f)
                {
                    __result = null;
                    return false;
                }
            }

            if (localWantedPlantDef.plant.cavePlant)
            {
                if (!c.Roofed(map))
                {
                    JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseUnroofedTrans);
                    __result = null;
                    return false;
                }

                if (map.glowGrid.GameGlowAt(c, ignoreCavePlants: true) > 0f)
                {
                    JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseOfLightTrans);
                    __result = null;
                    return false;
                }
            }

            if (localWantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
            {
                __result = null;
                return false;
            }

            Plant plant = c.GetPlant(map);
            if (plant != null && plant.def.plant.blockAdjacentSow)
            {
                if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
                {
                    __result = null;
                    return false;
                }

                __result = JobMaker.MakeJob(JobDefOf.CutPlant, plant);
                return false;
            }

            Thing thing2 = PlantUtility.AdjacentSowBlocker(localWantedPlantDef, c, map);
            if (thing2 != null)
            {
                Plant plant2 = thing2 as Plant;
                if (plant2 != null && pawn.CanReserve(plant2, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
                {
                    IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
                    if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
                    {
                        __result = JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
                        return false;
                    }
                }

                __result = null;
                return false;
            }

            ThingDef thingdef = localWantedPlantDef;
            if (thingdef != null && thingdef.plant != null && thingdef.plant.sowMinSkill > 0 && pawn != null && pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < localWantedPlantDef.plant.sowMinSkill)
            {
                WorkGiver workGiver = __instance;
                JobFailReason.Is("UnderAllowedSkill".Translate(localWantedPlantDef.plant.sowMinSkill), workGiver.def.label);
                __result = null;
                return false;
            }

            for (int j = 0; j < thingList.Count; j++)
            {
                Thing thing3 = thingList[j];
                if (!thing3.def.BlocksPlanting())
                {
                    continue;
                }
                if (!pawn.CanReserve(thing3, 1, -1, null, forced))
                {
                    __result = null;
                    return false;
                }
                if (thing3.def.category == ThingCategory.Plant)
                {
                    if (!thing3.IsForbidden(pawn))
                    {
                        __result = JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
                        return false;
                    }

                    __result = null;
                    return false;
                }

                if (thing3.def.EverHaulable)
                {
                    __result = HaulAIUtility.HaulAsideJobFor(pawn, thing3);
                    return false;
                }

                __result = null;
                return false;
            }

            if (!localWantedPlantDef.CanEverPlantAt_NewTemp(c, map) || !PlantUtility.GrowthSeasonNow(c, map, forSowing: true) || !pawn.CanReserve(c, 1, -1, null, forced))
            {
                __result = null;
                return false;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
            job.plantDefToSow = localWantedPlantDef;
            __result = job;
            return false;
        }
    }
}