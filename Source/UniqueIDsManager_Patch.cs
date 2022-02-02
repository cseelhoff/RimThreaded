using System;
using RimWorld;
using Verse;
using System.Threading;

namespace RimThreaded
{

    public class UniqueIDsManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(UniqueIDsManager);
            Type patched = typeof(UniqueIDsManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetNextID));
        }
        public static bool GetNextID(ref int __result, ref int nextID)
        {
                if (Scribe.mode == LoadSaveMode.LoadingVars && !Find.UniqueIDsManager.wasLoaded)
            {
                Log.Warning("Getting next unique ID during LoadingVars before UniqueIDsManager was loaded. Assigning a random value.");
                __result = Rand.Int;
                return false;
            }

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Log.Warning("Getting next unique ID during saving This may cause bugs.");
            }

            //int result = nextID;
            int result = Interlocked.Increment(ref nextID) - 1;
            if (nextID == int.MaxValue)
            {
                Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
                nextID = 0;
            }

            __result = result;
            return false;
        }
    }
}
