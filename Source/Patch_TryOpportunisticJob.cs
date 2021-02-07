using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Patch_TryOpportunisticJob
    {
		private static readonly FieldRef<Pawn_JobTracker, Pawn> pawnFieldRef = FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

		public static Pawn getPawn(Pawn_JobTracker jobTracker)
        {
			return pawnFieldRef(jobTracker);
		}
	}
}
