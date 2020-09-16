using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class Bullet_Patch
	{
        public static bool Impact(Bullet __instance, Thing hitThing)
		{
            Quaternion ExactRotation = Quaternion.LookRotation((Projectile_Patch.destination(__instance) - Projectile_Patch.origin(__instance)).Yto0());
            Map map = __instance.Map;
            IntVec3 position = __instance.Position;
            Projectile_Patch.Base_Impact(__instance, hitThing);
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(Projectile_Patch.launcher(__instance), 
                hitThing, __instance.intendedTarget.Thing, Projectile_Patch.equipmentDef(__instance), __instance.def, Projectile_Patch.targetCoverDef(__instance));
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            NotifyImpact(__instance, hitThing, map, position);
            if (hitThing != null)
            {
                DamageInfo dinfo = new DamageInfo(__instance.def.projectile.damageDef, __instance.DamageAmount, __instance.ArmorPenetration,
                    ExactRotation.eulerAngles.y, Projectile_Patch.launcher(__instance), 
                    null, Projectile_Patch.equipmentDef(__instance), DamageInfo.SourceCategory.ThingOrUnknown, __instance.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn = hitThing as Pawn;
                if (pawn != null && pawn.stances != null && pawn.BodySize <= __instance.def.projectile.StoppingPower + 0.001f)
                {
                    pawn.stances.StaggerFor(95);
                }

                if (__instance.def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in __instance.def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, Projectile_Patch.launcher(__instance),
                                null, Projectile_Patch.equipmentDef(__instance), DamageInfo.SourceCategory.ThingOrUnknown, __instance.intendedTarget.Thing);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
            else
            {
                
                float StartingTicksToImpact = (Projectile_Patch.origin(__instance) - Projectile_Patch.destination(__instance)).magnitude / __instance.def.projectile.SpeedTilesPerTick;
                if (StartingTicksToImpact <= 0f)
                {
                    StartingTicksToImpact = 0.001f;
                }                    
                float DistanceCoveredFraction = Mathf.Clamp01(1f - (float)(Projectile_Patch.ticksToImpact(__instance)) / StartingTicksToImpact);
                Vector3 b = (Projectile_Patch.destination(__instance) - Projectile_Patch.origin(__instance)).Yto0() * DistanceCoveredFraction;
                Vector3 ExactPosition = Projectile_Patch.origin(__instance).Yto0() + b + Vector3.up * __instance.def.Altitude;

                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(__instance.Position, map));
                if (__instance.Position.GetTerrain(map).takeSplashes)
                {
                    MoteMaker.MakeWaterSplash(ExactPosition, map, Mathf.Sqrt(__instance.DamageAmount) * 1f, 4f);
                }
                else
                {
                    MoteMaker.MakeStaticMote(ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt);
                }
            }
            return false;
		}
        public static bool NotifyImpact(Bullet __instance, Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData bulletImpactData = default(BulletImpactData);
            bulletImpactData.bullet = __instance;
            bulletImpactData.hitThing = hitThing;
            bulletImpactData.impactPosition = position;
            BulletImpactData impactData = bulletImpactData;
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
            return false;
        }
    }
}
