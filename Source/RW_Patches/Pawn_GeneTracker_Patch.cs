using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Pawn_GeneTracker_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_GeneTracker);
            Type patched = typeof(Pawn_GeneTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(FactorForDamage));
        }
        public static bool FactorForDamage(Pawn_GeneTracker __instance, ref float __result, DamageInfo dinfo)
        {
            if (!ModLister.BiotechInstalled || dinfo.Def == null || __instance.GenesListForReading.NullOrEmpty())
            {
                __result = 1f;
                return false;
            }
            Dictionary<DamageDef, float> cachedDamageFactors = __instance.cachedDamageFactors;
            lock (cachedDamageFactors)
            {
                if (cachedDamageFactors.TryGetValue(dinfo.Def, out var value))
                {
                    __result = value;
                    return false;
                }
                float num = 1f;
                for (int i = 0; i < __instance.GenesListForReading.Count; i++)
                {
                    if (__instance.GenesListForReading[i].def.damageFactors.NullOrEmpty())
                    {
                        continue;
                    }
                    for (int j = 0; j < __instance.GenesListForReading[i].def.damageFactors.Count; j++)
                    {
                        if (__instance.GenesListForReading[i].def.damageFactors[j].damageDef == dinfo.Def)
                        {
                            num *= __instance.GenesListForReading[i].def.damageFactors[j].factor;
                        }
                    }
                }
                cachedDamageFactors.Add(dinfo.Def, num);
                __result = num;
                return false;
            }
        }
    }
}
