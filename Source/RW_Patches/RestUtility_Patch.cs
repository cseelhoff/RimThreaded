using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    class RestUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RestUtility);
            Type patched = typeof(RestUtility_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(CurrentBed), null, false);
            RimThreadedHarmony.Prefix(original, patched, nameof(FindBedFor), new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus) });
        }
        public static bool CurrentBed(Pawn __instance, ref Building_Bed __result)
        {
            if (__instance == null)
            {
                __result = null;
                return false;
            }
            return true;
        }


        public static bool FindBedFor(ref Building_Bed __result, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            bool flag = false;
            if (sleeper.Ideo != null)
            {
                foreach (Precept item in sleeper.Ideo.PreceptsListForReading)
                {
                    if (item.def.prefersSlabBed)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            List<ThingDef> list = flag ? RestUtility.bedDefsBestToWorst_SlabBed_Medical : RestUtility.bedDefsBestToWorst_Medical;
            List<ThingDef> list2 = flag ? RestUtility.bedDefsBestToWorst_SlabBed_RestEffectiveness : RestUtility.bedDefsBestToWorst_RestEffectiveness;

            if (HealthAIUtility.ShouldSeekMedicalRest(sleeper))
            {
                if (sleeper.InBed() && sleeper.CurrentBed().Medical && RestUtility.IsValidBedFor(sleeper.CurrentBed(), sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
                {
                    __result = sleeper.CurrentBed();
                    return false;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef thingDef = list[i];
                    if (!RestUtility.CanUseBedEver(sleeper, thingDef))
                    {
                        continue;
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        Danger maxDanger2 = j == 0 ? Danger.None : Danger.Deadly;
                        Building_Bed building_Bed = (Building_Bed)GenClosest_Patch.ClosestBedReachable(sleeper.Position, sleeper.Map, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, (b) => ((Building_Bed)b).Medical && (int)b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int)maxDanger2 && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus));
                        if (building_Bed != null)
                        {
                            __result = building_Bed;
                            return false;
                        }
                    }
                }
            }
            if (sleeper.RaceProps.Dryad)
            {
                __result = null;
                return false;
            }
            if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null && RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
            {
                __result = sleeper.ownership.OwnedBed;
                return false;
            }
            DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(sleeper, allowDead: false);

            if (directPawnRelation != null)
            {
                Building_Bed ownedBed = directPawnRelation.otherPawn.ownership.OwnedBed;
                if (ownedBed != null && RestUtility.IsValidBedFor(ownedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
                {
                    __result = ownedBed;
                    return false;
                }
            }
            for (int k = 0; k < 2; k++)
            {
                Danger maxDanger = k == 0 ? Danger.None : Danger.Deadly;
                for (int l = 0; l < list2.Count; l++)
                {
                    ThingDef thingDef2 = list2[l];
                    if (RestUtility.CanUseBedEver(sleeper, thingDef2))
                    {
                        Building_Bed building_Bed2 = (Building_Bed)GenClosest_Patch.ClosestBedReachable(sleeper.Position, sleeper.Map, ThingRequest.ForDef(thingDef2), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, (b) => !((Building_Bed)b).Medical && (int)b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int)maxDanger && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus));
                        if (building_Bed2 != null)
                        {
                            __result = building_Bed2;
                            return false;
                        }
                    }
                }
            }
            __result = null;
            return false;
        }



    }
}
