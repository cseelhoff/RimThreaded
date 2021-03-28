using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

	public abstract class Projectile_Patch 
	{
		[ThreadStatic] public static List<Thing> cellThingsFiltered;
		[ThreadStatic] public static List<IntVec3> checkedCells;


		public static void InitializeThreadStatics()
		{
			cellThingsFiltered = new List<Thing>();
			checkedCells = new List<IntVec3>();
		}

		internal static void RunNonDestructivePatches()
		{
			Type original = typeof(Projectile);
			Type patched = typeof(Projectile_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "CheckForFreeInterceptBetween");
			RimThreadedHarmony.TranspileFieldReplacements(original, "ImpactSomething");
		}
    }
}
