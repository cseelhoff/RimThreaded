using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class Area_Patch
    {
        public static Dictionary<Area, Range2D> corners = new Dictionary<Area, Range2D>();

        public struct Range2D
        {
            public int minX;
            public int minZ;
            public int maxX;
            public int maxZ;

            public Range2D(int x1, int z1, int x2, int z2) 
            {
                minX = x1;
                minZ = z1;
                maxX = x2;
                maxZ = z2;
            }
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Area);
            Type patched = typeof(Area_Patch);
            RimThreadedHarmony.Postfix(original, patched, "Set"); //TODO need code to check on shrinking of area
        }

        public static void Set(Area __instance, IntVec3 c, bool val)
        {
            if (!val) return;
            Range2D range = GetCorners(__instance);
            range.minX = Math.Min(range.minX, c.x);
            range.minZ = Math.Min(range.minZ, c.z);
            range.maxX = Math.Max(range.maxX, c.x);
            range.maxZ = Math.Max(range.maxZ, c.z);
        }

        public static Range2D GetCorners(Area __instance)
        {
            if(__instance == null)
            {
                return new Range2D(0, 0, 99999, 99999); //TODO use map max range
            }

            if (corners.TryGetValue(__instance, out Range2D range)) return range;
            bool initialized = false;
            foreach (IntVec3 intVec3 in __instance.ActiveCells)
            {
                if(!initialized)
                {
                    range = new Range2D(intVec3.x, intVec3.z, intVec3.x, intVec3.z);
                    initialized = true;
                }
                range.minX = Math.Min(range.minX, intVec3.x);
                range.minZ = Math.Min(range.minZ, intVec3.z);
                range.maxX = Math.Max(range.maxX, intVec3.x);
                range.maxZ = Math.Max(range.maxZ, intVec3.z);
            }
            corners[__instance] = range;
            return range;
        }

    }
}
