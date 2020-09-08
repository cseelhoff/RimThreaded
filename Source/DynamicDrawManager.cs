using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;

namespace RimThreaded
{
    
    public class Verse_DynamicDrawManager_Patch
    {
        public static ConcurrentDictionary<DynamicDrawManager, ConcurrentDictionary<Thing, Thing>> drawThings = new ConcurrentDictionary<DynamicDrawManager, ConcurrentDictionary<Thing, Thing>>();
        public static AccessTools.FieldRef<DynamicDrawManager, Map> map =
            AccessTools.FieldRefAccess<DynamicDrawManager, Map>("map");
        public static AccessTools.FieldRef<DynamicDrawManager, bool> drawingNow =
            AccessTools.FieldRefAccess<DynamicDrawManager, bool>("drawingNow");

        public static bool RegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType != DrawerType.None)
            {
                if (drawingNow(__instance))
                    Log.Warning("Cannot register drawable " + (object)t + " while drawing is in progress. Things shouldn't be spawned in Draw methods.", false);
                drawThings.GetOrAdd(__instance, new ConcurrentDictionary<Thing, Thing>()).TryAdd(t, t);
            }
            return false;
        }

        public static bool DeRegisterDrawable(DynamicDrawManager __instance, Thing t)
        {
            if (t.def.drawerType != DrawerType.None)
            {
                if (drawingNow(__instance))
                    Log.Warning("Cannot deregister drawable " + (object)t + " while drawing is in progress. Things shouldn't be despawned in Draw methods.", false);
                drawThings.GetOrAdd(__instance, new ConcurrentDictionary<Thing, Thing>()).TryRemove(t, out _);
            }
            return false;
        }

        public static bool DrawDynamicThings(DynamicDrawManager __instance)
        {
            if (!DebugViewSettings.drawThingsDynamic || null == map(__instance))
                return false;
            drawingNow(__instance) = true;
            try
            {
                bool[] fogGrid = map(__instance).fogGrid.fogGrid;
                CellRect cellRect = Find.CameraDriver.CurrentViewRect;
                cellRect.ClipInsideMap(map(__instance));
                cellRect = cellRect.ExpandedBy(1);
                CellIndices cellIndices = map(__instance).cellIndices;
                foreach (Thing drawThing in drawThings.GetOrAdd(__instance, new ConcurrentDictionary<Thing, Thing>()).Values)
                {
                    IntVec3 position = drawThing.Position;
                    if ((cellRect.Contains(position) || drawThing.def.drawOffscreen) && (!fogGrid[cellIndices.CellToIndex(position)] || drawThing.def.seeThroughFog) && ((double)drawThing.def.hideAtSnowDepth >= 1.0 || (double)map(__instance).snowGrid.GetDepth(position) <= (double)drawThing.def.hideAtSnowDepth))
                    {
                        try
                        {
                            drawThing.Draw();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception drawing " + (object)drawThing + ": " + ex.ToString(), false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception drawing dynamic things: " + (object)ex, false);
            }
            drawingNow(__instance) = false;
            return false;
        }

        public static bool LogDynamicDrawThings(DynamicDrawManager __instance)
        {
            Log.Message(DebugLogsUtility.ThingListToUniqueCountString((IEnumerable<Thing>)drawThings.Values), false);
            return false;
        }

    }

}
