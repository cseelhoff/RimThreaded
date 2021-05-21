using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class Fire_Patch
    {
        [ThreadStatic] public static List<Thing> flammableList;

        public static void InitializeThreadStatics()
        {
            flammableList = new List<Thing>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Fire);
            Type patched = typeof(Fire_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "DoComplexCalcs");
        }
    }
}