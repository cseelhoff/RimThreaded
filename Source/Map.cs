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

namespace RimThreaded
{

    public class Map_Patch
    {
        public static bool AlwaysRedrawShadows =
            AccessTools.StaticFieldRefAccess<bool>(typeof(Map), "AlwaysRedrawShadows");
        public static Thread SkyManagerThread = null;
        public static AutoResetEvent SkyManagerStartEvent;
        public static AutoResetEvent SkyManagerDoneEvent;
        public static Map currentInstance = null;
        public static bool MapUpdate(Map __instance)
        {
            currentInstance = __instance;
            bool worldRenderedNow = WorldRendererUtility.WorldRenderedNow;
            if (null == SkyManagerThread)
            {
                SkyManagerThread = new Thread(() => SkyManagerTicks());
                SkyManagerStartEvent = new AutoResetEvent(false);
                SkyManagerDoneEvent = new AutoResetEvent(false);
                SkyManagerThread.Start();
            }
            SkyManagerStartEvent.Set();
            __instance.powerNetManager.UpdatePowerNetsAndConnections_First();
            __instance.regionGrid.UpdateClean();
            __instance.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
            __instance.glowGrid.GlowGridUpdate_First();
            __instance.lordManager.LordManagerUpdate();
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
        private static void SkyManagerTicks()
        {
            while (true)
            {
                SkyManagerStartEvent.WaitOne();
                currentInstance.skyManager.SkyManagerUpdate();
                SkyManagerDoneEvent.Set();
            }
        }


    }
}
