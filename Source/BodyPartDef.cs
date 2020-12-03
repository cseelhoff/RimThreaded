using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class BodyPartDef_Patch
    {
        public static bool IsSolid(BodyPartDef __instance, ref bool __result, BodyPartRecord part, List<Hediff> hediffs)
        {
            for (BodyPartRecord bodyPartRecord = part; bodyPartRecord != null; bodyPartRecord = bodyPartRecord.parent)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    Hediff hediff;
                    try
                    {
                        hediff = hediffs[i];
                    } catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    if (hediff != null && hediff.Part == bodyPartRecord && hediff is Hediff_AddedPart)
                    {
                        __result = hediff.def.addedPartProps.solid;
                        return false;
                    }
                }
            }

            __result = __instance.IsSolidInDefinition_Debug;
            return false;
        }


    }
}
