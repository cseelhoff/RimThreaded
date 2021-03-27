using System;
using System.Collections.Generic;
using RimWorld;
using Verse;


namespace RimThreaded
{

    public class BeautyUtility_Patch
    {
        [ThreadStatic] static List<Thing> tempCountedThings;
        [ThreadStatic] static List<IntVec3> tmpBeautyRelevantCells;
        [ThreadStatic] static List<Room> tmpVisibleRooms;

        public static void InitializeThreadStatics()
        {
            tempCountedThings = new List<Thing>();
            tmpBeautyRelevantCells = new List<IntVec3>();
            tmpVisibleRooms = new List<Room>();
        }

        public static bool AverageBeautyPerceptible(ref float __result, IntVec3 root, Map map)
        {
            if (!root.IsValid || !root.InBounds(map))
            {
                __result = 0.0f;
                return false;
            }
            tempCountedThings.Clear();
            float num = 0.0f;
            int num2 = 0;
            List<IntVec3> beautyRelevantCells = FillBeautyRelevantCells(root, map);
            for (int i = 0; i < beautyRelevantCells.Count; i++)
            {
                num += BeautyUtility.CellBeauty(beautyRelevantCells[i], map, tempCountedThings);
                num2++;
            }
            __result = num2 == 0 ? 0.0f : num / num2;
            return false;
        }
        public static List<IntVec3> FillBeautyRelevantCells(IntVec3 root, Map map)
        {
            tmpBeautyRelevantCells.Clear();            
            Room room = root.GetRoom(map);
            if (room == null)
            {
                return tmpBeautyRelevantCells;
            }
            tmpVisibleRooms.Clear();
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
            return tmpBeautyRelevantCells;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(BeautyUtility);
            Type patched = typeof(BeautyUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AverageBeautyPerceptible");
        }
    }
}