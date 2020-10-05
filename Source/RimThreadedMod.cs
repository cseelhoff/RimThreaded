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
							if (patch.owner.Equals("majorhoff.rimthreaded") && (patches.Prefixes.Count > 1 || patches.Postfixes.Count > 0 || patches.Transpilers.Count > 0))
							{
								isRimThreadedPrefixed = true;
								Settings.modsText += "\n---Patch method: " + patch.PatchMethod + "---\n";
								Settings.modsText += "RimThreaded priority: " + patch.priority + "\n";
								break;
							}
						}
						if (isRimThreadedPrefixed)
						{
							foreach (Patch patch in patches.Prefixes)
							{
								if (!patch.owner.Equals("majorhoff.rimthreaded"))
								{
									Settings.modsText += "owner: " + patch.owner + " - ";
									Settings.modsText += "priority: " + patch.priority + "\n";
								}
							}
							foreach (Patch patch in patches.Postfixes)
							{
								Settings.modsText += "owner: " + patch.owner + " - ";
								Settings.modsText += "priority: " + patch.priority + "\n";
							}
							foreach (Patch patch in patches.Transpilers)
							{
								Settings.modsText += "owner: " + patch.owner + " - ";
								Settings.modsText += "priority: " + patch.priority + "\n";
							}
						}
					}
				}
			}			
			Settings.DoWindowContents(inRect);
			if (Settings.maxThreads != RimThreaded.maxThreads)
			{
				RimThreaded.maxThreads = Math.Max(Settings.maxThreads, 1);
				RimThreaded.RestartAllWorkerThreads();
			}
			RimThreaded.timeoutMS = Math.Max(Settings.timeoutMS, 1);
		}

		public override string SettingsCategory()
		{
			return "RimThreaded";

		}

	}
	
}

