using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class Thing_Patch
    {
        public static Dictionary<Thing, sbyte> lastMapIndex = new Dictionary<Thing, sbyte>();
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Thing);
            Type patched = typeof(Thing_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Map));
            RimThreadedHarmony.Prefix(original, patched, nameof(TakeDamage));
            RimThreadedHarmony.Postfix(original, patched, nameof(SpawnSetup));
            //RimThreadedHarmony.Postfix(original, patched, nameof(DeSpawn));
            //RimThreadedHarmony.Prefix(original, patched, nameof(TakeDamage));
        }
        public static bool TakeDamage(Thing __instance, ref DamageWorker.DamageResult __result, DamageInfo dinfo)
        {
            //---START change---
            Map mapHeld = __instance.MapHeld; //moved
            if (mapHeld == null)
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }
            //---END change---

            if (__instance.Destroyed)
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }
            if ((double)dinfo.Amount == 0.0)
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }
            if (__instance.def.damageMultipliers != null)
            {
                for (int index = 0; index < __instance.def.damageMultipliers.Count; ++index)
                {
                    if (__instance.def.damageMultipliers[index].damageDef == dinfo.Def)
                    {
                        int num = Mathf.RoundToInt(dinfo.Amount * __instance.def.damageMultipliers[index].multiplier);
                        dinfo.SetAmount((float)num);
                    }
                }
            }
            bool absorbed;
            __instance.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed)
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }
            bool anyParentSpawned = __instance.SpawnedOrAnyParentSpawned;

            // Map mapHeld = __instance.MapHeld; //moved
            DamageWorker.DamageResult damageResult = dinfo.Def.Worker.Apply(dinfo, __instance);
            if (dinfo.Def.harmsHealth & anyParentSpawned)
                mapHeld.damageWatcher.Notify_DamageTaken(__instance, damageResult.totalDamageDealt);
            if (dinfo.Def.ExternalViolenceFor(__instance))
            {
                if (dinfo.SpawnFilth)
                    GenLeaving.DropFilthDueToDamage(__instance, damageResult.totalDamageDealt);
                if (dinfo.Instigator != null && dinfo.Instigator is Pawn instigator2)
                    instigator2.records.AddTo(RecordDefOf.DamageDealt, damageResult.totalDamageDealt);
            }
            __instance.PostApplyDamage(dinfo, damageResult.totalDamageDealt);
            __result = damageResult;
            return false;
        }
        //public static bool DeSpawn(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
        //{
        //    if (__instance.Destroyed)
        //        Log.Error("Tried to despawn " + __instance.ToStringSafe<Thing>() + " which is already destroyed.");
        //    else if (!__instance.Spawned)
        //    {
        //        Log.Error("Tried to despawn " + __instance.ToStringSafe<Thing>() + " which is not spawned.");
        //    }
        //    else
        //    {
        //        Map map = __instance.Map;
        //        map.overlayDrawer.DisposeHandle(__instance);
        //        RegionListersUpdater.DeregisterInRegions(__instance, map);
        //        ThingOwner newSpawnedThings = map.spawnedThings;
        //        map.spawnedThings.Remove(__instance);
        //        map.listerThings.Remove(__instance);
        //        map.thingGrid.Deregister(__instance);
        //        map.coverGrid.DeRegister(__instance);
        //        if (__instance.def.receivesSignals)
        //            Find.SignalManager.DeregisterReceiver((ISignalReceiver)__instance);
        //        map.tooltipGiverList.Notify_ThingDespawned(__instance);
        //        if (__instance.def.CanAffectLinker)
        //        {
        //            map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
        //            map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
        //        }
        //        if (Find.Selector.IsSelected((object)__instance))
        //        {
        //            Find.Selector.Deselect((object)__instance);
        //            Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
        //        }
        //        __instance.DirtyMapMesh(map);
        //        if (__instance.def.drawerType != DrawerType.MapMeshOnly)
        //            map.dynamicDrawManager.DeRegisterDrawable(__instance);
        //        Region regionAtNoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(__instance.Position);
        //        (regionAtNoRebuild == null ? (Room)null : regionAtNoRebuild.Room)?.Notify_ContainedThingSpawnedOrDespawned(__instance);
        //        if (__instance.def.AffectsRegions)
        //            map.regionDirtyer.Notify_ThingAffectingRegionsDespawned(__instance);
        //        if (__instance.def.pathCost != 0 || __instance.def.passability == Traversability.Impassable)
        //            map.pathing.RecalculatePerceivedPathCostUnderThing(__instance);
        //        if (__instance.def.AffectsReachability)
        //            map.reachability.ClearCache();
        //        Find.TickManager.DeRegisterAllTickabilityFor(__instance);
        //        __instance.mapIndexOrState = (sbyte)-1;
        //        if (__instance.def.category == ThingCategory.Item)
        //        {
        //            map.listerHaulables.Notify_DeSpawned(__instance);
        //            map.listerMergeables.Notify_DeSpawned(__instance);
        //        }
        //        map.attackTargetsCache.Notify_ThingDespawned(__instance);
        //        map.physicalInteractionReservationManager.ReleaseAllForTarget((LocalTargetInfo)__instance);
        //        StealAIDebugDrawer.Notify_ThingChanged(__instance);
        //        if (__instance is IHaulDestination haulDestination3)
        //            map.haulDestinationManager.RemoveHaulDestination(haulDestination3);
        //        if (__instance is IThingHolder && Find.ColonistBar != null)
        //            Find.ColonistBar.MarkColonistsDirty();
        //        if (__instance.def.category == ThingCategory.Item)
        //        {
        //            SlotGroup slotGroup = __instance.Position.GetSlotGroup(map);
        //            if (slotGroup != null && slotGroup.parent != null)
        //                slotGroup.parent.Notify_LostThing(__instance);
        //        }
        //        QuestUtility.SendQuestTargetSignals(__instance.questTags, "Despawned", __instance.Named("SUBJECT"));
        //    }
        //    return false;
        //}

        public static bool get_Map(Thing __instance, ref Map __result)
        {
            __result = null;
            if (__instance.mapIndexOrState >= 0)
                __result = Find.Maps[__instance.mapIndexOrState];
            else
            {
                lock (lastMapIndex)
                {
                    if (lastMapIndex.TryGetValue(__instance, out sbyte lastIndex))
                    {
                        __result = Find.Maps[lastIndex];
                    }
                }
            }
            return false;
        }

        public static void SpawnSetup(Thing __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance.mapIndexOrState >= 0)
            {
                lock (lastMapIndex)
                {
                    lastMapIndex[__instance] = __instance.mapIndexOrState;
                }
            }
        }

    }
}
