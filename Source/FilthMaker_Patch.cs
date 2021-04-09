﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class FilthMaker_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FilthMaker);
            Type patched = typeof(FilthMaker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryMakeFilth", new Type[] { typeof(IntVec3), typeof(Map), typeof(ThingDef), typeof(IEnumerable<string>), typeof(bool), typeof(FilthSourceFlags) });
        }

        [ThreadStatic] private static Filth filth;
        [ThreadStatic] private static List<Thing> thingList;
        [ThreadStatic] private static Thing thing;
        [ThreadStatic] private static IEnumerable<string> enumerable;


        public static bool TryMakeFilth(ref bool __result, IntVec3 c, Map map, ThingDef filthDef, IEnumerable<string> sources, bool shouldPropagate, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
        {
            filth = null;
            thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                try
                {
                    thing = thingList[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
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
                enumerable = sources?.ToList();
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
                            TryMakeFilth(ref tryMakeFilthResult, c2, map, filthDef, enumerable, shouldPropagate: false);
                            if (tryMakeFilthResult)
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }

                filth?.AddSources(enumerable);
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

            FilthMonitor2.Notify_FilthSpawned();
            __result = true;
            return false;
        }

    }
}