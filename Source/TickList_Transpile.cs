using System;
using Verse;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    class TickList_Transpile
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(TickList);
            TranspileMethodLock(original, "RegisterThing");
            TranspileMethodLock(original, "DeregisterThing");
        }
    }
}
