using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;

namespace RimThreaded
{

    public class UniqueIDsManager_Patch
	{
        public static bool GetNextID(ref int __result, ref int nextID)
        {
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Log.Warning("Getting next unique ID during saving or loading. This may cause bugs.");
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
