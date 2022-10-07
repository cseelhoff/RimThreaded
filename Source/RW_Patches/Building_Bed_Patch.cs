using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    class Building_Bed_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Building_Bed);
            Type patched = typeof(Building_Bed_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(SpawnSetup));
        }
        internal static Faction ThingFaction(Thing __instance)
        {
            return __instance.factionInt;
        }
        internal static Map ThingMap(Thing __instance)
        {
            if (__instance.mapIndexOrState >= 0)
            {
                return Find.Maps[__instance.mapIndexOrState];
            }
            return null;
        }
        internal static IntVec3 ThingPos(Thing __instance)
        {
            return __instance.positionInt;
        }
        internal static void SpawnSetupThing(Thing __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance.Destroyed)
            {
                Log.Error(string.Concat("Spawning destroyed thing ", __instance, " at ", __instance.Position, ". Correcting."));
                __instance.mapIndexOrState = -1;
                if (__instance.HitPoints <= 0 && __instance.def.useHitPoints)
                {
                    __instance.HitPoints = 1;
                }
            }
            if (__instance.Spawned)
            {
                Log.Error(string.Concat("Tried to spawn already-spawned thing ", __instance, " at ", __instance.Position));
                return;
            }
            int num = Find.Maps.IndexOf(map);
            if (num < 0)
            {
                Log.Error(string.Concat("Tried to spawn thing ", __instance, ", but the map provided does not exist."));
                return;
            }
            if (__instance.stackCount > __instance.def.stackLimit)
            {
                Log.Error(string.Concat("Spawned ", __instance, " with stackCount ", __instance.stackCount, " but stackLimit is ", __instance.def.stackLimit, ". Truncating."));
                __instance.stackCount = __instance.def.stackLimit;
            }
            __instance.mapIndexOrState = (sbyte)num;
            RegionListersUpdater.RegisterInRegions(__instance, map);
            if (!map.spawnedThings.TryAdd(__instance, canMergeWithExistingStacks: false))
            {
                Log.Error(string.Concat("Couldn't add thing ", __instance, " to spawned things."));
            }
            map.listerThings.Add(__instance);
            map.thingGrid.Register(__instance);
            if (map.IsPlayerHome)
            {
                __instance.EverSeenByPlayer = true;
            }
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(__instance);
            }
            __instance.DirtyMapMesh(map);
            if (__instance.def.drawerType != DrawerType.MapMeshOnly)
            {
                map.dynamicDrawManager.RegisterDrawable(__instance);
            }
            map.tooltipGiverList.Notify_ThingSpawned(__instance);
            if (__instance.def.CanAffectLinker)
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
                map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, regenAdjacentCells: true, regenAdjacentSections: false);
            }
            if (!__instance.def.CanOverlapZones)
            {
                map.zoneManager.Notify_NoZoneOverlapThingSpawned(__instance);
            }
            if (__instance.def.AffectsRegions)
            {
                map.regionDirtyer.Notify_ThingAffectingRegionsSpawned(__instance);
            }
            if (__instance.def.pathCost != 0 || __instance.def.passability == Traversability.Impassable)
            {
                map.pathing.RecalculatePerceivedPathCostUnderThing(__instance);
            }
            if (__instance.def.AffectsReachability)
            {
                map.reachability.ClearCache();
            }
            map.coverGrid.Register(__instance);
            if (__instance.def.category == ThingCategory.Item)
            {
                map.listerHaulables.Notify_Spawned(__instance);
                map.listerMergeables.Notify_Spawned(__instance);
            }
            map.attackTargetsCache.Notify_ThingSpawned(__instance);
            (map.regionGrid.GetValidRegionAt_NoRebuild(__instance.Position)?.Room)?.Notify_ContainedThingSpawnedOrDespawned(__instance);
            StealAIDebugDrawer.Notify_ThingChanged(__instance);
            IHaulDestination haulDestination = __instance as IHaulDestination;
            if (haulDestination != null)
            {
                map.haulDestinationManager.AddHaulDestination(haulDestination);
            }
            if (__instance is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            if (__instance.def.category == ThingCategory.Item)
            {
                SlotGroup slotGroup = __instance.Position.GetSlotGroup(map);
                if (slotGroup != null && slotGroup.parent != null)
                {
                    slotGroup.parent.Notify_ReceivedThing(__instance);
                }
            }
            if (__instance.def.receivesSignals)
            {
                Find.SignalManager.RegisterReceiver(__instance);
            }
            if (!respawningAfterLoad)
            {
                QuestUtility.SendQuestTargetSignals(__instance.questTags, "Spawned", __instance.Named("SUBJECT"));
            }
        }
        internal static void SpawnSetupThingWC(ThingWithComps __instance, Map map, bool respawningAfterLoad)
        {
            SpawnSetupThing(__instance, map, respawningAfterLoad);
            if (__instance.comps != null)
            {
                for (int i = 0; i < __instance.comps.Count; i++)
                {
                    __instance.comps[i].PostSpawnSetup(respawningAfterLoad);
                }
            }
        }
        internal static void SpawnSetupBuilding(Building __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance.def.IsEdifice())
            {
                map.edificeGrid.Register(__instance);
                if (__instance.def.Fillage == FillCategory.Full)
                {
                    map.terrainGrid.Drawer.SetDirty();
                }
                if (__instance.def.AffectsFertility)
                {
                    map.fertilityGrid.Drawer.SetDirty();
                }
            }
            SpawnSetupThingWC(__instance, map, respawningAfterLoad);
            Map Basemap = ThingMap(__instance);
            Basemap.listerBuildings.Add(__instance);
            if (__instance.def.coversFloor)
            {
                Basemap.mapDrawer.MapMeshDirty(ThingPos(__instance), MapMeshFlag.Terrain, regenAdjacentCells: true, regenAdjacentSections: false);
            }
            CellRect cellRect = __instance.OccupiedRect();
            for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
            {
                for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                {
                    IntVec3 intVec = new IntVec3(j, 0, i);
                    Basemap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.Buildings);
                    Basemap.glowGrid.MarkGlowGridDirty(intVec);
                    if (!SnowGrid.CanCoexistWithSnow(__instance.def))
                    {
                        Basemap.snowGrid.SetDepth(intVec, 0f);
                    }
                }
            }
            if (ThingFaction(__instance) == Faction.OfPlayer && __instance.def.building != null && __instance.def.building.spawnedConceptLearnOpportunity != null)
            {
                LessonAutoActivator.TeachOpportunity(__instance.def.building.spawnedConceptLearnOpportunity, OpportunityType.GoodToKnow);
            }
            AutoHomeAreaMaker.Notify_BuildingSpawned(__instance);
            if (__instance.def.building != null && !__instance.def.building.soundAmbient.NullOrUndefined())
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    SoundInfo info = SoundInfo.InMap(__instance);
                    __instance.sustainerAmbient = __instance.def.building.soundAmbient.TrySpawnSustainer(info);
                });
            }
            Basemap.listerBuildingsRepairable.Notify_BuildingSpawned(__instance);
            Basemap.listerArtificialBuildingsForMeditation.Notify_BuildingSpawned(__instance);
            Basemap.listerBuldingOfDefInProximity.Notify_BuildingSpawned(__instance);
            Basemap.listerBuildingWithTagInProximity.Notify_BuildingSpawned(__instance);
            if (!__instance.CanBeSeenOver())
            {
                Basemap.exitMapGrid.Notify_LOSBlockerSpawned();
            }
            SmoothSurfaceDesignatorUtility.Notify_BuildingSpawned(__instance);
            map.avoidGrid.Notify_BuildingSpawned(__instance);
            map.lordManager.Notify_BuildingSpawned(__instance);
            map.animalPenManager.Notify_BuildingSpawned(__instance);
        }
        internal static void Notify_RoomShapeChanged2(Room r)//Maybe it must be locked to prevent adding/removing districts for r.Map
        {
            Map tmpMap = r.Map;//changed
            if (tmpMap is null)// it is likely better to not even start the Notify procedure if map is null
            {
                return;
            }
            r.cachedCellCount = -1;
            r.cachedOpenRoofCount = -1;
            if (r.Dereferenced)
            {
                r.isPrisonCell = false;
                r.statsAndRoleDirty = true;
                return;
            }
            r.tempTracker.RoomChanged();
            if (Current.ProgramState == ProgramState.Playing && !r.Fogged)
            {
                tmpMap.autoBuildRoofAreaSetter.TryGenerateAreaFor(r);
            }
            r.isPrisonCell = false;
            if (Building_Bed.RoomCanBePrisonCell(r))
            {
                List<Thing> containedAndAdjacentThings = r.ContainedAndAdjacentThings;
                for (int i = 0; i < containedAndAdjacentThings.Count; i++)
                {
                    Building_Bed building_Bed = containedAndAdjacentThings[i] as Building_Bed;
                    if (building_Bed != null && building_Bed.ForPrisoners)
                    {
                        r.isPrisonCell = true;
                        break;
                    }
                }
            }
            List<Thing> list = tmpMap.listerThings.ThingsOfDef(ThingDefOf.NutrientPasteDispenser);
            for (int j = 0; j < list.Count; j++)
            {
                list[j].Notify_ColorChanged();
            }
            if (Current.ProgramState == ProgramState.Playing && r.isPrisonCell)
            {
                foreach (Building_Bed containedBed in r.ContainedBeds)
                {
                    containedBed.ForPrisoners = true;
                }
            }
            r.statsAndRoleDirty = true;
        }
        public static bool SpawnSetup(Building_Bed __instance, Map map, bool respawningAfterLoad)
        {
            SpawnSetupBuilding(__instance, map, respawningAfterLoad);

            Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(ThingPos(__instance));

            if (validRegionAt_NoRebuild != null && validRegionAt_NoRebuild.Room.IsPrisonCell)
            {
                __instance.ForPrisoners = true;
            }
            if (!__instance.alreadySetDefaultMed)
            {
                __instance.alreadySetDefaultMed = true;
                if (__instance.def.building.bed_defaultMedical)
                {
                    __instance.Medical = true;
                }
            }
            if (!respawningAfterLoad)
            {
                District district = __instance.GetDistrict();
                if (district != null)
                {
                    district.Notify_RoomShapeOrContainedBedsChanged();
                    Notify_RoomShapeChanged2(district.Room);
                }
            }
            return false;
        }
    }
}
