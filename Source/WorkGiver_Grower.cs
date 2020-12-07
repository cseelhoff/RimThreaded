using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class WorkGiver_Grower_Patch
    {
        public static ThingDef wantedPlantDef = 
            StaticFieldRefAccess<ThingDef>(typeof(WorkGiver_Grower), "wantedPlantDef");

        public static bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn)
        {
            return true;
        }
        public static Stopwatch sw1 = new Stopwatch();
        public static Stopwatch sw2 = new Stopwatch();
        public static Stopwatch sw3 = new Stopwatch();
        public static Stopwatch sw4 = new Stopwatch();
        public static Stopwatch sw5 = new Stopwatch();
        public static Stopwatch sw6 = new Stopwatch();
        public static Stopwatch sw7 = new Stopwatch();

        public static bool PotentialWorkCellsGlobal(WorkGiver_Grower __instance, ref IEnumerable<IntVec3> __result, Pawn pawn)
        {
            List<IntVec3> result = new List<IntVec3>();
            Danger maxDanger = pawn.NormalMaxDanger();
            List<Building_PlantGrower> bList = ListerBuildings_Patch.get_AllBuildingsColonistBuilding_PlantGrower(pawn.Map.listerBuildings);
            //List<Building> bList = pawn.Map.listerBuildings.allBuildingsColonist;
            sw1.Reset();
            sw2.Reset();
            sw3.Reset();
            sw4.Reset();
            sw5.Reset();
            sw6.Reset();
            sw7.Reset();
            sw7.Start();
            for (int j = 0; j < bList.Count; j++)
            {
                bool flag = false; ;
                Building_PlantGrower building_PlantGrower = bList[j]; // as Building_PlantGrower;
                sw1.Start();
                flag = building_PlantGrower == null;
                sw1.Stop();
                if (flag)
                {
                    continue;
                }

                sw2.Start();
                flag = !ExtraRequirements(building_PlantGrower, pawn);
                sw2.Stop();
                if (flag)
                {
                    continue;
                }

                sw3.Start();
                flag = building_PlantGrower.IsForbidden(pawn);
                sw3.Stop();
                if (flag)
                {
                    continue;
                }

                sw4.Start();
                flag = !pawn.CanReach(building_PlantGrower, PathEndMode.OnCell, maxDanger);
                sw4.Stop();
                if (flag)
                {
                    continue;
                }

                sw5.Start();
                flag = building_PlantGrower.IsBurning();
                sw5.Stop();
                if (flag)
                {
                    continue;
                }

                sw6.Start();
                foreach (IntVec3 item in building_PlantGrower.OccupiedRect())
                {
                    result.Add(item);
                }
                sw6.Stop();

                wantedPlantDef = null;
            }
            sw7.Stop();
            Log.Message("1: " + sw1.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("2: " + sw2.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("3: " + sw3.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("4: " + sw4.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("5: " + sw5.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("6: " + sw6.ElapsedMilliseconds.ToString() + "ms");
            Log.Message("7: " + sw6.ElapsedMilliseconds.ToString() + "ms");
            wantedPlantDef = null;
            List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
            for (int j = 0; j < zonesList.Count; j++)
            {
                Zone_Growing growZone = zonesList[j] as Zone_Growing;
                if (growZone == null)
                {
                    continue;
                }

                if (growZone.cells.Count == 0)
                {
                    Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
                }
                else if (ExtraRequirements(growZone, pawn) && !growZone.ContainsStaticFire && pawn.CanReach(growZone.Cells[0], PathEndMode.OnCell, maxDanger))
                {
                    for (int k = 0; k < growZone.cells.Count; k++)
                    {
                        result.Add( growZone.cells[k]);
                    }
                    wantedPlantDef = null;
                }
            }

            wantedPlantDef = null;
            __result = result;
            return false;
        }

    }
}
