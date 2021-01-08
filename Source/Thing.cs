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
		public static bool get_FlammableNow(Thing __instance, ref bool __result)
		{
			if (__instance.GetStatValue(StatDefOf.Flammability) < 0.01f)
			{
				__result = false;
				return false;
			}

			if (__instance.Spawned && !__instance.FireBulwark)
			{
				List<Thing> thingList = __instance.Position.GetThingList(__instance.Map);
				if (thingList != null)
				{
					for (int i = 0; i < thingList.Count; i++)
					{
						Thing thing;
						try
                        {
							 thing = thingList[i];
						} catch (ArgumentOutOfRangeException)
                        {
							break;
                        }
						if (thing != null && thing.FireBulwark)
						{
							__result = false;
							return false;
						}
					}
				}
			}

			__result = true;
			return false;
		}
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
				}
			}
			
			return damageResult;
		}



	}


}
