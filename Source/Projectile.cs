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
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

	public abstract class Projectile_Patch 
	{
		[ThreadStatic]
		public static List<Thing> cellThingsFiltered = new List<Thing>();
		[ThreadStatic]
		public static List<IntVec3> checkedCells = new List<IntVec3>();

		public static FieldRef<Projectile, Vector3> originField = FieldRefAccess<Projectile, Vector3>("origin");
		public static FieldRef<Projectile, Vector3> destinationField = FieldRefAccess<Projectile, Vector3>("destination");
		public static FieldRef<Projectile, Thing> launcherFieldRef = FieldRefAccess<Projectile, Thing>("launcher");
		public static FieldRef<Projectile, ThingDef> equipmentDef = FieldRefAccess<Projectile, ThingDef>("equipmentDef");
		public static FieldRef<Projectile, bool> landed = FieldRefAccess<Projectile, bool>("landed");
		public static FieldRef<Projectile, int> ticksToImpact = FieldRefAccess<Projectile, int>("ticksToImpact");
		public static FieldRef<Projectile, Sustainer> ambientSustainer = FieldRefAccess<Projectile, Sustainer>("ambientSustainer");
		public static FieldRef<Projectile, ThingDef> targetCoverDef = FieldRefAccess<Projectile, ThingDef>("targetCoverDef");

		public static FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

		private static readonly MethodInfo methodThrowDebugText =
			Method(typeof(Projectile), "ThrowDebugText");
		private static readonly Action<Projectile, string, IntVec3> actionThrowDebugText =
			(Action<Projectile, string, IntVec3 >)Delegate.CreateDelegate(typeof(Action<Projectile, string, IntVec3>), methodThrowDebugText);

		private static readonly MethodInfo methodImpact =
			Method(typeof(Projectile), "Impact");
		private static readonly Action<Projectile, Thing> actionImpact =
			(Action<Projectile, Thing>)Delegate.CreateDelegate(typeof(Action<Projectile, Thing>), methodImpact);

		private static readonly MethodInfo methodCanHit =
			Method(typeof(Projectile), "CanHit");
		private static readonly Func<Projectile, Thing, bool> funcCanHit =
			(Func<Projectile, Thing, bool>)Delegate.CreateDelegate(typeof(Func<Projectile, Thing, bool>), methodCanHit);

		private static readonly MethodInfo methodCheckForFreeIntercept =
			Method(typeof(Projectile), "CheckForFreeIntercept");
		private static readonly Func<Projectile, IntVec3, bool> funcCheckForFreeIntercept =
			(Func<Projectile, IntVec3, bool>)Delegate.CreateDelegate(typeof(Func<Projectile, IntVec3, bool>), methodCheckForFreeIntercept);

		public static bool CanHit(Projectile __instance, ref bool __result, Thing thing)
		{
			if (!thing.Spawned)
			{
				__result = false;
				return false;
			}

			if (thing == launcherFieldRef(__instance))
			{
				__result = false;
				return false;
			}

			bool flag = false;
			foreach (IntVec3 item in thing.OccupiedRect())
			{
				List<Thing> thingList = item.GetThingList(__instance.Map);
				bool flag2 = false;
				for (int i = 0; i < thingList.Count; i++)
				{
					Thing thing2;
					try
                    {
						thing2 = thingList[i];
					} 
					catch (ArgumentOutOfRangeException) {
						break;
					}
					if (thing2 != null && thing2 != thing && thing2.def != null && thing2.def.Fillage == FillCategory.Full && thing.def != null && thing2.def.Altitude >= thing.def.Altitude)
					{
						flag2 = true;
						break;
					}
				}

				if (!flag2)
				{
					flag = true;
					break;
				}
			}

			if (!flag)
			{
				__result = false;
				return false;
			}

			ProjectileHitFlags hitFlags = __instance.HitFlags;
			if (thing == __instance.intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != 0)
			{
				__result = true;
				return false;
			}

			if (thing != __instance.intendedTarget)
			{
				if (thing is Pawn)
				{
					if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != 0)
					{
						__result = true;
						return false;
					}
				}
				else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != 0)
				{
					__result = true;
					return false;
				}
			}

			if (thing == __instance.intendedTarget && thing.def.Fillage == FillCategory.Full)
			{
				__result = true;
				return false;
			}

			__result = false;
			return false;
		}

		public static bool ImpactSomething(Projectile __instance)
		{
			if (__instance.def.projectile.flyOverhead)
			{
				RoofDef roofDef = __instance.Map.roofGrid.RoofAt(__instance.Position);
				if (roofDef != null)
				{
					if (roofDef.isThickRoof)
					{
						actionThrowDebugText(__instance, "hit-thick-roof", __instance.Position);
						__instance.def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(__instance.Position, __instance.Map));
						__instance.Destroy();
						return false;
					}

					if (__instance.Position.GetEdifice(__instance.Map) == null || __instance.Position.GetEdifice(__instance.Map).def.Fillage != FillCategory.Full)
					{
						RoofCollapserImmediate.DropRoofInCells(__instance.Position, __instance.Map);
					}
				}
			}

			if (__instance.usedTarget.HasThing && funcCanHit(__instance, __instance.usedTarget.Thing))
			{
				Pawn pawn = __instance.usedTarget.Thing as Pawn;
				if (pawn != null && pawn.GetPosture() != 0 && (originField(__instance) - destinationField(__instance)).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.2f))
				{
					actionThrowDebugText(__instance, "miss-laying", __instance.Position);
					actionImpact(__instance, null);
				}
				else
				{
					actionImpact(__instance, __instance.usedTarget.Thing);
				}

				return false;
			}

			cellThingsFiltered.Clear();
			List<Thing> thingList = __instance.Position.GetThingList(__instance.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if ((thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Plant) && funcCanHit(__instance, thing))
				{
					cellThingsFiltered.Add(thing);
				}
			}

			cellThingsFiltered.Shuffle();
			for (int j = 0; j < cellThingsFiltered.Count; j++)
			{
				Thing thing2 = cellThingsFiltered[j];
				Pawn pawn2 = thing2 as Pawn;
				float num;
				if (pawn2 != null)
				{
					num = 0.5f * Mathf.Clamp(pawn2.BodySize, 0.1f, 2f);
					if (pawn2.GetPosture() != 0 && (originField(__instance) - destinationField(__instance)).MagnitudeHorizontalSquared() >= 20.25f)
					{
						num *= 0.2f;
					}

					if (launcherFieldRef(__instance) != null && pawn2.Faction != null && launcherFieldRef(__instance).Faction != null && !pawn2.Faction.HostileTo(launcherFieldRef(__instance).Faction))
					{
						num *= VerbUtility.InterceptChanceFactorFromDistance(originField(__instance), __instance.Position);
					}
				}
				else
				{
					num = 1.5f * thing2.def.fillPercent;
				}

				if (Rand.Chance(num))
				{
					actionThrowDebugText(__instance, "hit-" + num.ToStringPercent(), __instance.Position);
					actionImpact(__instance, cellThingsFiltered.RandomElement());
					return false;
				}

				actionThrowDebugText(__instance, "miss-" + num.ToStringPercent(), __instance.Position);
			}

			actionImpact(__instance, null);
			return false;
		}

		public static bool CheckForFreeInterceptBetween(Projectile __instance, ref bool __result, Vector3 lastExactPos, Vector3 newExactPos)
		{
			if (lastExactPos == newExactPos)
			{
				__result = false;
				return false;
			}

			List<Thing> list = __instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing;
				try
                {
					thing = list[i];
				} catch (ArgumentOutOfRangeException)
                {
					break;
                }
				if (thing.TryGetComp<CompProjectileInterceptor>().CheckIntercept(__instance, lastExactPos, newExactPos))
				{
					__instance.Destroy();
					__result = true;
					return false;
				}
			}

			IntVec3 intVec = lastExactPos.ToIntVec3();
			IntVec3 intVec2 = newExactPos.ToIntVec3();
			if (intVec2 == intVec)
			{
				__result = false;
				return false;
			}

			if (!intVec.InBounds(__instance.Map) || !intVec2.InBounds(__instance.Map))
			{
				__result = false;
				return false;
			}

			if (intVec2.AdjacentToCardinal(intVec))
			{
				__result = funcCheckForFreeIntercept(__instance, intVec2);
				return false;
			}

			if (VerbUtility.InterceptChanceFactorFromDistance(originField(__instance), intVec2) <= 0f)
			{
				__result = false;
				return false;
			}

			Vector3 vect = lastExactPos;
			Vector3 v = newExactPos - lastExactPos;
			Vector3 vector = v.normalized * 0.2f;
			int num = (int)(v.MagnitudeHorizontal() / 0.2f);
			checkedCells.Clear();
			int num2 = 0;
			IntVec3 intVec3;
			do
			{
				vect += vector;
				intVec3 = vect.ToIntVec3();
				if (!checkedCells.Contains(intVec3))
				{
					if (funcCheckForFreeIntercept(__instance, intVec3))
					{
						__result = true;
						return false;
					}

					checkedCells.Add(intVec3);
				}

				num2++;
				if (num2 > num)
				{
					__result = false;
					return false;
				}
			}
			while (!(intVec3 == intVec2));
			__result = false;
			return false;
		}

		public static bool CheckForFreeIntercept(Projectile __instance, ref bool __result, IntVec3 c)
		{
			if (destinationField(__instance).ToIntVec3() == c)
			{
				__result = false;
				return false;
			}

			float num = VerbUtility.InterceptChanceFactorFromDistance(originField(__instance), c);
			if (num <= 0f)
			{
				__result = false;
				return false;
			}

			bool flag = false;
			List<Thing> thingList = c.GetThingList(__instance.Map);
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
				if (!funcCanHit(__instance, thing))
				{
					continue;
				}

				bool flag2 = false;
				if (thing.def.Fillage == FillCategory.Full)
				{
					Building_Door building_Door = thing as Building_Door;
					if (building_Door == null || !building_Door.Open)
					{
						actionThrowDebugText(__instance, "int-wall", c);
						actionImpact(__instance, thing);
						__result = true;
						return false;
					}

					flag2 = true;
				}

				float num2 = 0f;
				Pawn pawn = thing as Pawn;
				if (pawn != null)
				{
					num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
					if (pawn.GetPosture() != 0)
					{
						num2 *= 0.1f;
					}

					if (launcherFieldRef(__instance) != null && pawn.Faction != null && launcherFieldRef(__instance).Faction != null && !pawn.Faction.HostileTo(launcherFieldRef(__instance).Faction))
					{
						num2 *= Find.Storyteller.difficultyValues.friendlyFireChanceFactor;
					}
				}
				else if (thing.def.fillPercent > 0.2f)
				{
					num2 = (flag2 ? 0.05f : ((!new IntVec3(destinationField(__instance)).AdjacentTo8Way(c)) ? (thing.def.fillPercent * 0.15f) : (thing.def.fillPercent * 1f)));
				}

				num2 *= num;
				if (num2 > 1E-05f)
				{
					if (Rand.Chance(num2))
					{
						actionThrowDebugText(__instance, "int-" + num2.ToStringPercent(), c);
						actionImpact(__instance, thing);
						__result = true;
						return false;
					}

					flag = true;
					actionThrowDebugText(__instance, num2.ToStringPercent(), c);
				}
			}

			if (!flag)
			{
				actionThrowDebugText(__instance, "o", c);
			}

			__result = false;
			return false;
		}




	}
}
