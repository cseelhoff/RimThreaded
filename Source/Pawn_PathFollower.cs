using HarmonyLib;
using System.Collections.Generic;
using System.Threading;
using Verse;
using Verse.AI;

namespace RimThreaded
{
	public class Pawn_PathFollower_Patch
	{

		public static AccessTools.FieldRef<Pawn_PathFollower, LocalTargetInfo> destination =
			AccessTools.FieldRefAccess<Pawn_PathFollower, LocalTargetInfo>("destination");
		public static AccessTools.FieldRef<Pawn_PathFollower, Pawn> pawn =
			AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");
		public static AccessTools.FieldRef<Pawn_PathFollower, PathEndMode> peMode =
			AccessTools.FieldRefAccess<Pawn_PathFollower, PathEndMode>("peMode");

		public static Dictionary<int, Dictionary<Map, PathFinder>> threadMapPathFinderDict = new Dictionary<int, Dictionary<Map, PathFinder>>();

		public static bool GenerateNewPath(Pawn_PathFollower __instance, ref PawnPath __result)
		{
			PathFinder pathFinder;
			lock (threadMapPathFinderDict)
			{
				__instance.lastPathedTargetPosition = destination(__instance).Cell;
				if (!threadMapPathFinderDict.TryGetValue(Thread.CurrentThread.ManagedThreadId, out Dictionary<Map, PathFinder> mapPathFinderDict))
				{
					mapPathFinderDict = new Dictionary<Map, PathFinder>();
					threadMapPathFinderDict.Add(Thread.CurrentThread.ManagedThreadId, mapPathFinderDict);
				}
				if(!mapPathFinderDict.TryGetValue(pawn(__instance).Map, out pathFinder)) {
					pathFinder = new PathFinder(pawn(__instance).Map);
					mapPathFinderDict.Add(pawn(__instance).Map, pathFinder);
				}
				__result = pathFinder.FindPath(pawn(__instance).Position, destination(__instance), pawn(__instance), peMode(__instance));
			}
			return false;
		}
	}
}