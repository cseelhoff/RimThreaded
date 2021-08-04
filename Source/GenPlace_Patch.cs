using System;
using Verse;

namespace RimThreaded
{
    class GenPlace_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GenPlace);
            Type patched = typeof(GenPlace_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryPlaceThing), new Type[] { typeof(Thing), typeof(IntVec3),
              typeof(Map), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>), typeof(Predicate<IntVec3>), 
              typeof(Rot4)});
        }

        public static bool TryPlaceThing( ref bool __result,
          Thing thing,
          IntVec3 center,
          Map map,
          ThingPlaceMode mode,
          out Thing lastResultingThing,
          Action<Thing, int> placedAction = null,
          Predicate<IntVec3> nearPlaceValidator = null,
          Rot4 rot = default(Rot4))
        {
            if (map == null)
            {
                Log.Error("Tried to place thing " + (object)thing + " in a null map.");
                lastResultingThing = (Thing)null;
                __result = false;
                return false;
            }
            if (thing == null)
            {
                lastResultingThing = null;
                __result = false;
                return false;
            }
            ThingDef def = thing.def;
            if (def == null)
            {
                lastResultingThing = null;
                __result = false;
                return false;
            }
            if (def.category == ThingCategory.Filth)
                mode = ThingPlaceMode.Direct;
            if (mode == ThingPlaceMode.Direct)
                return GenPlace.TryPlaceDirect(thing, center, rot, map, out lastResultingThing, placedAction);
            if (mode != ThingPlaceMode.Near)
                throw new InvalidOperationException();
            lastResultingThing = (Thing)null;
            int stackCount;
            do
            {
                stackCount = thing.stackCount;
                IntVec3 bestSpot;
                if (!GenPlace.TryFindPlaceSpotNear(center, rot, map, thing, true, out bestSpot, nearPlaceValidator))
                    return false;
                if (GenPlace.TryPlaceDirect(thing, bestSpot, rot, map, out lastResultingThing, placedAction))
                    return true;
            }
            while (thing.stackCount != stackCount);
            Log.Error("Failed to place " + (object)thing + " at " + (object)center + " in mode " + (object)mode + ".");
            lastResultingThing = (Thing)null;
            __result = false;
            return false;
        }
    }
}
