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

    public class Projectile_DoomsdayRocket_Patch
	{

		public static bool Impact(Projectile_DoomsdayRocket __instance, Thing hitThing)
		{
			
            Map map = __instance.Map;
            Projectile_Patch.Base_Impact(__instance, hitThing);
            GenExplosion.DoExplosion(__instance.Position, map, __instance.def.projectile.explosionRadius, DamageDefOf.Bomb, Projectile_Patch.launcher(__instance), __instance.DamageAmount, 
                __instance.ArmorPenetration, null, Projectile_Patch.equipmentDef(__instance), __instance.def, postExplosionSpawnThingDef: ThingDefOf.Filth_Fuel, 
                intendedTarget: __instance.intendedTarget.Thing, 
                postExplosionSpawnChance: 0.2f, postExplosionSpawnThingCount: 1, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, 
                preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0.4f);
            CellRect cellRect = CellRect.CenteredOn(__instance.Position, 5);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < 3; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                DoFireExplosion(__instance, randomCell, map, 3.9f);
            }
            return false;
		}
        private static void DoFireExplosion(Projectile_DoomsdayRocket __instance, IntVec3 pos, Map map, float radius)
        {
            GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Flame, Projectile_Patch.launcher(__instance), __instance.DamageAmount, __instance.ArmorPenetration, null, 
                Projectile_Patch.equipmentDef(__instance), __instance.def, __instance.intendedTarget.Thing);
        }
    }
}
