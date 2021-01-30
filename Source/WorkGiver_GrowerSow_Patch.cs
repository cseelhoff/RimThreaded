using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class WorkGiver_GrowerSow_Patch
    {
        public static ThingDef wantedPlantDef = StaticFieldRefAccess<ThingDef>(typeof(WorkGiver_Grower), "wantedPlantDef");
        public static string CantSowCavePlantBecauseOfLightTrans = StaticFieldRefAccess<string>(typeof(WorkGiver_GrowerSow), "CantSowCavePlantBecauseOfLightTrans");
        public static string CantSowCavePlantBecauseUnroofedTrans = StaticFieldRefAccess<string>(typeof(WorkGiver_GrowerSow), "CantSowCavePlantBecauseUnroofedTrans");
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

            if (wantedPlantDef == null)
            {
                wantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
                if (wantedPlantDef == null)
                {
                    __result = null;
                    return false;
                }
            }

            List<Thing> thingList = c.GetThingList(map);
            bool flag = false;
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing.def == wantedPlantDef)
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

            if (wantedPlantDef.plant.cavePlant)
            {
                if (!c.Roofed(map))
                {
                    JobFailReason.Is(CantSowCavePlantBecauseUnroofedTrans);
                    __result = null;
                    return false;
                }

                if (map.glowGrid.GameGlowAt(c, ignoreCavePlants: true) > 0f)
                {
                    JobFailReason.Is(CantSowCavePlantBecauseOfLightTrans);
                    __result = null;
                    return false;
                }
            }

            if (wantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
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

            Thing thing2 = PlantUtility.AdjacentSowBlocker(wantedPlantDef, c, map);
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

            ThingDef thingdef = wantedPlantDef;
            if (thingdef != null && thingdef.plant != null && thingdef.plant.sowMinSkill > 0 && pawn != null && pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < wantedPlantDef.plant.sowMinSkill)
            {
                WorkGiver workGiver = __instance;
                JobFailReason.Is("UnderAllowedSkill".Translate(wantedPlantDef.plant.sowMinSkill), workGiver.def.label);
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

            if (!wantedPlantDef.CanEverPlantAt_NewTemp(c, map) || !PlantUtility.GrowthSeasonNow(c, map, forSowing: true) || !pawn.CanReserve(c, 1, -1, null, forced))
            {
                __result = null;
                return false;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
            job.plantDefToSow = wantedPlantDef;
            __result = job;
            return false;
        }
    }
}