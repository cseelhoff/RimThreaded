using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Verb_MeleeAttack_Patch
    {
        public static FieldRef<Verb, LocalTargetInfo> currentTargetFieldRef = FieldRefAccess<Verb, LocalTargetInfo>("currentTarget");

        static readonly MethodInfo methodIsTargetImmobile =
            Method(typeof(Verb_MeleeAttack), "IsTargetImmobile");
        static readonly Func<Verb_MeleeAttack, LocalTargetInfo, bool> funcIsTargetImmobile =
            (Func<Verb_MeleeAttack, LocalTargetInfo, bool>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, bool>), methodIsTargetImmobile);

        static readonly MethodInfo methodGetNonMissChance =
            Method(typeof(Verb_MeleeAttack), "GetNonMissChance");
        static readonly Func<Verb_MeleeAttack, LocalTargetInfo, float> funcGetNonMissChance =
            (Func<Verb_MeleeAttack, LocalTargetInfo, float>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, float>), methodGetNonMissChance);

        static readonly MethodInfo methodGetDodgeChance =
            Method(typeof(Verb_MeleeAttack), "GetDodgeChance");
        static readonly Func<Verb_MeleeAttack, LocalTargetInfo, float> funcGetDodgeChance =
            (Func<Verb_MeleeAttack, LocalTargetInfo, float>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, float>), methodGetDodgeChance);

        static readonly MethodInfo methodApplyMeleeDamageToTarget =
            Method(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget");
        static readonly Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult> funcApplyMeleeDamageToTarget =
            (Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>), methodApplyMeleeDamageToTarget);

        static readonly MethodInfo methodSoundHitPawn =
            Method(typeof(Verb_MeleeAttack), "SoundHitPawn");
        static readonly Func<Verb_MeleeAttack, SoundDef> funcSoundHitPawn =
            (Func<Verb_MeleeAttack, SoundDef>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, SoundDef>), methodSoundHitPawn);

        static readonly MethodInfo methodSoundHitBuilding =
            Method(typeof(Verb_MeleeAttack), "SoundHitBuilding");
        static readonly Func<Verb_MeleeAttack, SoundDef> funcSoundHitBuilding =
            (Func<Verb_MeleeAttack, SoundDef>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, SoundDef>), methodSoundHitBuilding);

        static readonly MethodInfo methodSoundMiss =
            Method(typeof(Verb_MeleeAttack), "SoundMiss");
        static readonly Func<Verb_MeleeAttack, SoundDef> funcSoundMiss =
            (Func<Verb_MeleeAttack, SoundDef>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, SoundDef>), methodSoundMiss);

        static readonly MethodInfo methodSoundDodge =
            Method(typeof(Verb_MeleeAttack), "SoundDodge");
        static readonly Func<Verb_MeleeAttack, Thing, SoundDef> funcSoundDodge =
            (Func<Verb_MeleeAttack, Thing, SoundDef>)Delegate.CreateDelegate(typeof(Func<Verb_MeleeAttack, Thing, SoundDef>), methodSoundDodge);

        public static bool TryCastShot(Verb_MeleeAttack __instance, ref bool __result)
        {
            Pawn casterPawn = __instance.CasterPawn;
            if (!casterPawn.Spawned)
            {
                return false;
            }

            if (casterPawn.stances.FullBodyBusy)
            {
                return false;
            }

            Thing thing = currentTargetFieldRef(__instance).Thing;
            if (!__instance.CanHitTarget(thing))
            {
                Log.Warning(string.Concat(casterPawn, " meleed ", thing, " from out of melee position."));
            }

            casterPawn.rotationTracker.Face(thing.DrawPos);
            if (!funcIsTargetImmobile(__instance, currentTargetFieldRef(__instance)) && casterPawn.skills != null)
            {
                casterPawn.skills.Learn(SkillDefOf.Melee, 200f * __instance.verbProps.AdjustedFullCycleTime(__instance, casterPawn));
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null && !pawn.Dead && (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || pawn.MentalStateDef != MentalStateDefOf.SocialFighting))
            {
                pawn.mindState.meleeThreat = casterPawn;
                pawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
            }

            Map map = thing.Map;
            Vector3 drawPos = thing.DrawPos;
            SoundDef soundDef;
            bool result;
            if (Rand.Chance(funcGetNonMissChance(__instance, thing)))
            {
                if (!Rand.Chance(funcGetDodgeChance(__instance, thing)))
                {
                    soundDef = ((thing.def.category != ThingCategory.Building) ? funcSoundHitPawn(__instance) : funcSoundHitBuilding(__instance));
                    if (__instance.verbProps.impactMote != null)
                    {
                        MoteMaker.MakeStaticMote(drawPos, map, __instance.verbProps.impactMote);
                    }

                    BattleLogEntry_MeleeCombat battleLogEntry_MeleeCombat = __instance.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, alwaysShow: true);
                    result = true;
                    DamageWorker.DamageResult damageResult = funcApplyMeleeDamageToTarget(__instance, currentTargetFieldRef(__instance));
                    if (damageResult.stunned && damageResult.parts.NullOrEmpty())
                    {
                        Find.BattleLog.RemoveEntry(battleLogEntry_MeleeCombat);
                    }
                    else
                    {
                        damageResult.AssociateWithLog(battleLogEntry_MeleeCombat);
                        if (damageResult.deflected)
                        {
                            battleLogEntry_MeleeCombat.RuleDef = __instance.maneuver.combatLogRulesDeflect;
                            battleLogEntry_MeleeCombat.alwaysShowInCompact = false;
                        }
                    }
                }
                else
                {
                    result = false;
                    soundDef = funcSoundDodge(__instance, thing);
                    MoteMaker.ThrowText(drawPos, map, "TextMote_Dodge".Translate(), 1.9f);
                    __instance.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDodge, alwaysShow: false);
                }
            }
            else
            {
                result = false;
                soundDef = funcSoundMiss(__instance);
                __instance.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, alwaysShow: false);
            }

            soundDef.PlayOneShot(new TargetInfo(thing.Position, map));
            if (casterPawn.Spawned)
            {
                casterPawn.Drawer.Notify_MeleeAttackOn(thing);
            }

            if (pawn != null && !pawn.Dead && pawn.Spawned)
            {
                pawn.stances.StaggerFor(95);
            }

            if (casterPawn.Spawned)
            {
                casterPawn.rotationTracker.FaceCell(thing.Position);
            }

            if (casterPawn.caller != null)
            {
                casterPawn.caller.Notify_DidMeleeAttack();
            }

            return result;
        }


    }
}
