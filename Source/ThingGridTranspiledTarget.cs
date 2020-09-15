using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class ThingGrid2
    {
		private Map map;

		private List<Thing>[] thingGrid;

		private static readonly List<Thing> EmptyThingList = new List<Thing>();

		public ThingGrid2(Map map)
		{
			this.map = map;
			CellIndices cellIndices = map.cellIndices;
			thingGrid = new List<Thing>[cellIndices.NumGridCells];
			for (int i = 0; i < cellIndices.NumGridCells; i++)
			{
				thingGrid[i] = new List<Thing>(4);
			}
		}

		public void Register(Thing t)
		{
			if (t.def.size.x == 1 && t.def.size.z == 1)
			{
				RegisterInCell(t, t.Position);
				return;
			}
			CellRect cellRect = t.OccupiedRect();
			for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (int j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					RegisterInCell(t, new IntVec3(j, 0, i));
				}
			}
		}

		//patched
		private void RegisterInCell(Thing t, IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				Log.Warning(string.Concat(t, " tried to register out of bounds at ", c, ". Destroying."));
				t.Destroy();
			}
			else
			{
				lock (thingGrid[map.cellIndices.CellToIndex(c)])
				{
					thingGrid[map.cellIndices.CellToIndex(c)].Add(t);
				}
			}
		}

		public void Deregister(Thing t, bool doEvenIfDespawned = false)
		{
			if (!t.Spawned && !doEvenIfDespawned)
			{
				return;
			}
			if (t.def.size.x == 1 && t.def.size.z == 1)
			{
				DeregisterInCell(t, t.Position);
				return;
			}
			CellRect cellRect = t.OccupiedRect();
			for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (int j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					DeregisterInCell(t, new IntVec3(j, 0, i));
				}
			}
		}

		//patched
		private void DeregisterInCell(Thing t, IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				Log.Error(string.Concat(t, " tried to de-register out of bounds at ", c));
				return;
			}
			int num = map.cellIndices.CellToIndex(c);
			lock (thingGrid[num])
			{
				if (thingGrid[num].Contains(t))
				{
					thingGrid[num].Remove(t);
				}
			}
		}

		//patched
		public IEnumerable<Thing> ThingsAt(IntVec3 c)
		{
			if (c.InBounds(map))
			{
				List<Thing> list = thingGrid[map.cellIndices.CellToIndex(c)];
				int i = 0;
				Thing thing;
				while (true)
				{
					try
					{
						thing = list[i];
					}
					catch (ArgumentOutOfRangeException) {break;}
					yield return thing;
					++i;
				}
			}
		}

		public List<Thing> ThingsListAt(IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				Log.ErrorOnce("Got ThingsListAt out of bounds: " + c, 495287);
				return EmptyThingList;
			}
			return thingGrid[map.cellIndices.CellToIndex(c)];
		}

		public List<Thing> ThingsListAtFast(IntVec3 c)
		{
			return thingGrid[map.cellIndices.CellToIndex(c)];
		}

		public List<Thing> ThingsListAtFast(int index)
		{
			return thingGrid[index];
		}

		public bool CellContains(IntVec3 c, ThingCategory cat)
		{
			return ThingAt(c, cat) != null;
		}

		//patched
		public Thing ThingAt(IntVec3 c, ThingCategory cat)
		{
			if (!c.InBounds(map))
			{
				return null;
			}
			Thing thing;
			List<Thing> list = thingGrid[map.cellIndices.CellToIndex(c)];
			for (int i = 0; i < list.Count; i++)
			{
				//thing = null;
				try
                {
					thing = list[i];
                }
				catch (ArgumentOutOfRangeException) { break; }
				//if (null == thing) break;
				if (list[i].def.category == cat)
				{
					return list[i];
				}				
			} 
			return null;
		}

		public bool CellContains(IntVec3 c, ThingDef def)
		{
			return ThingAt(c, def) != null;
		}

		//patched
		public Thing ThingAt(IntVec3 c, ThingDef def)
		{
			if (!c.InBounds(map))
			{
				return null;
			}
			Thing thing;
			List<Thing> list = thingGrid[map.cellIndices.CellToIndex(c)];
			for (int i = 0; i < list.Count; i++)
			{
				//thing = null;
				try
				{
					thing = list[i];
				}
				catch (ArgumentOutOfRangeException) { break; }
				//if (null == thing) break;
				if (list[i].def == def)
				{
					return list[i];
				}
			}
			return null;
		}

		//patched
		public T ThingAt<T>(IntVec3 c) where T : Thing
		{
			if (!c.InBounds(map))
			{
				return null;
			}
		Thing thing;
		List<Thing> list = thingGrid[map.cellIndices.CellToIndex(c)];
			for (int i = 0; i < list.Count; i++)
			{
				//thing = null;
				try
                {
					thing = list[i];
                }
				catch (ArgumentOutOfRangeException) { break; }
				//if (null == thing) break;
                if (thing is T val)
                {
                    return val;
                }
            } 
			return null;
		}
	}

}
