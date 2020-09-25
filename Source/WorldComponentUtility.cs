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
