using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class DesignationManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(DesignationManager);
            Type patched = typeof(DesignationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveDesignation));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveAllDesignationsOn));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveAllDesignationsOfDef));
#if RW13
            RimThreadedHarmony.Prefix(original, patched, nameof(AddDesignation));
#endif
            RimThreadedHarmony.Prefix(original, patched, nameof(SpawnedDesignationsOfDef));
            RimThreadedHarmony.Prefix(original, patched, nameof(DesignationOn), 
                new Type[] { typeof(Thing), typeof(DesignationDef) }, false); //weird CanBeBuried CanExtractSkull Ideology requirement is null
        }

        public static bool DesignationOn(DesignationManager __instance, ref Designation __result, Thing t, DesignationDef def)
        {
            if(def == null)  //weird CanBeBuried CanExtractSkull Ideology requirement is null
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

        public static bool RemoveDesignation(DesignationManager __instance, Designation des)
        {
            des.Notify_Removing();
            if (!__instance.allDesignations.Contains(des)) return false;

            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                newAllDesignations.Remove(des);
                __instance.allDesignations = newAllDesignations;
            }
            return false;
        }
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

#if RW13
        public static bool AddDesignation(DesignationManager __instance, Designation newDes)
        {
            if (newDes.def.targetType == TargetType.Cell && __instance.DesignationAt(newDes.target.Cell, newDes.def) != null)
                Log.Error("Tried to double-add designation at location " + newDes.target);
            else if (newDes.def.targetType == TargetType.Thing && __instance.DesignationOn(newDes.target.Thing, newDes.def) != null)
            {
                Log.Error("Tried to double-add designation on Thing " + newDes.target);
            }
            else
            {
                if (newDes.def.targetType == TargetType.Thing)
                    newDes.target.Thing.SetForbidden(false, false);
                lock (__instance)
                {
                    __instance.allDesignations.Add(newDes);
                }

                newDes.designationManager = __instance;
                newDes.Notify_Added();
                Map map = newDes.target.HasThing ? newDes.target.Thing.Map : __instance.map;
                if (map == null)
                    return false;
                FleckMaker.ThrowMetaPuffs(newDes.target.ToTargetInfo(map));
            }
            return false;
        }
#endif
        public static bool SpawnedDesignationsOfDef(DesignationManager __instance, ref IEnumerable<Designation> __result,
            DesignationDef def)
        {
            __result = SpawnedDesignationsOfDefEnumerable(__instance, def);
            return false;
        }

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
    }
}
