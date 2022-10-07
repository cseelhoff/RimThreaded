using System;
using System.Collections.Generic;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    class PawnPathPool_Patch
    {
        [ThreadStatic] public static List<PawnPath> threadStaticPaths;

        internal static void InitializeThreadStatics()
        {
            threadStaticPaths = new List<PawnPath>(64);
        }
        public static void RunDestructivePatches()
        {
            Type original = typeof(PawnPathPool);
            Type patched = typeof(PawnPathPool_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetEmptyPawnPath");
        }

        public static bool GetEmptyPawnPath(PawnPathPool __instance, ref PawnPath __result)
        {
            for (int index = 0; index < threadStaticPaths.Count; ++index)
            {
                if (!threadStaticPaths[index].inUse)
                {
                    threadStaticPaths[index].inUse = true;
                    __result = threadStaticPaths[index];
                    return false;
                }
            }
            //if (threadStaticPaths.Count > this.map.mapPawns.AllPawnsSpawnedCount + 2)
            //{
            //    Log.ErrorOnce("PawnPathPool leak: more paths than spawned pawns. Force-recovering.", 664788);
            //    this.paths.Clear();
            //}
            PawnPath pawnPath = new PawnPath();
            threadStaticPaths.Add(pawnPath);
            pawnPath.inUse = true;
            __result = pawnPath;
            return false;
        }
    }
}
