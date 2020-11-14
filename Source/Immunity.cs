using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static Verse.ImmunityHandler;
using System.Threading;

namespace RimThreaded
{

    public class ImmunityHandler_Patch
    {
        public static Dictionary<int, List<ImmunityInfo>> immunityInfoLists = new Dictionary<int, List<ImmunityInfo>>();

        public static AccessTools.FieldRef<ImmunityHandler, List<ImmunityRecord>> immunityList =
            AccessTools.FieldRefAccess<ImmunityHandler, List<ImmunityRecord>>("immunityList");

        public static bool NeededImmunitiesNow(ImmunityHandler __instance, ref List<ImmunityInfo> __result)
        {
            List<ImmunityInfo> tmpNeededImmunitiesNow = immunityInfoLists[Thread.CurrentThread.ManagedThreadId]; //Added
            tmpNeededImmunitiesNow.Clear(); //Changed to tmpNeededImmunitiesNow
            List<Hediff> hediffs = __instance.pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def.PossibleToDevelopImmunityNaturally())
                {
                    //Changed to tmpNeededImmunitiesNow
                    tmpNeededImmunitiesNow.Add(new ImmunityInfo
                    {
                        immunity = hediff.def,
                        source = hediff.def
                    });
                }
            }
            //Changed to tmpNeededImmunitiesNow
            __result = tmpNeededImmunitiesNow;
            return false;
        }


            private static void TryAddImmunityRecord2(ImmunityHandler __instance, HediffDef def, HediffDef source)
        {
            //can remove if transpiled
            if (def.CompProps<HediffCompProperties_Immunizable>() != null && !__instance.ImmunityRecordExists(def))
            {
                immunityList(__instance).Add(new ImmunityRecord
                {
                    hediffDef = def,
                    source = source
                });
            }
        }

        public static bool ImmunityHandlerTick(ImmunityHandler __instance)
        {
            List<ImmunityInfo> list = null;
            NeededImmunitiesNow(__instance, ref list);
            for (int i = 0; i < list.Count; i++)
            {
                TryAddImmunityRecord2(__instance, list[i].immunity, list[i].source);
            }
            List<ImmunityRecord> this_immunityList = immunityList(__instance);
            for (int j = 0; j < immunityList(__instance).Count; j++)
            {
                ImmunityRecord immunityRecord = this_immunityList[j];
                Hediff firstHediffOfDef = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(immunityRecord.hediffDef);
                immunityRecord.ImmunityTick(__instance.pawn, firstHediffOfDef != null, firstHediffOfDef);
            }
            for (int num = this_immunityList.Count - 1; num >= 0; num--)
            {
                if (this_immunityList[num].immunity <= 0f)
                {
                    bool flag = false;
                    for (int k = 0; k < list.Count; k++)
                    {
                        if (list[k].immunity == this_immunityList[num].hediffDef)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        this_immunityList.RemoveAt(num);
                    }
                }
            }
            return false;
        }

    }
}
