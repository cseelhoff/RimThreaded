using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using RimWorld.Planet;
using System.IO;

namespace RimThreaded 
{ 
    class RimThreadedMod : Mod
    {
        public static RimThreadedSettings Settings;
        public RimThreadedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimThreadedSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings.modsText.Length == 0)
            {
                Settings.modsText = "Potential RimThreaded mod conflicts :\n";
                Settings.modsText += getPotentialModConflicts();

                //string path = "hmodText.txt";
                //StreamWriter writer = new StreamWriter(path, true);
                //writer.WriteLine(Settings.modsText);
                //writer.Close();
            }
            Settings.DoWindowContents(inRect);
            if (Settings.maxThreads != RimThreaded.maxThreads)
            {
                RimThreaded.maxThreads = Settings.disablelimits ? Math.Max(Settings.maxThreads, 1) : Math.Min(Math.Max(Settings.maxThreads, 1), 128);
                RimThreaded.RestartAllWorkerThreads();
            }
            RimThreaded.timeoutMS = Settings.disablelimits ? Math.Max(Settings.timeoutMS, 1) : Math.Min(Math.Max(Settings.timeoutMS, 5000), 100000);
            RimThreaded.timeSpeedNormal = Settings.timeSpeedNormal;
            RimThreaded.timeSpeedFast = Settings.timeSpeedFast;
            RimThreaded.timeSpeedSuperfast = Settings.timeSpeedSuperfast;
            RimThreaded.timeSpeedUltrafast = Settings.timeSpeedUltrafast;
        }

        public static string getPotentialModConflicts()
        {
            string modsText = "";
            IEnumerable<MethodBase> originalMethods = Harmony.GetAllPatchedMethods();
            foreach (MethodBase originalMethod in originalMethods)
            {
                Patches patches = Harmony.GetPatchInfo(originalMethod);
                if (patches is null) { }
                else
                {
                    bool isRimThreadedPrefixed = false;
                    foreach (Patch patch in patches.Prefixes)
                    {

                        if (patch.owner.Equals("majorhoff.rimthreaded") && !RimThreadedHarmony.nonDestructivePrefixes.Contains(patch.PatchMethod) && (patches.Prefixes.Count > 1 || patches.Postfixes.Count > 0 || patches.Transpilers.Count > 0))
                        {
                            isRimThreadedPrefixed = true;
                            modsText += "\n  ---Patch method: " + patch.PatchMethod.DeclaringType?.FullName + " " + patch.PatchMethod + "---\n";
                            modsText += "  RimThreaded priority: " + patch.priority + "\n";
                            break;
                        }
                    }
                    if (isRimThreadedPrefixed)
                    {
                        foreach (Patch patch in patches.Prefixes)
                        {
                            if (!patch.owner.Equals("majorhoff.rimthreaded"))
                            {
                                //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                                modsText += "  owner: " + patch.owner + " - ";
                                modsText += "  FullName: " + patch.PatchMethod.DeclaringType?.FullName + "\n";
                            }
                        }
                        foreach (Patch patch in patches.Postfixes)
                        {
                            //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                            modsText += "  owner: " + patch.owner + " - ";
                            modsText += "  FullName: " + patch.PatchMethod.DeclaringType?.FullName + "\n";
                        }
                        foreach (Patch patch in patches.Transpilers)
                        {
                            //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                            modsText += "  owner: " + patch.owner + " - ";
                            modsText += "  FullName: " + patch.PatchMethod.DeclaringType?.FullName + "\n";
                        }
                    }
                }
            }
            return modsText;
        }

        public static void getPotentialModConflicts_2()
        {
            bool WrongLoadOrder = false;
            bool Conflictingmods = false;
            string ModConflictsMessage = "";
            string[] IncompatibleMods = { "biomesteam.biomesislands", "mlie.bestmix", "rwmt.Multiplayer", "pyrce.terrain.movement.modkit" }; //Add/Remove mods here.
            string NewLine = "_______________________" + "\n" + "\n";
            var LoadOrder = LoadedModManager.RunningModsListForReading;
            int RTpos = LoadOrder.FindIndex(i => i.PackageId == "majorhoff.rimthreaded");

            if (RTpos != (LoadOrder.Count - 1))
            {
                WrongLoadOrder = true;
                ModConflictsMessage = NewLine + "Critical incompatibility:" + "\n" + NewLine + "RimThreaded is NOT last in your mod load order, fix immediately." + "\n";
            }
            ModConflictsMessage += NewLine + "Highly incompatible:" + "\n" + NewLine;
            for (int i = 0; i < LoadOrder.Count; i++)
            {
                if (IncompatibleMods.Contains(LoadOrder[i].PackageId))
                {
                    ModConflictsMessage += LoadOrder[i].Name + "\n";
                    Conflictingmods = true;
                }
            }
            if (!Conflictingmods) ModConflictsMessage += "No Conflicts detected :D" + "\n";

            ModConflictsMessage += NewLine + "Other (potential) incompatibilities:" + "\n" + NewLine + "Check out the wiki on github for more information" + "\n" + "_______________________";
            Dialog_MessageBox window2 = new Dialog_MessageBox(ModConflictsMessage, "Ill take my chances", null, "Disable this alert in settings", null, "RimThreaded Mod Conflicts detected:", true);
            if (WrongLoadOrder || Conflictingmods) Find.WindowStack.Add(window2);
        }

        public override string SettingsCategory()
        {
            return "RimThreaded";

        }

    }

}

