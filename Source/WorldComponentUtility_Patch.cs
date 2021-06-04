using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded
{
    //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
    public class WorldComponentUtility_Patch
	{
        public static bool WorldComponentTick(World world)
        {
            worldComponents = world.components;
            worldComponentTicks = worldComponents.Count;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WorldComponentUtility);
            Type patched = typeof(WorldComponentUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldComponentTick");
        }

        public static List<WorldComponent> worldComponents;
        public static int worldComponentTicks;

        public static void WorldComponentPrepare()
        {
            try
            {
                World world = Find.World; 
                world.debugDrawer.WorldDebugDrawerTick();
                world.pathGrid.WorldPathGridTick();
                WorldComponentUtility.WorldComponentTick(world);
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void WorldComponentListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref worldComponentTicks);
                if (index < 0) return;
                WorldComponent worldComponent = worldComponents[index];
                if (null != worldComponent) //TODO: is null-check and lock necessary?
                {
                    try
                    {
                        worldComponent.WorldComponentTick();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception ticking World Component: " + worldComponent.ToStringSafe() + ex);
                    }
                }
            }
        }
    }
}
