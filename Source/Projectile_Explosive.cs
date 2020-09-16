using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class Projectile_Explosive_Patch {

		public static AccessTools.FieldRef<Projectile_Explosive, int> ticksToDetonation =
			AccessTools.FieldRefAccess<Projectile_Explosive, int>("ticksToDetonation");
		public static bool Impact(Projectile_Explosive __instance, Thing hitThing)
		{
			if (__instance.def.projectile.explosionDelay == 0)
			{
				Explode(__instance);
				return false;
			}

			Projectile_Patch.landed(__instance) = true;
			ticksToDetonation(__instance) = __instance.def.projectile.explosionDelay;
			GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(__instance, __instance.def.projectile.damageDef, Projectile_Patch.launcher(__instance).Faction);
			return false;
		}
		public static bool Explode(Projectile_Explosive __instance)
		{
			Map map = __instance.Map;
			__instance.Destroy();
			if (__instance.def.projectile.explosionEffect != null)
			{
				Effecter effecter = __instance.def.projectile.explosionEffect.Spawn();
				effecter.Trigger(new TargetInfo(__instance.Position, map), new TargetInfo(__instance.Position, map));
				effecter.Cleanup();
			}

			GenExplosion.DoExplosion(__instance.Position, map, __instance.def.projectile.explosionRadius, __instance.def.projectile.damageDef, Projectile_Patch.launcher(__instance), __instance.DamageAmount,
				__instance.ArmorPenetration, __instance.def.projectile.soundExplode, Projectile_Patch.equipmentDef(__instance), __instance.def, __instance.intendedTarget.Thing, __instance.def.projectile.postExplosionSpawnThingDef,
				__instance.def.projectile.postExplosionSpawnChance, __instance.def.projectile.postExplosionSpawnThingCount, preExplosionSpawnThingDef: __instance.def.projectile.preExplosionSpawnThingDef, 
				preExplosionSpawnChance: __instance.def.projectile.preExplosionSpawnChance, preExplosionSpawnThingCount: __instance.def.projectile.preExplosionSpawnThingCount, 
				applyDamageToExplosionCellsNeighbors: __instance.def.projectile.applyDamageToExplosionCellsNeighbors, chanceToStartFire: __instance.def.projectile.explosionChanceToStartFire, 
				damageFalloff: __instance.def.projectile.explosionDamageFalloff, direction: Projectile_Patch.origin(__instance).AngleToFlat(Projectile_Patch.destination(__instance)));
			return false;
		}
	}
}
