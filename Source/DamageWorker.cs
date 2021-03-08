using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.DamageWorker;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class DamageWorker_Patch
    {
        [ThreadStatic]
        private static List<Thing> thingsToAffect;

        [ThreadStatic]
        private static List<IntVec3> openCells;

        [ThreadStatic]
        private static List<IntVec3> adjWallCells;

        private static readonly MethodInfo methodExplosionDamageThing =
            Method(typeof(DamageWorker), "ExplosionDamageThing", new Type[] { typeof(Explosion), typeof(Thing), typeof(List<Thing>), typeof(List<Thing>), typeof(IntVec3) });
        private static readonly Action<DamageWorker, Explosion, Thing, List<Thing>, List<Thing>, IntVec3> actionExplosionDamageThing =
            (Action<DamageWorker, Explosion, Thing, List<Thing>, List<Thing>, IntVec3>)Delegate.CreateDelegate(typeof(Action<DamageWorker, Explosion, Thing, List<Thing>, List<Thing>, IntVec3>), methodExplosionDamageThing);

        private static readonly MethodInfo methodExplosionDamageTerrain =
            Method(typeof(DamageWorker), "ExplosionDamageTerrain", new Type[] { typeof(Explosion), typeof(IntVec3) });
        private static readonly Action<DamageWorker, Explosion, IntVec3> actionExplosionDamageTerrain =
            (Action<DamageWorker, Explosion, IntVec3>)Delegate.CreateDelegate(typeof(Action<DamageWorker, Explosion, IntVec3>), methodExplosionDamageTerrain);

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

            if (thingsToAffect == null)
            {
                thingsToAffect = new List<Thing>();
            } else
            {
                thingsToAffect.Clear();
            }
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
                    actionExplosionDamageThing(__instance, explosion, thingsToAffect[j], damagedThings, ignoredThings, c);
                }
            }

            if (!flag)
            {
                actionExplosionDamageTerrain(__instance, explosion, c);
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

        public static bool ExplosionCellsToHit(DamageWorker __instance, ref IEnumerable<IntVec3> __result, IntVec3 center, Map map, float radius, IntVec3? needLOSToCell1 = null, IntVec3? needLOSToCell2 = null)
        {
            if (openCells == null)
            {
                openCells = new List<IntVec3>();
            }
            else
            {
                openCells.Clear();
            }
            if (adjWallCells == null)
            {
                adjWallCells = new List<IntVec3>();
            }
            else
            {
                adjWallCells.Clear();
            }
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map) || !GenSight.LineOfSight(center, intVec, map, skipFirstCell: true))
                {
                    continue;
                }

                if (needLOSToCell1.HasValue || needLOSToCell2.HasValue)
                {
                    bool flag = needLOSToCell1.HasValue && GenSight.LineOfSight(needLOSToCell1.Value, intVec, map);
                    bool flag2 = needLOSToCell2.HasValue && GenSight.LineOfSight(needLOSToCell2.Value, intVec, map);
                    if (!flag && !flag2)
                    {
                        continue;
                    }
                }

                openCells.Add(intVec);
            }

            for (int j = 0; j < openCells.Count; j++)
            {
                IntVec3 intVec2 = openCells[j];
                if (!intVec2.Walkable(map))
                {
                    continue;
                }

                for (int k = 0; k < 4; k++)
                {
                    IntVec3 intVec3 = intVec2 + GenAdj.CardinalDirections[k];
                    if (intVec3.InHorDistOf(center, radius) && intVec3.InBounds(map) && !intVec3.Standable(map) && intVec3.GetEdifice(map) != null && !openCells.Contains(intVec3) && adjWallCells.Contains(intVec3))
                    {
                        adjWallCells.Add(intVec3);
                    }
                }
            }

            __result = openCells.Concat(adjWallCells);
            return false;
        }


    }
}