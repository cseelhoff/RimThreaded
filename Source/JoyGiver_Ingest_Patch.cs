using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class JoyGiver_Ingest_Patch
    {
        [ThreadStatic]
        static List<Thing> tmpCandidates;
        static readonly MethodInfo methodCanIngestForJoy =
            Method(typeof(JoyGiver_Ingest), "CanIngestForJoy", new Type[] { typeof(Pawn), typeof(Thing) });
        static readonly Func<JoyGiver_Ingest, Pawn, Thing, bool> funcCanIngestForJoy =
            (Func<JoyGiver_Ingest, Pawn, Thing, bool>)Delegate.CreateDelegate(typeof(Func<JoyGiver_Ingest, Pawn, Thing, bool>), methodCanIngestForJoy);

        static readonly MethodInfo methodSearchSetWouldInclude =
            Method(typeof(JoyGiver_Ingest), "SearchSetWouldInclude", new Type[] { typeof(Thing) });
        static readonly Func<JoyGiver_Ingest, Thing, bool> funcSearchSetWouldInclude =
            (Func<JoyGiver_Ingest, Thing, bool>)Delegate.CreateDelegate(typeof(Func<JoyGiver_Ingest, Thing, bool>), methodSearchSetWouldInclude);

        static readonly MethodInfo methodGetSearchSet =
            Method(typeof(JoyGiver), "GetSearchSet", new Type[] { typeof(Pawn), typeof(List<Thing>) });
        static readonly Action<JoyGiver, Pawn, List<Thing>> actionGetSearchSet =
            (Action<JoyGiver, Pawn, List<Thing>>)Delegate.CreateDelegate(typeof(Action<JoyGiver, Pawn, List<Thing>>), methodGetSearchSet);

        public static bool BestIngestItem(JoyGiver_Ingest __instance, ref Thing __result, Pawn pawn, Predicate<Thing> extraValidator)
        {
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (!funcCanIngestForJoy(__instance, pawn, t))
                {
                    return false;
                }

                return (extraValidator == null || extraValidator(t)) ? true : false;
            };
            ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (funcSearchSetWouldInclude(__instance, innerContainer[i]) && predicate(innerContainer[i]))
                {
                    __result = innerContainer[i];
                    return false;
                }
            }
            if (tmpCandidates == null)
            {
                tmpCandidates = new List<Thing>();
            }
            else
            {
                tmpCandidates.Clear();
            }
            actionGetSearchSet(__instance, pawn, tmpCandidates);
            if (tmpCandidates.Count == 0)
            {
                __result = null;
                return false;
            }

            Thing result = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, predicate);
            //tmpCandidates.Clear();
            __result = result;
            return false;
        }

    }
}
