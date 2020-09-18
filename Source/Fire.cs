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

    public class Fire_Patch
    {
        public static int lastFireCountUpdateTick =
            AccessTools.StaticFieldRefAccess<int>(typeof(Fire), "lastFireCountUpdateTick");
        public static int fireCount =
            AccessTools.StaticFieldRefAccess<int>(typeof(Fire), "fireCount");
        public static IntRange SmokeIntervalRange =
            AccessTools.StaticFieldRefAccess<IntRange>(typeof(Fire), "SmokeIntervalRange");
        //public static List<Thing> flammableList =
            //AccessTools.StaticFieldRefAccess<List<Thing>>(typeof(Fire), "flammableList");

        public static AccessTools.FieldRef<Fire, float> flammabilityMax =
            AccessTools.FieldRefAccess<Fire, float>("flammabilityMax");
        public static AccessTools.FieldRef<Fire, int> ticksUntilSmoke =
            AccessTools.FieldRefAccess<Fire, int>("ticksUntilSmoke");
        public static AccessTools.FieldRef<Fire, int> ticksSinceSpawn =
            AccessTools.FieldRefAccess<Fire, int>("ticksSinceSpawn");
        public static AccessTools.FieldRef<Fire, int> ticksSinceSpread =
            AccessTools.FieldRefAccess<Fire, int>("ticksSinceSpread");
        public static AccessTools.FieldRef<Fire, Sustainer> sustainer =
            AccessTools.FieldRefAccess<Fire, Sustainer>("sustainer");

        private static float get_SpreadInterval(Fire __instance)
        {
            float num = (float)(150.0 - ((double)__instance.fireSize - 1.0) * 40.0);
            if ((double)num < 75.0)
                num = 75f;
            return num;
        }

        private static void SpawnSmokeParticles(Fire __instance)
        {
            if (fireCount < 15)
                MoteMaker.ThrowSmoke(__instance.DrawPos, __instance.Map, __instance.fireSize);
            if ((double)__instance.fireSize > 0.5 && __instance.parent == null)
                MoteMaker.ThrowFireGlow(__instance.Position, __instance.Map, __instance.fireSize);
            float num = __instance.fireSize / 2f;
            if ((double)num > 1.0)
                num = 1f;
            float lerpFactor = 1f - num;
            ticksUntilSmoke(__instance) = SmokeIntervalRange.Lerped(lerpFactor) + (int)(10.0 * (double)Rand.Value);
        }

        public static void TrySpread(Fire __instance)
        {
            IntVec3 position = __instance.Position;
            IntVec3 intVec3;
            bool flag;
            if (Rand.Chance(0.8f))
            {
                intVec3 = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
                flag = true;
            }
            else
            {
                intVec3 = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
                flag = false;
            }
            if (!intVec3.InBounds(__instance.Map) || !Rand.Chance(FireUtility.ChanceToStartFireIn(intVec3, __instance.Map)))
                return;
            if (!flag)
            {
                CellRect startRect = CellRect.SingleCell(__instance.Position);
                CellRect endRect = CellRect.SingleCell(intVec3);
                if (!GenSight.LineOfSight(__instance.Position, intVec3, __instance.Map, startRect, endRect, (Func<IntVec3, bool>)null))
                    return;
                ((Projectile)GenSpawn.Spawn(ThingDefOf.Spark, __instance.Position, __instance.Map, WipeMode.Vanish)).Launch((Thing)__instance, (LocalTargetInfo)intVec3, (LocalTargetInfo)intVec3, ProjectileHitFlags.All, (Thing)null);
            }
            else
                FireUtility.TryStartFireIn(intVec3, __instance.Map, 0.1f);
        }
        public static bool Tick(Fire __instance)
        {
            Map map = __instance.Map;
            if (null != map)
            {
                ++ticksSinceSpawn(__instance);
                if (lastFireCountUpdateTick != Find.TickManager.TicksGame)
                {
                    fireCount = __instance.Map.listerThings.ThingsOfDef(__instance.def).Count;
                    lastFireCountUpdateTick = Find.TickManager.TicksGame;
                }
                if (sustainer(__instance) != null)
                    sustainer(__instance).Maintain();
                else if (!__instance.Position.Fogged(__instance.Map))
                {
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(__instance.Position, __instance.Map, false), MaintenanceType.PerTick);
                    sustainer(__instance) = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor((ISizeReporter)__instance, SoundDefOf.FireBurning, info);
                }
                --ticksUntilSmoke(__instance);
                if (ticksUntilSmoke(__instance) <= 0)
                    SpawnSmokeParticles(__instance);
                if (fireCount < 15 && (double)__instance.fireSize > 0.699999988079071 && (double)Rand.Value < (double)__instance.fireSize * 0.00999999977648258)
                    MoteMaker.ThrowMicroSparks(__instance.DrawPos, __instance.Map);
                if ((double)__instance.fireSize > 1.0)
                {
                    ++ticksSinceSpread(__instance);
                    if ((double)ticksSinceSpread(__instance) >= (double)get_SpreadInterval(__instance))
                    {
                        TrySpread(__instance);
                        ticksSinceSpread(__instance) = 0;
                    }
                }
                if (__instance.IsHashIntervalTick(150))
                    DoComplexCalcs(__instance);
                if (ticksSinceSpawn(__instance) < 7500)
                    return false;
                TryBurnFloor(__instance);
            }
            return false;
        }

        public static bool DoComplexCalcs(Fire __instance)
        {
            bool flag = false;
            //flammableList.Clear();
            List<Thing> flammableList = new List<Thing>();
            flammabilityMax(__instance) = 0.0f;
            if (null != __instance.Map)
            {
                if (!__instance.Position.GetTerrain(__instance.Map).extinguishesFire)
                {
                    if (__instance.parent == null)
                    {
                        if (__instance.Position.TerrainFlammableNow(__instance.Map))
                            flammabilityMax(__instance) = __instance.Position.GetTerrain(__instance.Map).GetStatValueAbstract(StatDefOf.Flammability, (ThingDef)null);
                        List<Thing> thingList = __instance.Map.thingGrid.ThingsListAt(__instance.Position);
                        for (int index = 0; index < thingList.Count; ++index)
                        {
                            Thing thing = thingList[index];
                            if (thing is Building_Door)
                                flag = true;
                            float statValue = thing.GetStatValue(StatDefOf.Flammability, true);
                            if ((double)statValue >= 0.00999999977648258)
                            {
                                flammableList.Add(thingList[index]);
                                if ((double)statValue > (double)flammabilityMax(__instance))
                                    flammabilityMax(__instance) = statValue;
                                if (__instance.parent == null && (double)__instance.fireSize > 0.400000005960464 && (thingList[index].def.category == ThingCategory.Pawn && Rand.Chance(FireUtility.ChanceToAttachFireCumulative(thingList[index], 150f))))
                                    thingList[index].TryAttachFire(__instance.fireSize * 0.2f);
                            }
                        }
                    }
                    else
                    {
                        flammableList.Add(__instance.parent);
                        flammabilityMax(__instance) = __instance.parent.GetStatValue(StatDefOf.Flammability, true);
                    }
                }
                if ((double)flammabilityMax(__instance) < 0.00999999977648258)
                {
                    __instance.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Thing targ = __instance.parent == null ? (flammableList.Count <= 0 ? (Thing)null : flammableList[Rand.Range(0, flammableList.Count)]) : __instance.parent;
                    if (targ != null && ((double)__instance.fireSize >= 0.400000005960464 || targ == __instance.parent || targ.def.category != ThingCategory.Pawn))
                    {
                        DoFireDamage(__instance, targ);
                    }
                    if (!__instance.Spawned)
                        return false;
                    float energy = __instance.fireSize * 160f;
                    if (flag)
                        energy *= 0.15f;
                    GenTemperature.PushHeat(__instance.Position, __instance.Map, energy);
                    if ((double)Rand.Value < 0.400000005960464)
                        SnowUtility.AddSnowRadial(__instance.Position, __instance.Map, __instance.fireSize * 3f, (float)-((double)__instance.fireSize * 0.100000001490116));
                    __instance.fireSize += (float)(0.000549999997019768 * (double)flammabilityMax(__instance) * 150.0);
                    if ((double)__instance.fireSize > 1.75)
                        __instance.fireSize = 1.75f;
                    if ((double)__instance.Map.weatherManager.RainRate <= 0.00999999977648258 || !VulnerableToRain(__instance) || (double)Rand.Value >= 6.0)
                        return false;
                    //__instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f, 0.0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
                    //DamageInfo daminfo = new DamageInfo(DamageDefOf.Extinguish, 10f, 0.0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null);
                    __instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f, 0.0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
                    //DamageWorker.DamageResult dresult = new DamageWorker.DamageResult();
                    //Thing_Patch.TakeDamage(__instance, ref dresult, daminfo);
                }

            }
            return false;
        }

            private static bool VulnerableToRain(Fire __instance)
        {
            if (!__instance.Spawned)
                return false;
            RoofDef roofDef = __instance.Map.roofGrid.RoofAt(__instance.Position);
            if (roofDef == null)
                return true;
            if (roofDef.isThickRoof)
                return false;
            Thing edifice = (Thing)__instance.Position.GetEdifice(__instance.Map);
            return edifice != null && edifice.def.holdsRoof;
        }

        public static void DoFireDamage(Fire __instance, Thing targ)
        {
            int num = GenMath.RoundRandom(Mathf.Clamp((float)(0.0125000001862645 + 0.00359999993816018 * (double)__instance.fireSize), 0.0125f, 0.05f) * 150f);
            if (num < 1)
                num = 1;
            if (targ is Pawn recipient)
            {
                
                BattleLogEntry_DamageTaken entryDamageTaken = new BattleLogEntry_DamageTaken(recipient, RulePackDefOf.DamageEvent_Fire, (Pawn)null);
                Find.BattleLog.Add((LogEntry)entryDamageTaken);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, (float)num, 0.0f, -1f, (Thing)__instance, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null);
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                targ.TakeDamage(dinfo).AssociateWithLog((LogEntry_DamageResult)entryDamageTaken);
                Apparel result;
                if (recipient.apparel == null || !recipient.apparel.WornApparel.TryRandomElement<Apparel>(out result))
                    return;
                DamageWorker.DamageResult dresult = new DamageWorker.DamageResult();
                //result.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 0.0f, -1f, (Thing)__instance, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
                Thing_Patch.TakeDamage(result, ref dresult, new DamageInfo(DamageDefOf.Flame, (float)num, 0.0f, -1f, (Thing)__instance, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
            }
            else
            {
                DamageWorker.DamageResult dresult = new DamageWorker.DamageResult();
                //targ.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 0.0f, -1f, (Thing)__instance, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
                Thing_Patch.TakeDamage(targ, ref dresult, new DamageInfo(DamageDefOf.Flame, (float)num, 0.0f, -1f, (Thing)__instance, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
            }
        }

            private static void TryBurnFloor(Fire __instance)
        {
            if (__instance.parent != null || !__instance.Spawned || !__instance.Position.TerrainFlammableNow(__instance.Map))
                return;
            __instance.Map.terrainGrid.Notify_TerrainBurned(__instance.Position);
        }

    }
}
