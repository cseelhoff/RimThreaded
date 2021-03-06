﻿using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class FilthMaker_Patch
    {
        public static readonly Type original = typeof(FilthMaker);
        public static readonly Type patched = typeof(FilthMaker_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "TryMakeFilth", new Type[] { typeof(IntVec3), typeof(Map), typeof(ThingDef), typeof(IEnumerable<string>), typeof(bool), typeof(FilthSourceFlags) });
        }

        public static bool TryMakeFilth(ref bool __result, IntVec3 c, Map map, ThingDef filthDef, IEnumerable<string> sources, bool shouldPropagate, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
        {
            Filth filth = null;
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing != null && thing.def == filthDef)
                {
                    filth = (Filth)thing;
                    break;
                }
            }
            if (filth == null)
            {
                filth = (Filth)default(Thing);
            }

            if (!c.Walkable(map) || (filth != null && !filth.CanBeThickened))
            {
                if (shouldPropagate)
                {
                    List<IntVec3> list = GenAdj.AdjacentCells8WayRandomized();
                    for (int i = 0; i < 8; i++)
                    {
                        IntVec3 c2 = c + list[i];
                        //if (c2.InBounds(map) && TryMakeFilth(c2, map, filthDef, sources, shouldPropagate: false))
                        if (c2.InBounds(map))
                        {
                            bool tryMakeFilthResult = false;
                            TryMakeFilth(ref tryMakeFilthResult, c2, map, filthDef, sources, shouldPropagate: false);
                            if (tryMakeFilthResult)
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }

                filth?.AddSources(sources);
                __result = false;
                return false;
            }

            if (filth != null)
            {
                filth.ThickenFilth();
                filth.AddSources(sources);
            }
            else
            {
                if (!FilthMaker.CanMakeFilth(c, map, filthDef, additionalFlags))
                {
                    __result = false;
                    return false;
                }

                Filth obj = (Filth)ThingMaker.MakeThing(filthDef);
                obj.AddSources(sources);
                GenSpawn.Spawn(obj, c, map);
            }

            FilthMonitor.Notify_FilthSpawned();
            __result = true;
            return false;
        }


    }
}