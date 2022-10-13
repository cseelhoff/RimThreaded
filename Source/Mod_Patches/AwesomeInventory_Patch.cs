using System;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class AwesomeInventory_Patch
    {
        public static Type awesomeInventoryJobsJobGiver_FindItemByRadius;
        public static Type awesomeInventoryJobsJobGiver_FindItemByRadiusSub;
        public static Type jobGiver_AwesomeInventory_TakeArm;
        public static Type awesomeInventoryErrorMessage;
        public static void Patch()
        {
            awesomeInventoryJobsJobGiver_FindItemByRadius = TypeByName("AwesomeInventory.Jobs.JobGiver_FindItemByRadius");
            awesomeInventoryJobsJobGiver_FindItemByRadiusSub = TypeByName("AwesomeInventory.Jobs.JobGiver_FindItemByRadius+<>c__DisplayClass17_0");
            awesomeInventoryErrorMessage = TypeByName("AwesomeInventory.ErrorMessage");
            jobGiver_AwesomeInventory_TakeArm = TypeByName("AwesomeInventory.Jobs.JobGiver_AwesomeInventory_TakeArm");

            Type patched;
            if (awesomeInventoryJobsJobGiver_FindItemByRadius != null)
            {
                string methodName = "Reset";
                Log.Message("RimThreaded is patching " + awesomeInventoryJobsJobGiver_FindItemByRadius.FullName + " " + methodName);
                patched = typeof(JobGiver_FindItemByRadius_Transpile);
                Transpile(awesomeInventoryJobsJobGiver_FindItemByRadius, patched, methodName);
                methodName = "FindItem";
                Log.Message("RimThreaded is patching " + awesomeInventoryJobsJobGiver_FindItemByRadius.FullName + " " + methodName);
                Transpile(awesomeInventoryJobsJobGiver_FindItemByRadius, patched, methodName);
            }
        }
    }
}
