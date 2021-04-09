using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class Toils_Haul_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Toils_Haul);
            Type patched = typeof(Toils_Haul_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ErrorCheckForCarry");
        }
        public static bool ErrorCheckForCarry(Pawn pawn, Thing haulThing)
        {
            if (!haulThing.Spawned)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which isn't spawned."
                }), false);
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                return true;
            }
            if (haulThing.stackCount == 0)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which had stackcount 0. (removing...)"
                }), false);
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                haulThing.Discard(true);
                return true;
            }
            if (pawn.jobs.curJob.count <= 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Invalid count: ",
                    pawn.jobs.curJob.count,
                    ", setting to 1. Job was ",
                    pawn.jobs.curJob
                }), false);
                pawn.jobs.curJob.count = 1;
            }
            return false;
        }
	}
}