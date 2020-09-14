using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using UnityEngine;

namespace RimThreaded
{

    public class LordToil_Siege_Patch
	{
        private static void SetAsDefender(LordToilData_Siege data, Pawn p)
        {
            //LordToilData_Siege data = __instance.Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.siegeCenter, -1f);
            p.mindState.duty.radius = data.baseRadius;
        }

        private static void SetAsBuilder(LordToilData_Siege data, Pawn p)
        {
            //LordToilData_Siege data = this.Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Build, data.siegeCenter, -1f);
            p.mindState.duty.radius = data.baseRadius;
            int minLevel = Mathf.Max(ThingDefOf.Sandbags.constructionSkillPrerequisite, ThingDefOf.Turret_Mortar.constructionSkillPrerequisite);
            p.skills.GetSkill(SkillDefOf.Construction).EnsureMinLevelWithMargin(minLevel);
            p.workSettings.EnableAndInitialize();
            List<WorkTypeDef> defsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            for (int index = 0; index < defsListForReading.Count; ++index)
            {
                WorkTypeDef w = defsListForReading[index];
                if (w == WorkTypeDefOf.Construction)
                    p.workSettings.SetPriority(w, 1);
                else
                    p.workSettings.Disable(w);
            }
        }
        public static bool UpdateAllDuties(LordToil_Siege __instance)
        {
            LordToil lordToil = __instance;
            LordToilData_Siege data = (LordToilData_Siege)lordToil.data;
            if (__instance.lord.ticksInToil < 450)
            {
                for (int index = 0; index < __instance.lord.ownedPawns.Count; ++index)
                    SetAsDefender(data, __instance.lord.ownedPawns[index]);
            }
            else
            {
                __instance.rememberedDuties.Clear();
                int num1 = Mathf.RoundToInt((float)__instance.lord.ownedPawns.Count * data.desiredBuilderFraction);
                if (num1 <= 0)
                    num1 = 1;
                int num2 = __instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Where<Thing>((Func<Thing, bool>)(b => b.def.hasInteractionCell && b.Faction == __instance.lord.faction && b.Position.InHorDistOf(__instance.FlagLoc, data.baseRadius))).Count<Thing>();
                if (num1 < num2)
                    num1 = num2;
                int num3 = 0;
                for (int index = 0; index < __instance.lord.ownedPawns.Count; ++index)
                {
                    Pawn ownedPawn = __instance.lord.ownedPawns[index];
                    if (ownedPawn.mindState.duty.def == DutyDefOf.Build)
                    {
                        __instance.rememberedDuties.Add(ownedPawn, DutyDefOf.Build);
                        SetAsBuilder(data, ownedPawn);
                        ++num3;
                    }
                }
                int num4 = num1 - num3;
                for (int index = 0; index < num4; ++index)
                {
                    Pawn result;
                    if (__instance.lord.ownedPawns.Where<Pawn>((Func<Pawn, bool>)(pa => !__instance.rememberedDuties.ContainsKey(pa) && CanBeBuilder(pa))).TryRandomElement<Pawn>(out result))
                    {
                        __instance.rememberedDuties.Add(result, DutyDefOf.Build);
                        SetAsBuilder(data, result);
                        ++num3;
                    }
                }
                for (int index = 0; index < __instance.lord.ownedPawns.Count; ++index)
                {
                    Pawn ownedPawn = __instance.lord.ownedPawns[index];
                    if (!__instance.rememberedDuties.ContainsKey(ownedPawn))
                    {
                        SetAsDefender(data, ownedPawn);
                        __instance.rememberedDuties.Add(ownedPawn, DutyDefOf.Defend);
                    }
                }
                if (num3 != 0)
                    return false;
                __instance.lord.ReceiveMemo("NoBuilders");
            }
            return false;
        }
        private static bool CanBeBuilder(Pawn p)
        {
            return !p.WorkTypeIsDisabled(WorkTypeDefOf.Construction) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter);
        }
    }
}
