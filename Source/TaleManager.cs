using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class TaleManager_Patch
    {
        public static FieldRef<TaleManager, List<Tale>> tales = FieldRefAccess<TaleManager, List<Tale>>("tales");
        public static bool CheckCullUnusedVolatileTales(TaleManager __instance)
        {
            int num = 0;
            for (int i = 0; i < tales(__instance).Count; i++)
            {
                Tale tale1;
                try
                {
                    tale1 = tales(__instance)[i];
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (tale1 != null && tale1.def.type == TaleType.Volatile && tale1.Unused)
                {
                    num++;
                }
            }

            while (num > 350)
            {
                Tale tale = null;
                float num2 = float.MaxValue;
                for (int j = 0; j < tales(__instance).Count; j++)
                {
                    Tale tale2;
                    try
                    {
                        tale2 = tales(__instance)[j];
                    } catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    if (tale2 != null && tale2.def.type == TaleType.Volatile && tale2.Unused && tale2.InterestLevel < num2)
                    {
                        tale = tale2;
                        num2 = tale2.InterestLevel;
                    }
                    
                }

                RemoveTale(__instance, tale);
                num--;
            }
            return false;
        }
        public static void RemoveTale(TaleManager __instance, Tale tale)
        {
            if (!tale.Unused)
            {
                Log.Warning("Tried to remove used tale " + tale);
            }
            else
            {
                lock (tales(__instance))
                {
                    tales(__instance).Remove(tale);
                }
            }
        }


    }
}
