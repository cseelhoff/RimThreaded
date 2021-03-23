using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class GenLeaving_Patch
    {
        [ThreadStatic]
        private static List<IntVec3> tmpCellsCandidates;

        public static bool DropFilthDueToDamage(Thing t, float damageDealt)
        {
            if (!t.def.useHitPoints || !t.Spawned || t.def.filthLeaving == null)
            {
                return false;
            }

            CellRect cellRect = t.OccupiedRect().ExpandedBy(1);
            if (tmpCellsCandidates == null)
            {
                tmpCellsCandidates = new List<IntVec3>();
            }
            else
            {
                tmpCellsCandidates.Clear();
            }
            foreach (IntVec3 item in cellRect)
            {
                if (item.InBounds(t.Map) && item.Walkable(t.Map))
                {
                    tmpCellsCandidates.Add(item);
                }
            }

            if (tmpCellsCandidates.Any())
            {
                int num = GenMath.RoundRandom(damageDealt * Mathf.Min(0.0166666675f, 1f / (t.MaxHitPoints / 10f)));
                for (int i = 0; i < num; i++)
                {
                    FilthMaker.TryMakeFilth(tmpCellsCandidates.RandomElement(), t.Map, t.def.filthLeaving);
                }

                //tmpCellsCandidates.Clear();
            }
            return false;
        }
    }
}
