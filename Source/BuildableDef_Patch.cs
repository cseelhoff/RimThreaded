using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class BuildableDef_Patch
    {
        [ThreadStatic] public static List<PlaceWorker> placeWorkersInstantiatedInt;

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(BuildableDef);
            Type patched = typeof(BuildableDef_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched, false);
            RimThreadedHarmony.TranspileFieldReplacements(original, "get_PlaceWorkers");
        }


    }
}
