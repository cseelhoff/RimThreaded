using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    
    public static class Explosion_Patch
    {
        public static ConcurrentStack<HashSet<IntVec3>> hashSetIntVec3Stack = new ConcurrentStack<HashSet<IntVec3>>();
        public static ConcurrentStack<List<IntVec3>> listIntVec3Stack = new ConcurrentStack<List<IntVec3>>();
        public static ConcurrentStack<List<Thing>> listThingsStack = new ConcurrentStack<List<Thing>>();
        public static AccessTools.FieldRef<Explosion, List<IntVec3>> cellsToAffect =
            AccessTools.FieldRefAccess<Explosion, List<IntVec3>>("cellsToAffect");
        public static AccessTools.FieldRef<Explosion, List<Thing>> damagedThings =
            AccessTools.FieldRefAccess<Explosion, List<Thing>>("damagedThings");
        public static AccessTools.FieldRef<Explosion, HashSet<IntVec3>> addedCellsAffectedOnlyByDamage =
            AccessTools.FieldRefAccess<Explosion, HashSet<IntVec3>>("addedCellsAffectedOnlyByDamage");
        public static AccessTools.FieldRef<Explosion, int> startTick =
            AccessTools.FieldRefAccess<Explosion, int>("startTick");
        public static AccessTools.FieldRef<Explosion, List<Thing>> ignoredThings =
            AccessTools.FieldRefAccess<Explosion, List<Thing>>("ignoredThings");
        private static void TrySpawnExplosionThing(Explosion __instance, ThingDef thingDef, IntVec3 c, int count)
        {
            if (thingDef == null)
            {
                return;
            }
            if (thingDef.IsFilth)
            {
                FilthMaker.TryMakeFilth(c, __instance.Map, thingDef, count, FilthSourceFlags.None);
                return;
            }
            Thing thing = ThingMaker.MakeThing(thingDef, null);
            thing.stackCount = count;
            GenSpawn.Spawn(thing, c, __instance.Map, WipeMode.Vanish);
        }
        private static bool ShouldCellBeAffectedOnlyByDamage(Explosion __instance, IntVec3 c)
        {
            return __instance.applyDamageToExplosionCellsNeighbors && addedCellsAffectedOnlyByDamage(__instance).Contains(c);
        }
        private static void AffectCell(Explosion __instance, IntVec3 c)
        {
            if (!c.InBounds(__instance.Map))
            {
                return;
            }
            bool flag = ShouldCellBeAffectedOnlyByDamage(__instance, c);
            if (!flag && Rand.Chance(__instance.preExplosionSpawnChance) && c.Walkable(__instance.Map))
            {
                TrySpawnExplosionThing(__instance, __instance.preExplosionSpawnThingDef, c, __instance.preExplosionSpawnThingCount);
            }
            if (null != __instance.damType)
            {
                __instance.damType.Worker.ExplosionAffectCell(__instance, c, damagedThings(__instance), ignoredThings(__instance), !flag);
            }
            if (!flag && Rand.Chance(__instance.postExplosionSpawnChance) && c.Walkable(__instance.Map))
            {
                TrySpawnExplosionThing(__instance, __instance.postExplosionSpawnThingDef, c, __instance.postExplosionSpawnThingCount);
            }
            float num = __instance.chanceToStartFire;
            if (__instance.damageFalloff)
            {
                num *= Mathf.Lerp(1f, 0.2f, c.DistanceTo(__instance.Position) / __instance.radius);
            }
            if (Rand.Chance(num))
            {
                FireUtility.TryStartFireIn(c, __instance.Map, Rand.Range(0.1f, 0.925f));
            }
        }
        private static int GetCellAffectTick(Explosion __instance, IntVec3 cell)
        {
            return startTick(__instance) + (int)((cell - __instance.Position).LengthHorizontal * 1.5f);
        }
        public static bool Tick(Explosion __instance)
        {
            int ticksGame = Find.TickManager.TicksGame;
            int num;
            if (null != cellsToAffect(__instance))
            {
                lock (cellsToAffect(__instance))
                {
                    num = cellsToAffect(__instance).Count - 1;
                    while (num >= 0 && ticksGame >= GetCellAffectTick(__instance, cellsToAffect(__instance)[num]))
                    {
                        try
                        {
                            AffectCell(__instance, cellsToAffect(__instance)[num]);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(new object[]
                            {
                            "Explosion could not affect cell ",
                            cellsToAffect(__instance)[num],
                            ": ",
                            ex
                            }), false);
                        }
                        cellsToAffect(__instance).RemoveAt(num);
                        num--;
                    }
                    if (!cellsToAffect(__instance).Any())
                    {
                        __instance.Destroy(DestroyMode.Vanish);
                    }
                }
            }
            return false;
        }
        public static bool SpawnSetup(Explosion __instance, Map map, bool respawningAfterLoad)
        {
            Thing thing = (Thing)__instance;
            thing.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                if (!listIntVec3Stack.TryPop(out List<IntVec3> listIntVec3))
                {
                    listIntVec3 = new List<IntVec3>();
                }
                cellsToAffect(__instance) = listIntVec3;
                cellsToAffect(__instance).Clear();
                if (!listThingsStack.TryPop(out List<Thing> listThings))
                {
                    listThings = new List<Thing>();
                }
                damagedThings(__instance) = listThings;
                damagedThings(__instance).Clear();

                if (!hashSetIntVec3Stack.TryPop(out HashSet<IntVec3> hashSetIntVec3))
                {
                    hashSetIntVec3 = new HashSet<IntVec3>();
                }
                addedCellsAffectedOnlyByDamage(__instance) = hashSetIntVec3;
                addedCellsAffectedOnlyByDamage(__instance).Clear();
            }
            return false;
        }

        public static bool DeSpawn(Explosion __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            Thing thing = (Thing)__instance;
            thing.DeSpawn(mode);
            cellsToAffect(__instance).Clear();
            listIntVec3Stack.Push(cellsToAffect(__instance));
            cellsToAffect(__instance) = (List<IntVec3>)null;

            damagedThings(__instance).Clear();
            listThingsStack.Push(damagedThings(__instance));
            damagedThings(__instance) = (List<Thing>)null;

            addedCellsAffectedOnlyByDamage(__instance).Clear();
            hashSetIntVec3Stack.Push(addedCellsAffectedOnlyByDamage(__instance));
            addedCellsAffectedOnlyByDamage(__instance) = (HashSet<IntVec3>)null;
            return false;
        }

    }

}
