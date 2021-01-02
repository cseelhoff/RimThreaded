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
        public static AccessTools.FieldRef<Explosion, List<Thing>> ignoredThingsFR =
            AccessTools.FieldRefAccess<Explosion, List<Thing>>("ignoredThings");
        public static Dictionary<Explosion, List<IntVec3>> cellsToAffectDict = new Dictionary<Explosion, List<IntVec3>>();
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
                __instance.damType.Worker.ExplosionAffectCell(__instance, c, damagedThings(__instance), ignoredThingsFR(__instance), !flag);
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
            List<IntVec3> cells = getCellsToAffectFromDict(__instance);
            if (null != cells)
            {
                lock (cells)
                {
                    num = cells.Count - 1;
                    while (num >= 0 && ticksGame >= GetCellAffectTick(__instance, cells[num]))
                    {
                        try
                        {
                            AffectCell(__instance, cells[num]);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(new object[]
                            {
                            "Explosion could not affect cell ",
                            cells[num],
                            ": ",
                            ex
                            }), false);
                        }
                        cells.RemoveAt(num);
                        num--;
                    }
                    if (!cells.Any())
                    {
                        __instance.Destroy(DestroyMode.Vanish);
                    }
                }
            }
            return false;
        }
        public static bool StartExplosion(Explosion __instance, SoundDef explosionSound, List<Thing> ignoredThings)
        {
            Thing thing = __instance;
            if (!thing.Spawned)
            {
                Log.Error("Called StartExplosion() on unspawned thing.");
                return false;
            }

            startTick(__instance) = Find.TickManager.TicksGame;
            ignoredThingsFR(__instance) = ignoredThings;
            //List<IntVec3> cells = cellsToAffect(__instance);
            List<IntVec3> cells = getCellsToAffectFromDict(__instance);
            lock (cells)
            {
                cells.Clear();
                damagedThings(__instance).Clear();
                addedCellsAffectedOnlyByDamage(__instance).Clear();
                cells.AddRange(__instance.damType.Worker.ExplosionCellsToHit(__instance));
                if (__instance.applyDamageToExplosionCellsNeighbors)
                {
                    AddCellsNeighbors2(__instance, cells);
                }

                __instance.damType.Worker.ExplosionStart(__instance, cells);
                PlayExplosionSound2(__instance, explosionSound);
                MoteMaker.MakeWaterSplash(thing.Position.ToVector3Shifted(), thing.Map, __instance.radius * 6f, 20f);
                cells.Sort((IntVec3 a, IntVec3 b) => GetCellAffectTick2(__instance, b).CompareTo(GetCellAffectTick2(__instance, a)));
            }
            RegionTraverser.BreadthFirstTraverse(thing.Position, thing.Map, (Region from, Region to) => true, delegate (Region x)
            {
                List<Thing> allThings = x.ListerThings.AllThings;
                for (int num = allThings.Count - 1; num >= 0; num--)
                {
                    if (allThings[num].Spawned)
                    {
                        allThings[num].Notify_Explosion(__instance);
                    }
                }

                return false;
            }, 25);

            return false;
        }

        private static List<IntVec3> getCellsToAffectFromDict(Explosion __instance)
        {
            if(cellsToAffect(__instance) == null)
            {
                return null;
            }
            if(!cellsToAffectDict.TryGetValue(__instance, out List<IntVec3> value)) {
                lock(cellsToAffectDict)
                {
                    if (!cellsToAffectDict.TryGetValue(__instance, out List<IntVec3> value2))
                    {
                        value = new List<IntVec3>();
                        cellsToAffectDict[__instance] = value;
                    }
                }
            }
            return value;

        }

        private static void AddCellsNeighbors2(Explosion __instance, List<IntVec3> cells)
        {
            HashSet<IntVec3> tmpCells = new HashSet<IntVec3>();
            addedCellsAffectedOnlyByDamage(__instance).Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                tmpCells.Add(cells[i]);
            }
            Thing thing = __instance;
            for (int j = 0; j < cells.Count; j++)
            {
                if (!cells[j].Walkable(thing.Map))
                {
                    continue;
                }

                for (int k = 0; k < GenAdj.AdjacentCells.Length; k++)
                {
                    IntVec3 intVec = cells[j] + GenAdj.AdjacentCells[k];
                    if (intVec.InBounds(thing.Map) && tmpCells.Add(intVec))
                    {
                        addedCellsAffectedOnlyByDamage(__instance).Add(intVec);
                    }
                }
            }

            cells.Clear();
            foreach (IntVec3 tmpCell in tmpCells)
            {
                cells.Add(tmpCell);
            }

        }
        private static void PlayExplosionSound2(Explosion __instance, SoundDef explosionSound)
        {
            Thing thing = __instance;
            if ((!Prefs.DevMode) ? (!explosionSound.NullOrUndefined()) : (explosionSound != null))
            {
                SoundStarter_Patch.PlayOneShot(explosionSound, (new TargetInfo(thing.Position, thing.Map)));
            }
            else
            {
                SoundStarter_Patch.PlayOneShot(__instance.damType.soundExplosion, new TargetInfo(thing.Position, thing.Map));
            }
        }
        private static int GetCellAffectTick2(Explosion __instance, IntVec3 cell)
        {
            Thing thing = __instance;
            return startTick(__instance) + (int)((cell - thing.Position).LengthHorizontal * 1.5f);
        }
        public static bool SpawnSetup(Explosion __instance, Map map, bool respawningAfterLoad)
        {
            Thing thing = __instance;
            thing.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                lock (cellsToAffect(__instance))
                {
                    cellsToAffect(__instance) = new List<IntVec3>();
                }
                //cellsToAffect.Clear();
                lock (damagedThings(__instance))
                {
                    damagedThings(__instance) = new List<Thing>();
                }
                //damagedThings.Clear();
                lock (addedCellsAffectedOnlyByDamage(__instance))
                {
                    addedCellsAffectedOnlyByDamage(__instance) = new HashSet<IntVec3>();
                }
                //addedCellsAffectedOnlyByDamage.Clear();
            }
            return false;
        }

        public static bool DeSpawn(Explosion __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            Thing thing = __instance;
            thing.DeSpawn(mode);
            //cellsToAffect(__instance) = null;
            //damagedThings(__instance) = null;
            //addedCellsAffectedOnlyByDamage(__instance) = null;
            return false;
        }
    }

}
