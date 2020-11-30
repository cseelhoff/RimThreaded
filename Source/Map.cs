using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;
using System.Threading;
using System.Diagnostics;

namespace RimThreaded
{

    public class Map_Patch
    {
        public static bool AlwaysRedrawShadows =
            AccessTools.StaticFieldRefAccess<bool>(typeof(Map), "AlwaysRedrawShadows");
        public static Dictionary<Map, AutoResetEvent> skyManagerStartEvents = new Dictionary<Map, AutoResetEvent>();


        public static void SkyManagerUpdate2(Map __instance)
        {
            if (!skyManagerStartEvents.TryGetValue(__instance, out AutoResetEvent skyManagerStartEvent))
            {
                skyManagerStartEvent = new AutoResetEvent(false);
                skyManagerStartEvents.Add(__instance, skyManagerStartEvent);
                new Thread(() =>
                {
                    AutoResetEvent skyManagerStartEvent2 = skyManagerStartEvents[__instance];
                    SkyManager skyManager = __instance.skyManager;
                    while (true)
                    {
                        skyManagerStartEvent2.WaitOne();
                        skyManager.SkyManagerUpdate();
                    }
                }).Start();
            }
            skyManagerStartEvent.Set();
        }
        public static bool get_IsPlayerHome(Map __instance, ref bool __result)
        {
            if (__instance.info != null && __instance.info.parent != null && __instance.info.parent.def != null && __instance.info.parent.def.canBePlayerHome)
            {
                __result = __instance.info.parent.Faction == Faction.OfPlayer;
                return false;
            }
            __result = false;
            return false;
            
        }

        public static bool MapUpdate(Map __instance)
        {
            
            
            SkyManagerUpdate2(__instance);
            __instance.powerNetManager.UpdatePowerNetsAndConnections_First();
            __instance.regionGrid.UpdateClean();
            __instance.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
            __instance.glowGrid.GlowGridUpdate_First();
            __instance.lordManager.LordManagerUpdate();
            
            bool worldRenderedNow = WorldRendererUtility.WorldRenderedNow;
            if (!worldRenderedNow && Find.CurrentMap == __instance)
            {

                if (AlwaysRedrawShadows)
                {
                __instance.mapDrawer.WholeMapChanged(MapMeshFlag.Things);
                }
                PlantFallColors.SetFallShaderGlobals(__instance);
                __instance.waterInfo.SetTextures();
                __instance.avoidGrid.DebugDrawOnMap();

                __instance.mapDrawer.MapMeshDrawerUpdate_First();
                __instance.powerNetGrid.DrawDebugPowerNetGrid();
                DoorsDebugDrawer.DrawDebug();
                __instance.mapDrawer.DrawMapMesh();
                __instance.dynamicDrawManager.DrawDynamicThings();

                __instance.gameConditionManager.GameConditionManagerDraw(__instance);
                MapEdgeClipDrawer.DrawClippers(__instance);
                __instance.designationManager.DrawDesignations();
                __instance.overlayDrawer.DrawAllOverlays();
                __instance.temporaryThingDrawer.Draw();
                
            }
            
            try
            {
                __instance.areaManager.AreaManagerUpdate();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            __instance.weatherManager.WeatherManagerUpdate();
            MapComponentUtility.MapComponentUpdate(__instance);
            
            return false;
        }

    }
}
