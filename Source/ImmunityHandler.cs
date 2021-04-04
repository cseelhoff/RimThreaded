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

        public static FieldRef<ImmunityHandler, List<ImmunityRecord>> immunityList =
            FieldRefAccess<ImmunityHandler, List<ImmunityRecord>>("immunityList");

        private static readonly MethodInfo methodTryAddImmunityRecord =
            Method(typeof(ImmunityHandler), "TryAddImmunityRecord", new Type[] { typeof(HediffDef), typeof(HediffDef) });
        private static readonly Action<ImmunityHandler, HediffDef, HediffDef> actionTryAddImmunityRecord =
            (Action<ImmunityHandler, HediffDef, HediffDef>)Delegate.CreateDelegate(
                typeof(Action<ImmunityHandler, HediffDef, HediffDef>), methodTryAddImmunityRecord);

        private static readonly MethodInfo methodNeededImmunitiesNow =
            Method(typeof(ImmunityHandler), "NeededImmunitiesNow", new Type[] { });
        private static readonly Func<ImmunityHandler, List<ImmunityInfo>> funcNeededImmunitiesNow =
            (Func<ImmunityHandler, List<ImmunityInfo>>)Delegate.CreateDelegate(typeof(Func<ImmunityHandler, List<ImmunityInfo>>), methodNeededImmunitiesNow);

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
            List<ImmunityInfo> list = funcNeededImmunitiesNow(__instance);
            for (int i = 0; i < list.Count; i++)
            {
                actionTryAddImmunityRecord(__instance, list[i].immunity, list[i].source);
            }
            lock (__instance)
            {
                List<ImmunityRecord> newImmunityList = new List<ImmunityRecord>(immunityList(__instance));
                for (int j = 0; j < immunityList(__instance).Count; j++)
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
                immunityList(__instance) = newImmunityList;
            }
            return false;
        }

    }
}
