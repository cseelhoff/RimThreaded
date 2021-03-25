using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class FloatMenuMakerMap_Patch
	{
        [ThreadStatic] public static List<Pawn> tmpPawns;

        public static void InitializeThreadStatics()
        {
            tmpPawns = new List<Pawn>();
        }

    }
}
