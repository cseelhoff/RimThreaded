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

    public class Pawn_MeleeVerbs_Patch
    {
        public static AccessTools.FieldRef<Pawn_MeleeVerbs, Pawn> pawn =
            AccessTools.FieldRefAccess<Pawn_MeleeVerbs, Pawn>("pawn");
        public static AccessTools.FieldRef<Pawn_MeleeVerbs, Pawn_MeleeVerbs_TerrainSource> terrainVerbs =
            AccessTools.FieldRefAccess<Pawn_MeleeVerbs, Pawn_MeleeVerbs_TerrainSource>("terrainVerbs");

        public static bool GetUpdatedAvailableVerbsList(Pawn_MeleeVerbs __instance, ref List<VerbEntry> __result, bool terrainTools)
        {
            Pawn this_pawn = pawn(__instance);
            //meleeVerbs.Clear();
            //verbsToAdd.Clear();
            List<VerbEntry> meleeVerbs = new List<VerbEntry>();
            List<Verb> verbsToAdd = new List<Verb>();

            if (!terrainTools)
            {
                List<Verb> allVerbs1 = this_pawn.verbTracker.AllVerbs;
                for (int index = 0; index < allVerbs1.Count; ++index)
                {
                    if (IsUsableMeleeVerb(allVerbs1[index]))
                        verbsToAdd.Add(allVerbs1[index]);
                }
                if (this_pawn.equipment != null)
                {
                    List<ThingWithComps> equipmentListForReading = this_pawn.equipment.AllEquipmentListForReading;
                    for (int index1 = 0; index1 < equipmentListForReading.Count; ++index1)
                    {
                        CompEquippable comp = equipmentListForReading[index1].GetComp<CompEquippable>();
                        if (comp != null)
                        {
                            List<Verb> allVerbs2 = comp.AllVerbs;
                            if (allVerbs2 != null)
                            {
                                for (int index2 = 0; index2 < allVerbs2.Count; ++index2)
                                {
                                    if (IsUsableMeleeVerb(allVerbs2[index2]))
                                        verbsToAdd.Add(allVerbs2[index2]);
                                }
                            }
                        }
                    }
                }
                if (this_pawn.apparel != null)
                {
                    List<Apparel> wornApparel = this_pawn.apparel.WornApparel;
                    for (int index1 = 0; index1 < wornApparel.Count; ++index1)
                    {
                        CompEquippable comp = wornApparel[index1].GetComp<CompEquippable>();
                        if (comp != null)
                        {
                            List<Verb> allVerbs2 = comp.AllVerbs;
                            if (allVerbs2 != null)
                            {
                                for (int index2 = 0; index2 < allVerbs2.Count; ++index2)
                                {
                                    if (IsUsableMeleeVerb(allVerbs2[index2]))
                                        verbsToAdd.Add(allVerbs2[index2]);
                                }
                            }
                        }
                    }
                }
                foreach (Verb hediffsVerb in this_pawn.health.hediffSet.GetHediffsVerbs())
                {
                    if (IsUsableMeleeVerb(hediffsVerb))
                        verbsToAdd.Add(hediffsVerb);
                }
            }
            else if (this_pawn.Spawned)
            {
                TerrainDef terrain = this_pawn.Position.GetTerrain(this_pawn.Map);
                if (terrainVerbs(__instance) == null || terrainVerbs(__instance).def != terrain)
                    terrainVerbs(__instance) = Pawn_MeleeVerbs_TerrainSource.Create(__instance, terrain);
                List<Verb> allVerbs = terrainVerbs(__instance).tracker.AllVerbs;
                for (int index = 0; index < allVerbs.Count; ++index)
                {
                    Verb v = allVerbs[index];
                    if (IsUsableMeleeVerb(v))
                        verbsToAdd.Add(v);
                }
            }
            float highestSelWeight = 0.0f;
            foreach (Verb v in verbsToAdd)
            {
                float num = VerbUtility.InitialVerbWeight(v, this_pawn);
                if ((double)num > (double)highestSelWeight)
                    highestSelWeight = num;
            }
            foreach (Verb verb in verbsToAdd)
                meleeVerbs.Add(new VerbEntry(verb, this_pawn, verbsToAdd, highestSelWeight));
            __result = meleeVerbs;
            return false;

            bool IsUsableMeleeVerb(Verb v)
            {
                return v.IsStillUsableBy(this_pawn) && v.IsMeleeAttack;
            }
        }


    }
}
