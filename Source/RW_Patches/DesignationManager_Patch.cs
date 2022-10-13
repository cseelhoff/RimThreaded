using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    class DesignationManager_Patch
    {
        internal static void RunDestructivePatches() //TODO: use better Add and Remove without locks
        {
            Type original = typeof(DesignationManager);
            Type patched = typeof(DesignationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveDesignation));
            //RimThreadedHarmony.Prefix(original, patched, nameof(RemoveAllDesignationsOn));
            //RimThreadedHarmony.Prefix(original, patched, nameof(RemoveAllDesignationsOfDef));
            //RimThreadedHarmony.Prefix(original, patched, nameof(AddDesignation));
            RimThreadedHarmony.Prefix(original, patched, nameof(IndexDesignation));
            //RimThreadedHarmony.Prefix(original, patched, nameof(SpawnedDesignationsOfDef));
            //RimThreadedHarmony.Prefix(original, patched, nameof(DesignationOn),
            //    new Type[] { typeof(Thing), typeof(DesignationDef) }, false); //weird CanBeBuried CanExtractSkull Ideology requirement is null
        }

        /*
        public static bool DesignationOn(DesignationManager __instance, ref Designation __result, Thing t, DesignationDef def)
        {
            if (def == null)  //weird CanBeBuried CanExtractSkull Ideology requirement is null
            {
                for (int index = 0; index < __instance.allDesignations.Count; ++index)
                {
                    Designation allDesignation = __instance.allDesignations[index];
                    if (allDesignation.target.Thing == t && allDesignation.def == def)
                    {
                        __result = allDesignation;
                        return false;
                    }
                }
                __result = null;
                return false;
            }
            return true;
        }
        */
        public static bool RemoveDesignation(DesignationManager __instance, Designation des)
        {
            des.Notify_Removing();
            if (des.def.targetType == TargetType.Cell)
            {
                if (__instance.TryGetCellDesignations(des.target.Cell, out var foundDesignations))
                {
                    foundDesignations.Remove(des);
                }
                else
                {
                    Log.Warning($"Tried to remove designation with target cell that couldn't be found in index: {des.target.Cell}");
                }
            }
            else if (des.def.targetType == TargetType.Thing)
            {
                Thing thing = des.target.Thing;
                Dictionary<Thing, List<Designation>> thingDesignations = __instance.thingDesignations;
                if (thingDesignations.ContainsKey(thing))
                {
                    List<Designation> list = thingDesignations[thing];
                    list.Remove(des);
                    if (list.Count == 0)
                    {
                        lock (thingDesignations) //added
                        {
                            thingDesignations.Remove(des.target.Thing);
                        }
                    }
                }
                else
                {
                    Log.Warning("Tried to remove thing designation that wasn't indexed");
                }
            }
            else
            {
                Log.Error($"Tried to remove designation with unexpected type: {des.def.targetType}");
            }

            List<Designation> designations = __instance.designationsByDef[des.def];
            lock (designations) //added
            {
                designations.Remove(des);
            }
            __instance.DirtyCellDesignationsCache(des.def);
            return false;
        }
        /*
        public static bool RemoveAllDesignationsOn(DesignationManager __instance, Thing t, bool standardCanceling = false)
        {
            bool matchFound = false;
            for (int index = 0; index < __instance.allDesignations.Count; ++index)
            {
                Designation designation = __instance.allDesignations[index];
                if ((!standardCanceling || designation.def.designateCancelable) && designation.target.Thing == t)
                {
                    designation.Notify_Removing();
                    matchFound = true;
                }
            }
            if (!matchFound) return false;
            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                newAllDesignations.RemoveAll(d => (!standardCanceling || d.def.designateCancelable) && d.target.Thing == t);
                __instance.allDesignations = newAllDesignations;
            }

            return false;
        }
        */

        /*
        public static bool RemoveAllDesignationsOfDef(DesignationManager __instance, DesignationDef def)
        {
            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                for (int index = newAllDesignations.Count - 1; index >= 0; --index)
                {
                    if (newAllDesignations[index].def != def) continue;

                    newAllDesignations[index].Notify_Removing();
                    newAllDesignations.RemoveAt(index);
                }
                __instance.allDesignations = newAllDesignations;
            }

            return false;
        }
        */
        /*
        public static bool AddDesignation(DesignationManager __instance, Designation newDes)
        {
            if (newDes.def.targetType == TargetType.Cell && __instance.DesignationAt(newDes.target.Cell, newDes.def) != null)
            {
                Log.Error("Tried to double-add designation at location " + newDes.target);
                return false;
            }

            if (newDes.def.targetType == TargetType.Thing && __instance.DesignationOn(newDes.target.Thing, newDes.def) != null)
            {
                Log.Error("Tried to double-add designation on Thing " + newDes.target);
                return false;
            }

            if (newDes.def.targetType == TargetType.Thing)
            {
                newDes.target.Thing.SetForbidden(value: false, warnOnFail: false);
            }

            __instance.IndexDesignation(newDes);
            newDes.designationManager = __instance;
            newDes.Notify_Added();
            Map map = (newDes.target.HasThing ? newDes.target.Thing.Map : __instance.map);
            if (map != null)
            {
                FleckMaker.ThrowMetaPuffs(newDes.target.ToTargetInfo(map));
            }
            return false;
        }
        */
        /*
        public static bool SpawnedDesignationsOfDef(DesignationManager __instance, ref IEnumerable<Designation> __result,
    DesignationDef def)
        {
            __result = SpawnedDesignationsOfDefEnumerable(__instance, def);
            return false;
        }
        */
        /*
        public static IEnumerable<Designation> SpawnedDesignationsOfDefEnumerable(DesignationManager __instance,
            DesignationDef def)
        {
            List<Designation> allDesignationsSnapshot = __instance.allDesignations;
            int count = allDesignationsSnapshot.Count;
            for (int i = 0; i < count; ++i)
            {
                Designation allDesignation = allDesignationsSnapshot[i];
                if (allDesignation.def == def && (!allDesignation.target.HasThing || allDesignation.target.Thing.Map == __instance.map))
                    yield return allDesignation;
            }
        }
        */
        public static bool IndexDesignation(DesignationManager __instance, Designation designation)
        {
            List<Designation> designations = __instance.designationsByDef[designation.def];
            lock (designations)
            {
                designations.Add(designation);
            }
            __instance.DirtyCellDesignationsCache(designation.def);
            if (designation.def.targetType == TargetType.Thing)
            {
                Thing thing = designation.target.Thing;
                Dictionary<Thing, List<Designation>> thingDesignations = __instance.thingDesignations;
                lock (thingDesignations) {
                    if (!__instance.thingDesignations.ContainsKey(thing))
                    {
                        __instance.thingDesignations[thing] = new List<Designation>();
                    }
                    thingDesignations[thing].Add(designation);
                }
            }
            else if (designation.def.targetType == TargetType.Cell)
            {
                IntVec3 cell = designation.target.Cell;
                Map map = __instance.map; //added
                int num = map.cellIndices.CellToIndex(cell);
                List<Designation>[] designationsAtCell = __instance.designationsAtCell;
                lock (designationsAtCell)
                {
                    if (num >= 0 && num < designationsAtCell.Length)
                    {
                        List<Designation> foundDesignations = designationsAtCell[num];
                        if (foundDesignations == null)
                        {
                            foundDesignations = new List<Designation>
                            {
                                designation
                            };
                            designationsAtCell[num] = foundDesignations;
                        }
                        else
                        {
                            //lock (foundDesignations)
                            //{
                                foundDesignations.Add(designation);
                            //}
                        }
                    }
                    else
                    {
                        Log.Error($"Tried to create cell target designation at invalid cell: {designation.target.Cell}");
                    }
                }
            }
            else
            {
                Log.Error($"Tried to index unexpected designation type: {designation.def.targetType}");
            }
            return false;
        }
    }
}
