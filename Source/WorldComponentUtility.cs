using RimWorld.Planet;

namespace RimThreaded
{

    public class WorldComponentUtility_Patch
	{
        public static bool WorldComponentTick(World world)
        {
            RimThreaded.WorldComponents = world.components;
            RimThreaded.WorldComponentTicks = world.components.Count;
            return false;
        }



    }
}
