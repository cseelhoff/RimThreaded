using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class FireUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FireUtility);
            Type patched = typeof(FireUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryAttachFire));
        }

        public static bool TryAttachFire(Thing t, float fireSize)
        {
            if (!t.CanEverAttachFire() || t.HasAttachment(ThingDefOf.Fire))
                return false;
            Fire fire = (Fire)ThingMaker.MakeThing(ThingDefOf.Fire);
            fire.fireSize = fireSize;
            fire.AttachTo(t);
            GenSpawn.Spawn(fire, t.Position, t.Map, Rot4.North);
            if (!(t is Pawn pawn))
                return false;
            Verse.AI.Pawn_JobTracker jobs = pawn.jobs;
            if(jobs != null)
                jobs.StopAll();
            pawn.records.Increment(RecordDefOf.TimesOnFire);
            return false;
        }
    }
}
