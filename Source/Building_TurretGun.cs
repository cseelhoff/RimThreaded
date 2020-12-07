using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Building_TurretGun_Patch
    {
        public static FieldRef<Building_TurretGun, CompMannable> mannableComp = 
            FieldRefAccess<Building_TurretGun, CompMannable>("mannableComp");

        public static bool IsMortar(Building_TurretGun __instance) { return __instance.def.building.IsMortar; }

        public static bool TryFindNewTarget(Building_TurretGun __instance, ref LocalTargetInfo __result)
        {
            Building_Turret building_Turret = __instance;
            IAttackTargetSearcher attackTargetSearcher = TargSearcher(__instance);
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = __instance.AttackVerb.verbProps.range;
            if (Rand.Value < 0.5f && __instance.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && 
                building_Turret.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
            {
                float num = __instance.AttackVerb.verbProps.EffectiveMinRange(x, __instance);
                float num2 = x.Position.DistanceToSquared(building_Turret.Position);
                return num2 > num * num && num2 < range * range;
            }).TryRandomElement(out Building result))
            {
                __result = result;
                return false;
            }

            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            if (!__instance.AttackVerb.ProjectileFliesOverhead())
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
            }

            if (__instance.AttackVerb.IsIncendiary())
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }

            if (IsMortar(__instance))
            {
                targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
            }

            //return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget);
            __result = null;
            return false;
        }
        public static IAttackTargetSearcher TargSearcher(Building_TurretGun __instance)
        {
            if (mannableComp(__instance) != null && mannableComp(__instance).MannedNow)
            {
                return mannableComp(__instance).ManningPawn;
            }

            return __instance;
        }

        private static bool IsValidTarget(Building_TurretGun __instance, Thing t)
        {
            Building_Turret building_Turret = __instance;
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (__instance.AttackVerb.ProjectileFliesOverhead())
                {
                    RoofDef roofDef = building_Turret.Map.roofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }

                if (mannableComp == null)
                {
                    return !GenAI.MachinesLike(building_Turret.Faction, pawn);
                }

                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
