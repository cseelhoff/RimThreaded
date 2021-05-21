using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class AlertsReadout_Patch
    {
        public static void RunDestructivesPatches()
        {
            Type original = typeof(AlertsReadout);
            Type patched = typeof(AlertsReadout_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AlertsReadoutUpdate");
        }

        public static bool AlertsReadoutUpdate(AlertsReadout __instance)
        {
            if (Mathf.Max(Find.TickManager.TicksGame, Find.TutorialState.endTick) < 600)
            {
                return false;
            }

            if (Find.Storyteller.def != null && Find.Storyteller.def.disableAlerts)
            {
                __instance.activeAlerts.Clear();
                return false;
            }

            if (TickManager_Patch.curTimeSpeed(Find.TickManager) == TimeSpeed.Ultrafast && RimThreadedMod.Settings.disablesomealerts)
            {
                //this will disable alert checks on ultrafast speed for an added speed boost
                return false; 
            }

            __instance.curAlertIndex++;
            if (__instance.curAlertIndex >= 24)
            {
                __instance.curAlertIndex = 0;
            }

            for (int i = __instance.curAlertIndex; i < __instance.AllAlerts.Count; i += 24)
            {
                //CheckAddOrRemoveAlert2(__instance, AllAlerts(__instance)[i]);
                __instance.CheckAddOrRemoveAlert(__instance.AllAlerts[i], false);
            }

            if (Time_Patch.get_frameCount() % 20 == 0)
            {
                List<Quest> questsListForReading = Find.QuestManager.QuestsListForReading;
                for (int j = 0; j < questsListForReading.Count; j++)
                {
                    List<QuestPart> partsListForReading = questsListForReading[j].PartsListForReading;
                    for (int k = 0; k < partsListForReading.Count; k++)
                    {
                        QuestPartActivable questPartActivable = partsListForReading[k] as QuestPartActivable;
                        if (questPartActivable == null)
                        {
                            continue;
                        }

                        Alert cachedAlert = questPartActivable.CachedAlert;
                        if (cachedAlert == null) continue;
                        bool flag = questsListForReading[j].State != QuestState.Ongoing || questPartActivable.State != QuestPartState.Enabled;
                        bool alertDirty = questPartActivable.AlertDirty;
                        //CheckAddOrRemoveAlert(__instance, cachedAlert, flag || alertDirty);
                        __instance.CheckAddOrRemoveAlert(cachedAlert, flag || alertDirty);
                        if (alertDirty)
                        {
                            questPartActivable.ClearCachedAlert();
                        }
                    }
                }
            }

            for (int num = __instance.activeAlerts.Count - 1; num >= 0; num--)
            {
                Alert alert = __instance.activeAlerts[num];
                try
                {
                    __instance.activeAlerts[num].AlertActiveUpdate();
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception updating alert " + alert.ToString() + ": " + ex.ToString(), 743575);
                    __instance.activeAlerts.RemoveAt(num);
                }
            }

            if (__instance.mouseoverAlertIndex >= 0 && __instance.mouseoverAlertIndex < __instance.activeAlerts.Count)
            {
                IEnumerable<GlobalTargetInfo> allCulprits = __instance.activeAlerts[__instance.mouseoverAlertIndex].GetReport().AllCulprits;
                if (allCulprits != null)
                {
                    foreach (GlobalTargetInfo item in allCulprits)
                    {
                        TargetHighlighter.Highlight(item);
                    }
                }
            }

            __instance.mouseoverAlertIndex = -1;
            return false;
        }

    }
}
