using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class ImmunityHandler_Patch
    {
        public static AccessTools.FieldRef<ImmunityHandler, List<ImmunityRecord>> immunityList =
            AccessTools.FieldRefAccess<ImmunityHandler, List<ImmunityRecord>>("immunityList");

        public static bool ImmunityHandlerTick(ImmunityHandler __instance)
        {
            List<ImmunityHandler.ImmunityInfo> tmpNeededImmunitiesNow = new List<ImmunityHandler.ImmunityInfo>();
            //this.NeededImmunitiesNow();
            List<Hediff> hediffs = __instance.pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def.PossibleToDevelopImmunityNaturally())
                {
                    tmpNeededImmunitiesNow.Add(new ImmunityHandler.ImmunityInfo
                    {
                        immunity = hediff.def,
                        source = hediff.def
                    });
                }
            }
            ImmunityHandler.ImmunityInfo[] immunityInfoList = tmpNeededImmunitiesNow.ToArray<ImmunityHandler.ImmunityInfo>();
            for (int index = 0; index < immunityInfoList.Length; ++index)
            {
                //tryAddImmunityRecord.Invoke(__instance, new object[] { immunityInfoList[index].immunity, immunityInfoList[index].source });
                HediffDef def = immunityInfoList[index].immunity;
                if (def.CompProps<HediffCompProperties_Immunizable>() != null && !__instance.ImmunityRecordExists(def))
                {
                    immunityList(__instance).Add(new ImmunityRecord()
                    {
                        hediffDef = def,
                        source = immunityInfoList[index].source
                    });
                }
            }
            for (int index = 0; index < immunityList(__instance).Count; ++index)
            {
                ImmunityRecord immunity = immunityList(__instance)[index];
                Hediff firstHediffOfDef = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(immunity.hediffDef, false);
                immunity.ImmunityTick(__instance.pawn, firstHediffOfDef != null, firstHediffOfDef);
            }
            for (int index1 = immunityList(__instance).Count - 1; index1 >= 0; --index1)
            {
                if (immunityList(__instance)[index1].immunity <= 0f)
                {
                    bool flag = false;
                    for (int index2 = 0; index2 < immunityInfoList.Length; ++index2)
                    {
                        if (immunityInfoList[index2].immunity == immunityList(__instance)[index1].hediffDef)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        immunityList(__instance).RemoveAt(index1);
                }
            }
            return false;
        }

    }
}
