using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimThreaded
{
    
    public class Thing_Patch
	{
		public static AccessTools.FieldRef<Thing, sbyte> mapIndexOrState =
			AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");
		public static AccessTools.FieldRef<RegionDirtyer, List<Region>> regionsToDirty =
			AccessTools.FieldRefAccess<RegionDirtyer, List<Region>>("regionsToDirty");
		public static AccessTools.FieldRef<RegionDirtyer, Map> map =
			AccessTools.FieldRefAccess<RegionDirtyer, Map>("map");
		public static AccessTools.FieldRef<RegionDirtyer, List<IntVec3>> dirtyCells =
			AccessTools.FieldRefAccess<RegionDirtyer, List<IntVec3>>("dirtyCells");
		//public static System.Reflection.MethodInfo removeRes = typeof(Thing).GetMethod("RemoveAllReservationsAndDesignationsOnThis", BindingFlags.Instance | BindingFlags.NonPublic);
		//public static System.Reflection.MethodInfo notifyMethod = typeof(RegionDirtyer).GetMethod("Notify_ThingAffectingRegionsDespawned", BindingFlags.Instance | BindingFlags.NonPublic);

		public static bool TakeDamage(Thing __instance, ref DamageWorker.DamageResult __result, DamageInfo dinfo)
		{
			
			if (__instance.Destroyed) {
				__result = new DamageWorker.DamageResult();
				return false;
			}
			if ((double)dinfo.Amount == 0.0) {
				__result = new DamageWorker.DamageResult();
				return false;
			}
			if (__instance.def.damageMultipliers != null)
			{
				for (int index = 0; index < __instance.def.damageMultipliers.Count; ++index)
				{
					if (__instance.def.damageMultipliers[index].damageDef == dinfo.Def)
					{
						int num = UnityEngine.Mathf.RoundToInt(dinfo.Amount * __instance.def.damageMultipliers[index].multiplier);
						dinfo.SetAmount((float)num);
					}
				}
			}
			//__result = new DamageWorker.DamageResult();
			//if (__instance is Plant)
			if(true)
			{
				bool absorbed;
				__instance.PreApplyDamage(ref dinfo, out absorbed);
				
				if (absorbed)
				{
					__result = new DamageWorker.DamageResult();
					return false;
				}

				bool anyParentSpawned = __instance.SpawnedOrAnyParentSpawned;
				Map mapHeld = __instance.MapHeld;

				DamageWorker.DamageResult damageResult = Apply(dinfo, __instance);
				if (dinfo.Def.harmsHealth & anyParentSpawned)
					mapHeld.damageWatcher.Notify_DamageTaken(__instance, damageResult.totalDamageDealt);
				if (dinfo.Def.ExternalViolenceFor(__instance))
				{
					GenLeaving.DropFilthDueToDamage(__instance, damageResult.totalDamageDealt);
					if (dinfo.Instigator != null && dinfo.Instigator is Pawn instigator)
					{
						instigator.records.AddTo(RecordDefOf.DamageDealt, damageResult.totalDamageDealt);
						instigator.records.AccumulateStoryEvent(StoryEventDefOf.DamageDealt);
					}
				}
				
				__instance.PostApplyDamage(dinfo, damageResult.totalDamageDealt);
				__result = damageResult;
			}
			
			return false;
		}
		public static DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
		{
			DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
			
			if (victim.SpawnedOrAnyParentSpawned)
				ImpactSoundUtility.PlayImpactSound(victim, dinfo.Def.impactSoundType, victim.MapHeld);
			if (victim.def.useHitPoints && dinfo.Def.harmsHealth)
			{
				float amount = dinfo.Amount;
				if (victim.def.category == ThingCategory.Building)
					amount *= dinfo.Def.buildingDamageFactor;
				if (victim.def.category == ThingCategory.Plant)
					amount *= dinfo.Def.plantDamageFactor;
				damageResult.totalDamageDealt = (float)Mathf.Min(victim.HitPoints, GenMath.RoundRandom(amount));
				victim.HitPoints -= Mathf.RoundToInt(damageResult.totalDamageDealt);
				if (victim.HitPoints <= 0)
				{
					victim.HitPoints = 0;
					victim.Kill(new DamageInfo?(dinfo), (Hediff)null);
					//Kill2(victim, new DamageInfo?(dinfo), (Hediff)null);
					//victim.Destroy(DestroyMode.KillFinalize);
				}
			}
			
			return damageResult;
		}

		public static void Kill2(Thing victim, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
			/*

		if (victim is Pawn pawn)
		{
			IntVec3 positionHeld = pawn.PositionHeld;
			Map map1 = pawn.Map;
			Map mapHeld = pawn.MapHeld;
			bool flag1 = pawn.Spawned;
			bool anyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
			bool wasWorldPawn = pawn.IsWorldPawn();
			Caravan caravan = pawn.GetCaravan();
			Building_Grave assignedGrave = (Building_Grave)null;
			if (pawn.ownership != null)
				assignedGrave = pawn.ownership.AssignedGrave;
			bool inBed = pawn.InBed();
			float bedRotation = 0.0f;
			if (inBed)
				bedRotation = pawn.CurrentBed().Rotation.AsAngle;
			ThingOwner thingOwner = (ThingOwner)null;
			bool containerEnclosed = pawn.InContainerEnclosed;
			if (containerEnclosed)
			{
				thingOwner = pawn.holdingOwner;
				thingOwner.Remove((Thing)pawn);
			}
			bool flag2 = false;
			bool flag3 = false;
			if (Current.ProgramState == ProgramState.Playing && map1 != null)
			{
				flag2 = map1.designationManager.DesignationOn((Thing)pawn, DesignationDefOf.Hunt) != null;
				flag3 = map1.designationManager.DesignationOn((Thing)pawn, DesignationDefOf.Slaughter) != null;
			}
			bool flag4 = PawnUtility.ShouldSendNotificationAbout(pawn) && (!flag3 || !dinfo.HasValue || dinfo.Value.Def != DamageDefOf.ExecutionCut);
			float fireSize = 0.0f;
			Thing attachment = pawn.GetAttachment(ThingDefOf.Fire);
			if (attachment != null)
				fireSize = ((Fire)attachment).CurrentSize();
			if (Current.ProgramState == ProgramState.Playing)
				Find.Storyteller.Notify_PawnEvent(pawn, AdaptationEvent.Died, new DamageInfo?());
			if (pawn.IsColonist)
				Find.StoryWatcher.statsRecord.Notify_ColonistKilled();
			if (flag1 && dinfo.HasValue && dinfo.Value.Def.ExternalViolenceFor((Thing)pawn))
				LifeStageUtility.PlayNearestLifestageSound(pawn, (Func<LifeStageAge, SoundDef>)(ls => ls.soundDeath), 1f);
			if (dinfo.HasValue && dinfo.Value.Instigator != null && dinfo.Value.Instigator is Pawn instigator)
			{
				RecordsUtility.Notify_PawnKilled(pawn, instigator);
				if (instigator.equipment != null)
					instigator.equipment.Notify_KilledPawn();
				if (pawn.IsColonist)
					instigator.records.AccumulateStoryEvent(StoryEventDefOf.KilledPlayer);
			}
			TaleUtility.Notify_PawnDied(pawn, dinfo);
			DamageInfo damageInfo;
			if (flag1)
			{
				BattleLog battleLog = Find.BattleLog;
				RulePackDef deathRules = pawn.RaceProps.DeathActionWorker.DeathRules;
				Pawn initiator = dinfo.HasValue ? dinfo.Value.Instigator as Pawn : (Pawn)null;
				Hediff culpritHediff = exactCulprit;
				BodyPartRecord culpritTargetDef;
				if (!dinfo.HasValue)
				{
					culpritTargetDef = (BodyPartRecord)null;
				}
				else
				{
					damageInfo = dinfo.Value;
					culpritTargetDef = damageInfo.HitPart;
				}
				BattleLogEntry_StateTransition entryStateTransition = new BattleLogEntry_StateTransition((Thing)pawn, deathRules, initiator, culpritHediff, culpritTargetDef);
				battleLog.Add((LogEntry)entryStateTransition);
			}
			pawn.health.surgeryBills.Clear();
			if (pawn.apparel != null)
				pawn.apparel.Notify_PawnKilled(dinfo);
			if (pawn.RaceProps.IsFlesh)
				Pawn_RelationsTracker_Patch.Notify_PawnKilled(pawn.relations, dinfo, map1);
			pawn.meleeVerbs.Notify_PawnKilled();
			for (int index = 0; index < pawn.health.hediffSet.hediffs.Count; ++index)
				pawn.health.hediffSet.hediffs[index].Notify_PawnKilled();
			if (pawn.ParentHolder is Pawn_CarryTracker parentHolder && pawn.holdingOwner.TryDrop_NewTmp((Thing)pawn, parentHolder.pawn.Position, parentHolder.pawn.Map, ThingPlaceMode.Near, out Thing _, (Action<Thing, int>)null, (Predicate<IntVec3>)null, true))
			{
				Map map2 = parentHolder.pawn.Map;
				flag1 = true;
			}
			PawnDiedOrDownedThoughtsUtility.RemoveLostThoughts(pawn);
			PawnDiedOrDownedThoughtsUtility.RemoveResuedRelativeThought(pawn);
			PawnDiedOrDownedThoughtsUtility.TryGiveThoughts(pawn, dinfo, PawnDiedOrDownedThoughtsKind.Died);
			pawn.health.SetDead();
			if (pawn.health.deflectionEffecter != null)
			{
				pawn.health.deflectionEffecter.Cleanup();
				pawn.health.deflectionEffecter = (Effecter)null;
			}
			if (pawn.health.woundedEffecter != null)
			{
				pawn.health.woundedEffecter.Cleanup();
				pawn.health.woundedEffecter = (Effecter)null;
			}
			caravan?.Notify_MemberDied(pawn);
			pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.IncappedOrKilled, dinfo);
			if (flag1)
				pawn.DropAndForbidEverything(false);
			if (flag1)
				pawn.DeSpawn(DestroyMode.Vanish);
			if (pawn.royalty != null)
				pawn.royalty.Notify_PawnKilled();
			Corpse corpse = (Corpse)null;
			if (!PawnGenerator.IsBeingGenerated(pawn))
			{
				if (containerEnclosed)
				{
					corpse = pawn.MakeCorpse(assignedGrave, inBed, bedRotation);
					if (!thingOwner.TryAdd((Thing)corpse, true))
					{
						corpse.Destroy(DestroyMode.Vanish);
						corpse = (Corpse)null;
					}
				}
				else if (anyParentSpawned)
				{
					if (pawn.holdingOwner != null)
						pawn.holdingOwner.Remove((Thing)pawn);
					corpse = pawn.MakeCorpse(assignedGrave, inBed, bedRotation);
					if (GenPlace.TryPlaceThing((Thing)corpse, positionHeld, mapHeld, ThingPlaceMode.Direct, (Action<Thing, int>)null, (Predicate<IntVec3>)null, new Rot4()))
					{
						corpse.Rotation = pawn.Rotation;
						if (HuntJobUtility.WasKilledByHunter(pawn, dinfo))
						{
							damageInfo = dinfo.Value;
							Pawn instigator2 = (Pawn)damageInfo.Instigator;
							LocalTargetInfo target = (LocalTargetInfo)(Thing)corpse;
							damageInfo = dinfo.Value;
							Job curJob = ((Pawn)damageInfo.Instigator).CurJob;
							instigator2.Reserve(target, curJob, 1, -1, (ReservationLayerDef)null, true);
						}
						else if (!flag2 && !flag3)
							corpse.SetForbiddenIfOutsideHomeArea();
						if ((double)fireSize > 0.0)
							FireUtility.TryStartFireIn(corpse.Position, corpse.Map, fireSize);
					}
					else
					{
						corpse.Destroy(DestroyMode.Vanish);
						corpse = (Corpse)null;
					}
				}
				else if (caravan != null && caravan.Spawned)
				{
					corpse = pawn.MakeCorpse(assignedGrave, inBed, bedRotation);
					caravan.AddPawnOrItem((Thing)corpse, true);
				}
				else if (pawn.holdingOwner != null || pawn.IsWorldPawn())
					Corpse.PostCorpseDestroy(pawn);
				else
					corpse = pawn.MakeCorpse(assignedGrave, inBed, bedRotation);
			}
			if (corpse != null)
			{
				Hediff firstHediffOfDef1 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup, false);
				Hediff firstHediffOfDef2 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Scaria, false);
				CompRottable comp = corpse.GetComp<CompRottable>();
				if (firstHediffOfDef1 != null && (double)Rand.Value < (double)firstHediffOfDef1.Severity && comp != null || firstHediffOfDef2 != null && Rand.Chance(Find.Storyteller.difficultyValues.scariaRotChance))
					comp.RotImmediately();
			}
			if (!pawn.Destroyed)
				pawn.Destroy(DestroyMode.KillFinalize);
			PawnComponentsUtility.RemoveComponentsOnKilled(pawn);
			pawn.health.hediffSet.DirtyCache();
			PortraitsCache.SetDirty(pawn);
			for (int index = pawn.health.hediffSet.hediffs.Count - 1; index >= 0; --index)
				pawn.health.hediffSet.hediffs[index].Notify_PawnDied();
			pawn.FactionOrExtraMiniOrHomeFaction?.Notify_MemberDied(pawn, dinfo, wasWorldPawn, mapHeld);
			if (corpse != null)
			{
				if (pawn.RaceProps.DeathActionWorker != null & flag1)
					pawn.RaceProps.DeathActionWorker.PawnDied(corpse);
				if (Find.Scenario != null)
					Find.Scenario.Notify_PawnDied(corpse);
			}
			if (pawn.Faction != null && pawn.Faction.IsPlayer)
				BillUtility.Notify_ColonistUnavailable(pawn);
			if (anyParentSpawned)
				GenHostility.Notify_PawnLostForTutor(pawn, mapHeld);
			if (pawn.Faction != null && pawn.Faction.IsPlayer && Current.ProgramState == ProgramState.Playing)
				Find.ColonistBar.MarkColonistsDirty();
			pawn.psychicEntropy?.Notify_PawnDied();
			if (flag4)
				pawn.health.NotifyPlayerOfKilled(dinfo, exactCulprit, caravan);
			Find.QuestManager.Notify_PawnKilled(pawn, dinfo);
			Find.FactionManager.Notify_PawnKilled(pawn);
		} else
            {
				//Destroy2(victim, DestroyMode.KillFinalize);
			}
			*/

		}

		public static bool Destroy2(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
		{
			if (!Thing.allowDestroyNonDestroyable && !__instance.def.destroyable)
			{
				//Log.Error("Tried to destroy non-destroyable thing " + __instance, false);
			}
			else if (__instance.Destroyed)
			{
				//Log.Error("Tried to destroy already-destroyed thing " + __instance, false);
			}
			else
			{
				/*
				int num = __instance.Spawned ? 1 : 0;
				Map map = __instance.Map;
				if (__instance.Spawned)
					__instance.DeSpawn(mode);
				mapIndexOrState(__instance) = (sbyte)-2;

				if (__instance.def.DiscardOnDestroyed)
					__instance.Discard(false);
				CompExplosive comp = __instance.TryGetComp<CompExplosive>();
				if (num != 0)
				{
					List<Thing> thingList = comp != null ? new List<Thing>() : null;
					GenLeaving.DoLeavingsFor(__instance, map, mode, thingList);
					comp?.AddThingsIgnoredByExplosion(thingList);
				}
				if (__instance.holdingOwner != null)
					__instance.holdingOwner.Notify_ContainedItemDestroyed(__instance);
				RemoveAllReservationsAndDesignationsOnThis2(__instance);
				if (!(__instance is Pawn))
					__instance.stackCount = 0;
				if (mode != DestroyMode.QuestLogic)
					QuestUtility.SendQuestTargetSignals(__instance.questTags, "Destroyed", __instance.Named("SUBJECT"));
				if (mode != DestroyMode.KillFinalize)
					return false;
				QuestUtility.SendQuestTargetSignals(__instance.questTags, "Killed", __instance.Named("SUBJECT"));
				*/
			}
			return false;
		}

		public static bool Destroy(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
        {
			if (!Thing.allowDestroyNonDestroyable && !__instance.def.destroyable)
				Log.Error("Tried to destroy non-destroyable thing " + __instance, false);
			else if (__instance.Destroyed)
			{
				Log.Error("Tried to destroy already-destroyed thing " + __instance, false);
			}
			else
			{
				int num = __instance.Spawned ? 1 : 0;
				Map map = __instance.Map;
				if (__instance.Spawned)
					__instance.DeSpawn(mode);
				mapIndexOrState(__instance) = (sbyte)-2;
				
				if (__instance.def.DiscardOnDestroyed)
					__instance.Discard(false);
					CompExplosive comp = __instance.TryGetComp<CompExplosive>();
				if (num != 0)
					{
						List<Thing> thingList = comp != null ? new List<Thing>() : null;
						GenLeaving.DoLeavingsFor(__instance, map, mode, thingList);
						comp?.AddThingsIgnoredByExplosion(thingList);
					}
					if (__instance.holdingOwner != null)
						__instance.holdingOwner.Notify_ContainedItemDestroyed(__instance);
					RemoveAllReservationsAndDesignationsOnThis2(__instance);
					if (!(__instance is Pawn))
						__instance.stackCount = 0;
					if (mode != DestroyMode.QuestLogic)
						QuestUtility.SendQuestTargetSignals(__instance.questTags, "Destroyed", __instance.Named("SUBJECT"));
					if (mode != DestroyMode.KillFinalize)
						return false;
					QuestUtility.SendQuestTargetSignals(__instance.questTags, "Killed", __instance.Named("SUBJECT"));
				
			}
			return false;
		}

		private static void RemoveAllReservationsAndDesignationsOnThis2(Thing __instance)
		{
			if (__instance.def.category == ThingCategory.Mote)
				return;
			List<Map> maps = Find.Maps;
			for (int index = 0; index < maps.Count; ++index)
			{
				maps[index].reservationManager.ReleaseAllForTarget(__instance);
				maps[index].physicalInteractionReservationManager.ReleaseAllForTarget(__instance);
				if (__instance is IAttackTarget target)
					maps[index].attackTargetReservationManager.ReleaseAllForTarget(target);
				maps[index].designationManager.RemoveAllDesignationsOn(__instance, false);
			}
		}

		public static bool DeSpawn(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
        {
			
			if (__instance.Destroyed)
			{
				Log.Error("Tried to despawn " + __instance.ToStringSafe() + " which is already destroyed.", false);
				return false;
			}
			if (!__instance.Spawned)
			{
				Log.Error("Tried to despawn " + __instance.ToStringSafe() + " which is not spawned.", false);
				return false;
			}
			Map map = __instance.Map;
			RegionListersUpdater.DeregisterInRegions(__instance, map);
			map.spawnedThings.Remove(__instance);
			map.listerThings.Remove(__instance);
			map.thingGrid.Deregister(__instance, false);
			map.coverGrid.DeRegister(__instance);
			if (__instance.def.receivesSignals)
			{
				Find.SignalManager.DeregisterReceiver(__instance);
			}
			map.tooltipGiverList.Notify_ThingDespawned(__instance);
			if (__instance.def.graphicData != null && __instance.def.graphicData.Linked)
			{
				map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
				map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
			}
			if (Find.Selector.IsSelected(__instance))
			{
				Find.Selector.Deselect(__instance);
				Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
			}
			__instance.DirtyMapMesh(map);
			if (__instance.def.drawerType != DrawerType.MapMeshOnly)
			{
				map.dynamicDrawManager.DeRegisterDrawable(__instance);
			}
			Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(__instance.Position);
			Room room = (validRegionAt_NoRebuild == null) ? null : validRegionAt_NoRebuild.Room;
			if (room != null)
			{
				room.Notify_ContainedThingSpawnedOrDespawned(__instance);
			}
			if (__instance.def.AffectsRegions)
			{
				Notify_ThingAffectingRegionsDespawned2(map.regionDirtyer, __instance);
			}
			if (__instance.def.pathCost != 0 || __instance.def.passability == Traversability.Impassable)
			{
				map.pathGrid.RecalculatePerceivedPathCostUnderThing(__instance);
			}
			if (__instance.def.AffectsReachability)
			{
				map.reachability.ClearCache();
			}
			Find.TickManager.DeRegisterAllTickabilityFor(__instance);
			mapIndexOrState(__instance) = -1;
			if (__instance.def.category == ThingCategory.Item)
			{
				map.listerHaulables.Notify_DeSpawned(__instance);
				map.listerMergeables.Notify_DeSpawned(__instance);
			}
			map.attackTargetsCache.Notify_ThingDespawned(__instance);
			map.physicalInteractionReservationManager.ReleaseAllForTarget(__instance);
			StealAIDebugDrawer.Notify_ThingChanged(__instance);
			IHaulDestination haulDestination = __instance as IHaulDestination;
			if (haulDestination != null)
			{
				map.haulDestinationManager.RemoveHaulDestination(haulDestination);
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
					slotGroup.parent.Notify_LostThing(__instance);
				}
			}
			QuestUtility.SendQuestTargetSignals(__instance.questTags, "Despawned", __instance.Named("SUBJECT"));
			
			return false;
		}
		static void Notify_ThingAffectingRegionsDespawned2(RegionDirtyer rd, Thing b)
		{
			regionsToDirty(rd).Clear();
			Region regionAtNoRebuild1 = map(rd).regionGrid.GetValidRegionAt_NoRebuild(b.Position);
			if (regionAtNoRebuild1 != null)
			{
				map(rd).temperatureCache.TryCacheRegionTempInfo(b.Position, regionAtNoRebuild1);
				regionsToDirty(rd).Add(regionAtNoRebuild1);
			}
			foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(b))
			{
				if (c.InBounds(map(rd)))
				{
					Region regionAtNoRebuild2 = map(rd).regionGrid.GetValidRegionAt_NoRebuild(c);
					if (regionAtNoRebuild2 != null)
					{
						map(rd).temperatureCache.TryCacheRegionTempInfo(c, regionAtNoRebuild2);
						regionsToDirty(rd).Add(regionAtNoRebuild2);
					}
				}
			}
			for (int index = 0; index < regionsToDirty(rd).Count; ++index)
				SetRegionDirty2(rd, regionsToDirty(rd)[index], true);
			regionsToDirty(rd).Clear();
			if (b.def.size.x == 1 && b.def.size.z == 1)
			{
				dirtyCells(rd).Add(b.Position);
			}
			else
			{
				CellRect cellRect = b.OccupiedRect();
				for (int minZ = cellRect.minZ; minZ <= cellRect.maxZ; ++minZ)
				{
					for (int minX = cellRect.minX; minX <= cellRect.maxX; ++minX)
						dirtyCells(rd).Add(new IntVec3(minX, 0, minZ));
				}
			}
		}


		private static void SetRegionDirty2(RegionDirtyer rd, Region reg, bool addCellsToDirtyCells = true)
		{
			if (!reg.valid)
				return;
			reg.valid = false;
			reg.Room = (Room)null;
			for (int index = 0; index < reg.links.Count; ++index)
				reg.links[index].Deregister(reg);
			reg.links.Clear();
			if (!addCellsToDirtyCells)
				return;
			foreach (IntVec3 cell in reg.Cells)
			{
				dirtyCells(rd).Add(cell);
				if (DebugViewSettings.drawRegionDirties)
					map(rd).debugDrawer.FlashCell(cell, 0.0f, (string)null, 50);
			}
		}

	}


}
