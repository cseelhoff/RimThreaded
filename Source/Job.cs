using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class Job_Patch
    {
        public static bool MakeDriver(Job __instance, ref JobDriver __result, Pawn driverPawn)
        {
            __result = null;
            JobDef jobDef = __instance.def;
            if (jobDef != null)
            {
                Type driverClass = jobDef.driverClass;
                object obj = Activator.CreateInstance(driverClass);
                JobDriver jobDriver = (JobDriver)obj;
                jobDriver.pawn = driverPawn;
                jobDriver.job = __instance;
                __result = jobDriver;
            }
            return false;
        }
        public static bool ToString(Job __instance, ref String __result)
        {
            JobDef jobDef = __instance.def;
            string text1 = "";
            if (jobDef != null)
            {
                text1 = jobDef.ToString();
            }
            string text = text1 + " (" + __instance.GetUniqueLoadID() + ")";
            if (__instance.targetA.IsValid)
            {
                text = text + " A=" + __instance.targetA.ToString();
            }

            if (__instance.targetB.IsValid)
            {
                text = text + " B=" + __instance.targetB.ToString();
            }

            if (__instance.targetC.IsValid)
            {
                text = text + " C=" + __instance.targetC.ToString();
            }
            __result = text;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Job);
            Type patched = typeof(Job_Patch);
            RimThreadedHarmony.Prefix(original, patched, "MakeDriver");
            RimThreadedHarmony.Prefix(original, patched, "ToString", Type.EmptyTypes);
        }
    }
}