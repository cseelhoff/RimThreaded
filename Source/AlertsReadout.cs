using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class AlertsReadout_Patch
    {
        public static FieldRef<AlertsReadout, List<Alert>> activeAlerts =
            FieldRefAccess<AlertsReadout, List<Alert>>("activeAlerts");
        public static FieldRef<AlertsReadout, int> curAlertIndex =
            FieldRefAccess<AlertsReadout, int>("curAlertIndex");
        public static FieldRef<AlertsReadout, List<Alert>> AllAlerts =
            FieldRefAccess<AlertsReadout, List<Alert>>("AllAlerts");
        public static FieldRef<AlertsReadout, int> mouseoverAlertIndex =
            FieldRefAccess<AlertsReadout, int>("mouseoverAlertIndex");

        public static bool AlertsReadoutUpdate(AlertsReadout __instance)
        {
            if (Mathf.Max(Find.TickManager.TicksGame, Find.TutorialState.endTick) < 600)
            {
                return false;
            }

            if (Find.Storyteller.def != null && Find.Storyteller.def.disableAlerts)
            {
                activeAlerts(__instance).Clear();
                return false;
            }

            if (TickManager_Patch.curTimeSpeed(Find.TickManager) != TimeSpeed.Ultrafast)
            {
                //Uncommenting this will disable alert checks on ultrafast speed for an added speed boost
                //return false;
            }

                curAlertIndex(__instance)++;
            if (curAlertIndex(__instance) >= 24)
            {
                curAlertIndex(__instance) = 0;
            }

            for (int i = curAlertIndex(__instance); i < AllAlerts(__instance).Count; i += 24)
            {
                CheckAddOrRemoveAlert2(__instance, AllAlerts(__instance)[i]);
            }

            if (Time.frameCount % 20 == 0)
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
                        if (cachedAlert != null)
                        {
                            bool flag = questsListForReading[j].State != QuestState.Ongoing || questPartActivable.State != QuestPartState.Enabled;
                            bool alertDirty = questPartActivable.AlertDirty;
                            CheckAddOrRemoveAlert2(__instance, cachedAlert, flag || alertDirty);
                            if (alertDirty)
                            {
                                questPartActivable.ClearCachedAlert();
                            }
                        }
                    }
                }
            }

            for (int num = activeAlerts(__instance).Count - 1; num >= 0; num--)
            {
                Alert alert = activeAlerts(__instance)[num];
                try
                {
                    activeAlerts(__instance)[num].AlertActiveUpdate();
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception updating alert " + alert.ToString() + ": " + ex.ToString(), 743575);
                    activeAlerts(__instance).RemoveAt(num);
                }
            }

            if (mouseoverAlertIndex(__instance) >= 0 && mouseoverAlertIndex(__instance) < activeAlerts(__instance).Count)
            {
                IEnumerable<GlobalTargetInfo> allCulprits = activeAlerts(__instance)[mouseoverAlertIndex(__instance)].GetReport().AllCulprits;
                if (allCulprits != null)
                {
                    foreach (GlobalTargetInfo item in allCulprits)
                    {
                        TargetHighlighter.Highlight(item);
                    }
                }
            }

            mouseoverAlertIndex(__instance) = -1;
            return false;
        }
        public static void CheckAddOrRemoveAlert2(AlertsReadout __instance, Alert alert, bool forceRemove = false)
        {
            try
            {
                alert.Recalculate();                
                if (!forceRemove && alert.Active)
                {
                    if (!activeAlerts(__instance).Contains(alert))
                    {
                        activeAlerts(__instance).Add(alert);
                        alert.Notify_Started();
                    }
                }
                else
                {
                    activeAlerts(__instance).Remove(alert);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("Exception processing alert " + alert.ToString() + ": " + ex.ToString(), 743575);
                activeAlerts(__instance).Remove(alert);
            }
        }


    }
}
