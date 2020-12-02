using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.DamageWorker;

namespace RimThreaded
{
    class DamageWorker_Patch
    {
        public static bool ExplosionAffectCell(DamageWorker __instance, Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
        {
            if (__instance.def.explosionCellMote != null && canThrowMotes)
            {
                Mote mote = c.GetFirstThing(explosion.Map, __instance.def.explosionCellMote) as Mote;
                if (mote != null)
                {
                    mote.spawnTick = Find.TickManager.TicksGame;
                }
                else
                {
                    float t = Mathf.Clamp01((explosion.Position - c).LengthHorizontal / explosion.radius);
                    Color color = Color.Lerp(__instance.def.explosionColorCenter, __instance.def.explosionColorEdge, t);
                    MoteMaker.ThrowExplosionCell(c, explosion.Map, __instance.def.explosionCellMote, color);
                }
            }

            //thingsToAffect.Clear();
            List<Thing> thingsToAffect = new List<Thing>();
            float num = float.MinValue;
            bool flag = false;
            List<Thing> list = explosion.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.category != ThingCategory.Mote && thing.def.category != ThingCategory.Ethereal)
                {
                    thingsToAffect.Add(thing);
                    if (thing.def.Fillage == FillCategory.Full && thing.def.Altitude > num)
                    {
                        flag = true;
                        num = thing.def.Altitude;
                    }
                }
            }

            for (int j = 0; j < thingsToAffect.Count; j++)
            {
                if (thingsToAffect[j].def.Altitude >= num) //Null Reference Exception
                {
                    ExplosionDamageThing(__instance, explosion, thingsToAffect[j], damagedThings, ignoredThings, c);
                }
            }

            if (!flag)
            {
                ExplosionDamageTerrain(__instance, explosion, c);
            }

            if (__instance.def.explosionSnowMeltAmount > 0.0001f)
            {
                float lengthHorizontal = (c - explosion.Position).LengthHorizontal;
                float num2 = 1f - lengthHorizontal / explosion.radius;
                if (num2 > 0f)
                {
                    explosion.Map.snowGrid.AddDepth(c, (0f - num2) * __instance.def.explosionSnowMeltAmount);
                }
            }

            if (__instance.def != DamageDefOf.Bomb && __instance.def != DamageDefOf.Flame)
            {
                return false;
            }

            List<Thing> list2 = explosion.Map.listerThings.ThingsOfDef(ThingDefOf.RectTrigger);
            for (int k = 0; k < list2.Count; k++)
            {
                RectTrigger rectTrigger = (RectTrigger)list2[k];
                if (rectTrigger.activateOnExplosion && rectTrigger.Rect.Contains(c))
                {
                    rectTrigger.ActivatedBy(null);
                }
            }
            return false;
        }

        public static void ExplosionDamageThing(DamageWorker __instance, Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
        {
            if (t.def.category == ThingCategory.Mote || t.def.category == ThingCategory.Ethereal || damagedThings.Contains(t))
            {
                return;
            }

            damagedThings.Add(t);
            if (ignoredThings != null && ignoredThings.Contains(t))
            {
                return;
            }

            if (__instance.def == DamageDefOf.Bomb && t.def == ThingDefOf.Fire && !t.Destroyed)
            {
                t.Destroy();
                return;
            }

            DamageInfo dinfo = new DamageInfo(angle: (!(t.Position == explosion.Position)) ? (t.Position - explosion.Position).AngleFlat : ((float)Rand.RangeInclusive(0, 359)), def: __instance.def, amount: explosion.GetDamageAmountAt(cell), armorPenetration: explosion.GetArmorPenetrationAt(cell), instigator: explosion.instigator, hitPart: null, weapon: explosion.weapon, category: DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget: explosion.intendedTarget);
            if (__instance.def.explosionAffectOutsidePartsOnly)
            {
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            }

            BattleLogEntry_ExplosionImpact battleLogEntry_ExplosionImpact = null;
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                battleLogEntry_ExplosionImpact = new BattleLogEntry_ExplosionImpact(explosion.instigator, t, explosion.weapon, explosion.projectile, __instance.def);
                Find.BattleLog.Add(battleLogEntry_ExplosionImpact);
            }

            DamageResult damageResult = t.TakeDamage(dinfo);
            damageResult.AssociateWithLog(battleLogEntry_ExplosionImpact);
            if (pawn != null && damageResult.wounded && pawn.stances != null)
            {
                pawn.stances.StaggerFor(95);
            }
        }
        public static void ExplosionDamageTerrain(DamageWorker __instance, Explosion explosion, IntVec3 c)
        {
            if (__instance.def == DamageDefOf.Bomb && explosion.Map.terrainGrid.CanRemoveTopLayerAt(c))
            {
                TerrainDef terrain = c.GetTerrain(explosion.Map);
                if (!(terrain.destroyOnBombDamageThreshold < 0f) && (float)explosion.GetDamageAmountAt(c) >= terrain.destroyOnBombDamageThreshold)
                {
                    explosion.Map.terrainGrid.Notify_TerrainDestroyed(c);
                }
            }
        }



    }
}