using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;


namespace RimThreaded
{

    public class BeautyUtility_Patch
    {


        public static bool AverageBeautyPerceptible(ref float __result, IntVec3 root, Map map)
        {
            if (!root.IsValid || !root.InBounds(map))
            {
                __result = 0.0f;
                return false;
            }
            //BeautyUtility.tempCountedThings.Clear();
            List<Thing> tempCountedThings = new List<Thing>();
            float num1 = 0.0f;
            int num2 = 0;
            FillBeautyRelevantCells(root, map);
            IntVec3 cells;
            for (int index = 0; index < BeautyUtility.beautyRelevantCells.Count; ++index)
            {
                try
                {
                    cells = BeautyUtility.beautyRelevantCells[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                num1 += BeautyUtility.CellBeauty(cells, map, tempCountedThings);
                ++num2;
            }
            //BeautyUtility.tempCountedThings.Clear();
            __result = num2 == 0 ? 0.0f : num1 / (float)num2;
            return false;
        }
        public static bool FillBeautyRelevantCells(IntVec3 root, Map map)
        {
            List<IntVec3> tmpBeautyRelevantCells = new List<IntVec3>();
            //beautyRelevantCells.Clear();
            Room room = root.GetRoom(map);
            if (room == null)
            {
                return false;
            }
            List<Room> tmpVisibleRooms = new List<Room>();
            //visibleRooms.Clear();
            tmpVisibleRooms.Add(room);
            if (room.Regions.Count == 1 && room.Regions[0].type == RegionType.Portal)
            {
                foreach (Region neighbor in room.Regions[0].Neighbors)
                {
                    if (!tmpVisibleRooms.Contains(neighbor.Room))
                    {
                        tmpVisibleRooms.Add(neighbor.Room);
                    }
                }
            }

            for (int i = 0; i < BeautyUtility.SampleNumCells_Beauty; i++)
            {
                IntVec3 intVec = root + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map) || intVec.Fogged(map))
                {
                    continue;
                }

                Room room2 = intVec.GetRoom(map);
                if (!tmpVisibleRooms.Contains(room2))
                {
                    bool flag = false;
                    for (int j = 0; j < 8; j++)
                    {
                        IntVec3 loc = intVec + GenAdj.AdjacentCells[j];
                        if (tmpVisibleRooms.Contains(loc.GetRoom(map)))
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        continue;
                    }
                }

                tmpBeautyRelevantCells.Add(intVec);
            }
            BeautyUtility.beautyRelevantCells = tmpBeautyRelevantCells;
            return false;
        }


    }
}