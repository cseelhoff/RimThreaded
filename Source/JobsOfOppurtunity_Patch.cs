using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
	class JobsOfOppurtunity_Patch
	{

		public static Type jobsOfOpportunityJobsOfOpportunity_Hauling;
		public static Type jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob;

		public static void Patch()
		{
			jobsOfOpportunityJobsOfOpportunity_Hauling = TypeByName("JobsOfOpportunity.JobsOfOpportunity+Hauling");
			jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob = TypeByName("JobsOfOpportunity.JobsOfOpportunity+Patch_TryOpportunisticJob");

			Type patched;
			if (jobsOfOpportunityJobsOfOpportunity_Hauling != null)
			{
				cachedStoreCell = Field(jobsOfOpportunityJobsOfOpportunity_Hauling, "cachedStoreCell");
				string methodName = "CanHaul";
				patched = typeof(Hauling_Transpile);
				Log.Message("RimThreaded is patching " + jobsOfOpportunityJobsOfOpportunity_Hauling.FullName + " " + methodName);
				Transpile(jobsOfOpportunityJobsOfOpportunity_Hauling, patched, methodName);
			}

			if (jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob != null)
			{
				string methodName = "TryOpportunisticJob";
				patched = typeof(Patch_TryOpportunisticJob_Transpile);
				Log.Message("RimThreaded is patching " + jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob.FullName + " " + methodName);
				Transpile(jobsOfOpportunityJobsOfOpportunity_Patch_TryOpportunisticJob, patched, methodName);
			}

		}
	}
}
