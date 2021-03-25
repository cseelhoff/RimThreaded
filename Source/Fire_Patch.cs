using System;
using System.Collections.Generic;
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
    }
}