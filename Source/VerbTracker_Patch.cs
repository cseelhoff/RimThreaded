using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class VerbTracker_Patch
    {

        public static FieldRef<VerbTracker, List<Verb>> verbsFieldRef = FieldRefAccess<VerbTracker, List<Verb>>("verbs");
        public static bool VerbsTick(VerbTracker __instance)
        {
            List<Verb> verbs = verbsFieldRef(__instance);
            if (verbs != null)
            {
                for (int i = verbs.Count - 1; i >= 0; i--)
                {
                    Verb verb = null;
                    try
                    {
                        verb = verbs[i];
                    } catch (ArgumentOutOfRangeException)
                    {

                    }
                    if (verb != null && verb.state == VerbState.Bursting && verb.CurrentTarget == null)
                    {
                        //TODO: fix hack. prevent this from starting
                        Log.Warning("Removing verb that no longer has a target: " + verb.ToString());
                        verbs.RemoveAt(i);                        
                    }                 
                }
            }
            return true;
        }
    }
}
