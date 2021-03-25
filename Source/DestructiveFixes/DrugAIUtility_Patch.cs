using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class DrugAIUtility_Patch
    {
        public static FieldRef<DrugPolicy, List<DrugPolicyEntry>> entriesInt = FieldRefAccess<DrugPolicy, List<DrugPolicyEntry>>("entriesInt");
        public static bool IngestAndTakeToInventoryJob(ref Job __result, Thing drug, Pawn pawn, int maxNumToCarry = 9999)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Ingest, drug);
            job.count = Mathf.Min(drug.stackCount, drug.def.ingestible.maxNumToIngestAtOnce, maxNumToCarry);
            if (pawn.drugs != null && drugPolicyExists(entriesInt(pawn.drugs.CurrentPolicy), drug.def))
            {
                DrugPolicyEntry drugPolicyEntry = pawn.drugs.CurrentPolicy[drug.def];
                int num = pawn.inventory.innerContainer.TotalStackCountOfDef(drug.def) - job.count;
                if (drugPolicyEntry.allowScheduled && num <= 0)
                {
                    job.takeExtraIngestibles = drugPolicyEntry.takeToInventory;
                }
            }

            __result = job;
            return false;
        }

        private static bool drugPolicyExists(List<DrugPolicyEntry> entriesInt, ThingDef def)
        {
            for (int index = 0; index < entriesInt.Count; index++)
            {
                if (entriesInt[index].drug == def)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
