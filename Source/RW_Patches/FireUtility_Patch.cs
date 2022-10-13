using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class FireUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FireUtility);
            Type patched = typeof(FireUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryAttachFire));
            RimThreadedHarmony.Prefix(original, patched, nameof(ChanceToStartFireIn), null, false);
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
            if (jobs != null)
                jobs.StopAll();
            pawn.records.Increment(RecordDefOf.TimesOnFire);
            return false;
        }
        public static bool ChanceToStartFireIn(ref float __result, IntVec3 c, Map map)
        {
            if (map == null)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}
