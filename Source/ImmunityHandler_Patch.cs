using System;
using System.Collections.Generic;
using Verse;
using static Verse.ImmunityHandler;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{

    public class ImmunityHandler_Patch
    {
        [ThreadStatic] public static List<ImmunityInfo> tmpNeededImmunitiesNow;
        
        public static void InitializeThreadStatics()
        {
            tmpNeededImmunitiesNow = new List<ImmunityInfo>(); ;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(ImmunityHandler);
            Type patched = typeof(ImmunityHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ImmunityHandlerTick");
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(ImmunityHandler);
            Type patched = typeof(ImmunityHandler_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "NeededImmunitiesNow");
        }


        public static bool ImmunityHandlerTick(ImmunityHandler __instance)
        {
            List<ImmunityInfo> list = __instance.NeededImmunitiesNow();
            for (int i = 0; i < list.Count; i++)
            {
                __instance.TryAddImmunityRecord(list[i].immunity, list[i].source);
            }
            lock (__instance)
            {
                List<ImmunityRecord> newImmunityList = new List<ImmunityRecord>(__instance.immunityList);
                for (int j = 0; j < __instance.immunityList.Count; j++)
                {
                    ImmunityRecord immunityRecord = newImmunityList[j];
                    Hediff firstHediffOfDef = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(immunityRecord.hediffDef);
                    immunityRecord.ImmunityTick(__instance.pawn, firstHediffOfDef != null, firstHediffOfDef);
                }
                for (int num = newImmunityList.Count - 1; num >= 0; num--)
                {
                    if (newImmunityList[num].immunity <= 0f)
                    {
                        bool flag = false;
                        for (int k = 0; k < list.Count; k++)
                        {
                            if (list[k].immunity == newImmunityList[num].hediffDef)
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (!flag)
                        {
                            newImmunityList.RemoveAt(num);
                        }
                    }
                }
                __instance.immunityList = newImmunityList;
            }
            return false;
        }

    }
}
