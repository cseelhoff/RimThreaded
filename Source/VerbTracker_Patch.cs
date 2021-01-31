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
                int i = 0;
                while (i < verbs.Count)
                {
                    Verb verb = verbs[i];
                    if (verb.state == VerbState.Bursting && verb.CurrentTarget == null)
                    {
                        //TODO: fix hack. prevent this from starting
                        Log.Warning("Removing verb that no longer has a target: " + verb.ToString());
                        verbs.RemoveAt(i);                        
                    } else
                    {
                        verbs[i].VerbTick();
                        i++;
                    }                    
                }
            }
            return false;
        }
    }
}
