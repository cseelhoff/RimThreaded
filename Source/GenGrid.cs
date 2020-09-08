using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RimThreaded
{

    public class GenGrid_Patch
    {
		public static bool Standable(ref bool __result, IntVec3 c, Map map)
		{
			if(null == map)
			{
				__result = false;
				return false;
			}
			if (null == map.pathGrid)
            {
				__result = false;
				return false;
			}
			if (!map.pathGrid.Walkable(c))
			{
				__result = false;
				return false;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(c);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].def.passability != Traversability.Standable)
				{
					__result = false;
					return false;
				}
			}
			__result = true;
			return false;
		}
		public static bool InBounds(ref bool __result, IntVec3 c, Map map)
		{
			__result = false;
			if (null != map && null != c)
			{
				IntVec3 size = map.Size;
				if (null != size)
				{
					__result = c.x < size.x && c.z < size.z;
				}
			}
			return __result;
		}
		public static bool InBounds(ref bool __result, Vector3 v, Map map)
		{
			__result = false;
			if (null != map && null != v)
			{
				IntVec3 size = map.Size;
				if (null != size)
				{
					__result = v.x >= 0f && v.z >= 0f && v.x < size.x && v.z < size.z;
				}
			}
			return __result;
		}
		public static bool Walkable(ref bool __result, IntVec3 c, Map map)
		{
			__result = false;
			if (null != map) //Possible problem with Verse_RegionTraverser_Patch.BreadthFirstTraverse -> BFSWorker_Patch.BreadthFirstTraverseWork ?
			{
				PathGrid pg = map.pathGrid;
				if (null != pg)
				{
					__result = map.pathGrid.Walkable(c);
				}
			}
			return false;
		}
	}
}
