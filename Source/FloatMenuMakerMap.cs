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
using RimWorld.Planet;

namespace RimThreaded
{

    public class FloatMenuMakerMap_Patch
	{
        public static bool AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            foreach (Thing thing in c.GetThingList(pawn.Map))
            {
                Pawn pawn2;
                if ((pawn2 = (thing as Pawn)) != null)
                {
                    Lord lord = pawn2.GetLord();
                    if (lord != null && lord.CurLordToil != null)
                    {
                        IEnumerable<FloatMenuOption> enumerable = lord.CurLordToil.ExtraFloatMenuOptions(pawn2, pawn);
                        if (enumerable != null)
                        {
                            foreach (FloatMenuOption item8 in enumerable)
                            {
                                opts.Add(item8);
                            }
                        }
                    }
                }
            }

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (LocalTargetInfo item9 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
                {
                    bool flag = item9.HasThing && item9.Thing is Pawn && ((Pawn)item9.Thing).IsWildMan();
                    if (pawn.Drafted || flag)
                    {
                        if (item9.Thing is Pawn && (pawn.InSameExtraFaction((Pawn)item9.Thing, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction((Pawn)item9.Thing, ExtraFactionType.MiniFaction)))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "SameFaction".Translate((Pawn)item9.Thing), null));
                        }
                        else if (!pawn.CanReach(item9, PathEndMode.OnCell, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            Pawn pTarg2 = (Pawn)item9.Thing;
                            Action action = delegate
                            {
                                Building_Bed building_Bed3 = RestUtility.FindBedFor(pTarg2, pawn, sleeperWillBePrisoner: true, checkSocialProperness: false);
                                if (building_Bed3 == null)
                                {
                                    building_Bed3 = RestUtility.FindBedFor(pTarg2, pawn, sleeperWillBePrisoner: true, checkSocialProperness: false, ignoreOtherReservations: true);
                                }

                                if (building_Bed3 == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg2, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                else
                                {
                                    Job job19 = JobMaker.MakeJob(JobDefOf.Arrest, pTarg2, building_Bed3);
                                    job19.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job19);
                                    if (pTarg2.Faction != null && ((pTarg2.Faction != Faction.OfPlayer && !pTarg2.Faction.Hidden) || pTarg2.IsQuestLodger()))
                                    {
                                        TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg2.GetAcceptArrestChance(pawn).ToStringPercent());
                                    }
                                }
                            };
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TryToArrest".Translate(item9.Thing.LabelCap, item9.Thing, pTarg2.GetAcceptArrestChance(pawn).ToStringPercent()), action, MenuOptionPriority.High, null, item9.Thing), pawn, pTarg2));
                        }
                    }
                }
            }

            foreach (Thing thing2 in c.GetThingList(pawn.Map))
            {
                Thing t = thing2;
                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                {
                    string text = (!t.def.ingestible.ingestCommandString.NullOrEmpty()) ? string.Format(t.def.ingestible.ingestCommandString, t.LabelShort) : ((string)"ConsumeThing".Translate(t.LabelShort, t));
                    if (!t.IsSociallyProper(pawn))
                    {
                        text = text + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
                    }

                    FloatMenuOption floatMenuOption;
                    if (t.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                    {
                        floatMenuOption = new FloatMenuOption(text + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).GetLabelCapFor(pawn), null);
                    }
                    else if (FoodUtility.InappropriateForTitle(t.def, pawn, allowIfStarving: true))
                    {
                        floatMenuOption = new FloatMenuOption(text + ": " + "FoodBelowTitleRequirements".Translate(pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn)), null);
                    }
                    else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
                    {
                        floatMenuOption = new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                    }
                    else
                    {
                        MenuOptionPriority priority = (t is Corpse) ? MenuOptionPriority.Low : MenuOptionPriority.Default;
                        int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                        floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
                        {
                            int maxAmountToPickup2 = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                            if (maxAmountToPickup2 != 0)
                            {
                                t.SetForbidden(value: false);
                                Job job18 = JobMaker.MakeJob(JobDefOf.Ingest, t);
                                job18.count = maxAmountToPickup2;
                                pawn.jobs.TryTakeOrderedJob(job18);
                            }
                        }, priority), pawn, t);
                        if (maxAmountToPickup == 0)
                        {
                            floatMenuOption.action = null;
                        }
                    }

                    opts.Add(floatMenuOption);
                }
            }

            foreach (LocalTargetInfo item10 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForQuestPawnsWhoWillJoinColony(pawn), thingsOnly: true))
            {
                Pawn toHelpPawn = (Pawn)item10.Thing;
                FloatMenuOption item4 = pawn.CanReach(item10, PathEndMode.Touch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(toHelpPawn.IsPrisoner ? "FreePrisoner".Translate() : "OfferHelp".Translate(), delegate
                {
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.OfferHelp, toHelpPawn));
                }, MenuOptionPriority.RescueOrCapture, null, toHelpPawn), pawn, toHelpPawn) : new FloatMenuOption("CannotGoNoPath".Translate(), null);
                opts.Add(item4);
            }

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (Thing thing3 in c.GetThingList(pawn.Map))
                {
                    Corpse corpse = thing3 as Corpse;
                    if (corpse != null && corpse.IsInValidStorage())
                    {
                        StoragePriority priority2 = StoreUtility.CurrentHaulDestinationOf(corpse).GetStoreSettings().Priority;
                        if (StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(corpse, pawn, pawn.Map, priority2, Faction.OfPlayer, out IHaulDestination haulDestination, acceptSamePriority: true) && haulDestination.GetStoreSettings().Priority == priority2 && haulDestination is Building_Grave)
                        {
                            Building_Grave grave = haulDestination as Building_Grave;
                            string label = "PrioritizeGeneric".Translate("Burying".Translate(), corpse.Label);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
                            {
                                pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, grave));
                            }), pawn, new LocalTargetInfo(corpse)));
                        }
                    }
                }

                foreach (LocalTargetInfo item11 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                {
                    Pawn victim3 = (Pawn)item11.Thing;
                    if (!victim3.InBed() && pawn.CanReserveAndReach(victim3, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) && !victim3.mindState.WillJoinColonyIfRescued)
                    {
                        if (!victim3.IsPrisonerOfColony && (!victim3.InMentalState || victim3.health.hediffSet.HasHediff(HediffDefOf.Scaria)) && (victim3.Faction == Faction.OfPlayer || victim3.Faction == null || !victim3.Faction.HostileTo(Faction.OfPlayer)))
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate(victim3.LabelCap, victim3), delegate
                            {
                                Building_Bed building_Bed2 = RestUtility.FindBedFor(victim3, pawn, sleeperWillBePrisoner: false, checkSocialProperness: false);
                                if (building_Bed2 == null)
                                {
                                    building_Bed2 = RestUtility.FindBedFor(victim3, pawn, sleeperWillBePrisoner: false, checkSocialProperness: false, ignoreOtherReservations: true);
                                }

                                if (building_Bed2 == null)
                                {
                                    string t3 = (!victim3.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate());
                                    Messages.Message("CannotRescue".Translate() + ": " + t3, victim3, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                else
                                {
                                    Job job17 = JobMaker.MakeJob(JobDefOf.Rescue, victim3, building_Bed2);
                                    job17.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job17);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim3), pawn, victim3));
                        }

                        if (victim3.RaceProps.Humanlike && (victim3.InMentalState || victim3.Faction != Faction.OfPlayer || (victim3.Downed && (victim3.guilt.IsGuilty || victim3.IsPrisonerOfColony))))
                        {
                            TaggedString taggedString = "Capture".Translate(victim3.LabelCap, victim3);
                            if (victim3.Faction != null && victim3.Faction != Faction.OfPlayer && !victim3.Faction.Hidden && !victim3.Faction.HostileTo(Faction.OfPlayer) && !victim3.IsPrisonerOfColony)
                            {
                                taggedString += ": " + "AngersFaction".Translate().CapitalizeFirst();
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim3, pawn, sleeperWillBePrisoner: true, checkSocialProperness: false);
                                if (building_Bed == null)
                                {
                                    building_Bed = RestUtility.FindBedFor(victim3, pawn, sleeperWillBePrisoner: true, checkSocialProperness: false, ignoreOtherReservations: true);
                                }

                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim3, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                else
                                {
                                    Job job16 = JobMaker.MakeJob(JobDefOf.Capture, victim3, building_Bed);
                                    job16.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job16);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                                    if (victim3.Faction != null && victim3.Faction != Faction.OfPlayer && !victim3.Faction.Hidden && !victim3.Faction.HostileTo(Faction.OfPlayer) && !victim3.IsPrisonerOfColony)
                                    {
                                        Messages.Message("MessageCapturingWillAngerFaction".Translate(victim3.Named("PAWN")).AdjustedFor(victim3), victim3, MessageTypeDefOf.CautionInput, historical: false);
                                    }
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim3), pawn, victim3));
                        }
                    }
                }

                foreach (LocalTargetInfo item12 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                {
                    LocalTargetInfo localTargetInfo = item12;
                    Pawn victim2 = (Pawn)localTargetInfo.Thing;
                    if (victim2.Downed && pawn.CanReserveAndReach(victim2, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) && Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, ignoreOtherReservations: true) != null)
                    {
                        string text2 = "CarryToCryptosleepCasket".Translate(localTargetInfo.Thing.LabelCap, localTargetInfo.Thing);
                        JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
                        Action action2 = delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, ignoreOtherReservations: true);
                            }

                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim2, MessageTypeDefOf.RejectInput, historical: false);
                            }
                            else
                            {
                                Job job15 = JobMaker.MakeJob(jDef, victim2, building_CryptosleepCasket);
                                job15.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job15);
                            }
                        };
                        if (victim2.IsQuestLodger())
                        {
                            text2 += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
                        }
                        else if (victim2.GetExtraHostFaction() != null)
                        {
                            text2 += " (" + "CryptosleepCasketGuestPrisonersNotAllowed".Translate() + ")";
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
                        }
                        else
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, action2, MenuOptionPriority.Default, null, victim2), pawn, victim2));
                        }
                    }
                }

                if (ModsConfig.RoyaltyActive)
                {
                    foreach (LocalTargetInfo item13 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForShuttle(pawn), thingsOnly: true))
                    {
                        LocalTargetInfo localTargetInfo2 = item13;
                        Pawn victim = (Pawn)localTargetInfo2.Thing;
                        Predicate<Thing> validator = (Thing thing) => thing.TryGetComp<CompShuttle>()?.IsAllowedNow(victim) ?? false;
                        Thing shuttleThing = GenClosest.ClosestThingReachable(victim.Position, victim.Map, ThingRequest.ForDef(ThingDefOf.Shuttle), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
                        if (shuttleThing != null && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                        {
                            string label2 = "CarryToShuttle".Translate(localTargetInfo2.Thing);
                            Action action3 = delegate
                            {
                                CompShuttle compShuttle = shuttleThing.TryGetComp<CompShuttle>();
                                if (!compShuttle.LoadingInProgressOrReadyToLaunch)
                                {
                                    TransporterUtility.InitiateLoading(Gen.YieldSingle(compShuttle.Transporter));
                                }

                                Job job14 = JobMaker.MakeJob(JobDefOf.HaulToTransporter, victim, shuttleThing);
                                job14.ignoreForbidden = true;
                                job14.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job14);
                            };
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label2, action3), pawn, victim));
                        }
                    }
                }
            }

            foreach (LocalTargetInfo item14 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true))
            {
                LocalTargetInfo stripTarg = item14;
                FloatMenuOption item5 = pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly) ? ((stripTarg.Pawn == null || !stripTarg.Pawn.HasExtraHomeFaction()) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Strip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing), delegate
                {
                    stripTarg.Thing.SetForbidden(value: false, warnOnFail: false);
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, stripTarg));
                    StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarg.Thing);
                }), pawn, stripTarg) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null)) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                opts.Add(item5);
            }

            ThingWithComps equipment;
            if (pawn.equipment != null)
            {
                equipment = null;
                List<Thing> thingList = c.GetThingList(pawn.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].TryGetComp<CompEquippable>() != null)
                    {
                        equipment = (ThingWithComps)thingList[i];
                        break;
                    }
                }

                if (equipment != null)
                {
                    string labelShort = equipment.LabelShort;
                    FloatMenuOption item6;
                    string cantReason;
                    if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
                    }
                    else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate(), null);
                    }
                    else if (equipment.IsBurning())
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
                    }
                    else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
                    }
                    else if (!EquipmentUtility.CanEquip_NewTmp(equipment, pawn, out cantReason, checkBonded: false))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
                    }
                    else
                    {
                        string text3 = "Equip".Translate(labelShort);
                        if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                        {
                            text3 += " " + "EquipWarningBrawler".Translate();
                        }

                        if (EquipmentUtility.AlreadyBondedToWeapon(equipment, pawn))
                        {
                            text3 += " " + "BladelinkAlreadyBonded".Translate();
                            TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"), equipment.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
                            item6 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, delegate
                            {
                                Find.WindowStack.Add(new Dialog_MessageBox(dialogText));
                            }, MenuOptionPriority.High), pawn, equipment);
                        }
                        else
                        {
                            item6 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, delegate
                            {
                                string personaWeaponConfirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(equipment, pawn);
                                if (!personaWeaponConfirmationText.NullOrEmpty())
                                {
                                    Find.WindowStack.Add(new Dialog_MessageBox(personaWeaponConfirmationText, "Yes".Translate(), delegate
                                    {
                                        Equip();
                                    }, "No".Translate()));
                                }
                                else
                                {
                                    Equip();
                                }
                            }, MenuOptionPriority.High), pawn, equipment);
                        }
                    }

                    opts.Add(item6);
                }
            }

            foreach (Pair<CompReloadable, Thing> item15 in ReloadableUtility.FindPotentiallyReloadableGear(pawn, c.GetThingList(pawn.Map)))
            {
                CompReloadable comp = item15.First;
                Thing second = item15.Second;
                string text4 = "Reload".Translate(comp.parent.Named("GEAR"), NamedArgumentUtility.Named(comp.AmmoDef, "AMMO")) + " (" + comp.LabelRemaining + ")";
                List<Thing> chosenAmmo;
                if (!pawn.CanReach(second, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(text4 + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else if (!comp.NeedsReload(allowForcedReload: true))
                {
                    opts.Add(new FloatMenuOption(text4 + ": " + "ReloadFull".Translate(), null));
                }
                else if ((chosenAmmo = ReloadableUtility.FindEnoughAmmo(pawn, second.Position, comp, forceReload: true)) == null)
                {
                    opts.Add(new FloatMenuOption(text4 + ": " + "ReloadNotEnough".Translate(), null));
                }
                else
                {
                    Action action4 = delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(comp, chosenAmmo));
                    };
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text4, action4), pawn, second));
                }
            }

            if (pawn.apparel != null)
            {
                if (pawn.Map.thingGrid.ThingAt(c, ThingCategory.Item) is Apparel apparel)
                {
                    string key = "CannotWear";
                    string key2 = "ForceWear";
                    if (apparel.def.apparel.LastLayer.IsUtilityLayer)
                    {
                        key = "CannotEquipApparel";
                        key2 = "ForceEquipApparel";
                    }

                    string cantReason2;
                    FloatMenuOption item7 = (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly)) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (apparel.IsBurning() ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "Burning".Translate(), null) : (pawn.apparel.WouldReplaceLockedApparel(apparel) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "WouldReplaceLockedApparel".Translate().CapitalizeFirst(), null) : ((!ApparelUtility.HasPartsToWear(pawn, apparel.def)) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "CannotWearBecauseOfMissingBodyParts".Translate(), null) : (EquipmentUtility.CanEquip_NewTmp(apparel, pawn, out cantReason2) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(key2.Translate(apparel.LabelShort, apparel), delegate
                    {
                        apparel.SetForbidden(value: false);
                        Job job13 = JobMaker.MakeJob(JobDefOf.Wear, apparel);
                        pawn.jobs.TryTakeOrderedJob(job13);
                    }, MenuOptionPriority.High), pawn, apparel) : new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + cantReason2, null)))));
                    opts.Add(item7);
                }
            }

            if (pawn.IsFormingCaravan())
            {
                Thing item3 = c.GetFirstItem(pawn.Map);
                if (item3 != null && item3.def.EverHaulable && item3.def.canLoadIntoCaravan)
                {
                    Pawn packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                    JobDef jobDef = (packTarget == pawn) ? JobDefOf.TakeInventory : JobDefOf.GiveToPackAnimal;
                    if (!pawn.CanReach(item3, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item3.Label, item3) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item3, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item3.Label, item3) + ": " + "TooHeavy".Translate(), null));
                    }
                    else
                    {
                        LordJob_FormAndSendCaravan lordJob = (LordJob_FormAndSendCaravan)pawn.GetLord().LordJob;
                        float capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
                        if (item3.stackCount == 1)
                        {
                            float capacityLeft2 = capacityLeft - item3.GetStatValue(StatDefOf.Mass);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate(item3.Label, item3), capacityLeft2), delegate
                            {
                                item3.SetForbidden(value: false, warnOnFail: false);
                                Job job12 = JobMaker.MakeJob(jobDef, item3);
                                job12.count = 1;
                                job12.checkEncumbrance = (packTarget == pawn);
                                pawn.jobs.TryTakeOrderedJob(job12);
                            }, MenuOptionPriority.High), pawn, item3));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item3, item3.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate(item3.Label, item3) + ": " + "TooHeavy".Translate(), null));
                            }
                            else
                            {
                                float capacityLeft3 = capacityLeft - (float)item3.stackCount * item3.GetStatValue(StatDefOf.Mass);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate(item3.Label, item3), capacityLeft3), delegate
                                {
                                    item3.SetForbidden(value: false, warnOnFail: false);
                                    Job job11 = JobMaker.MakeJob(jobDef, item3);
                                    job11.count = item3.stackCount;
                                    job11.checkEncumbrance = (packTarget == pawn);
                                    pawn.jobs.TryTakeOrderedJob(job11);
                                }, MenuOptionPriority.High), pawn, item3));
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("LoadIntoCaravanSome".Translate(item3.LabelNoCount, item3), delegate
                            {
                                int to3 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, item3), item3.stackCount);
                                Dialog_Slider window3 = new Dialog_Slider(delegate (int val)
                                {
                                    float capacityLeft4 = capacityLeft - (float)val * item3.GetStatValue(StatDefOf.Mass);
                                    return CaravanFormingUtility.AppendOverweightInfo(string.Format("LoadIntoCaravanCount".Translate(item3.LabelNoCount, item3), val), capacityLeft4);
                                }, 1, to3, delegate (int count)
                                {
                                    item3.SetForbidden(value: false, warnOnFail: false);
                                    Job job10 = JobMaker.MakeJob(jobDef, item3);
                                    job10.count = count;
                                    job10.checkEncumbrance = (packTarget == pawn);
                                    pawn.jobs.TryTakeOrderedJob(job10);
                                });
                                Find.WindowStack.Add(window3);
                            }, MenuOptionPriority.High), pawn, item3));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                Thing item2 = c.GetFirstItem(pawn.Map);
                if (item2 != null && item2.def.EverHaulable)
                {
                    if (!pawn.CanReach(item2, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(item2.Label, item2) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item2, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(item2.Label, item2) + ": " + "TooHeavy".Translate(), null));
                    }
                    else if (item2.stackCount == 1)
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUp".Translate(item2.Label, item2), delegate
                        {
                            item2.SetForbidden(value: false, warnOnFail: false);
                            Job job9 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                            job9.count = 1;
                            job9.checkEncumbrance = true;
                            pawn.jobs.TryTakeOrderedJob(job9);
                        }, MenuOptionPriority.High), pawn, item2));
                    }
                    else
                    {
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item2, item2.stackCount))
                        {
                            opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(item2.Label, item2) + ": " + "TooHeavy".Translate(), null));
                        }
                        else
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate(item2.Label, item2), delegate
                            {
                                item2.SetForbidden(value: false, warnOnFail: false);
                                Job job8 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                                job8.count = item2.stackCount;
                                job8.checkEncumbrance = true;
                                pawn.jobs.TryTakeOrderedJob(job8);
                            }, MenuOptionPriority.High), pawn, item2));
                        }

                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate(item2.LabelNoCount, item2), delegate
                        {
                            int to2 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item2), item2.stackCount);
                            Dialog_Slider window2 = new Dialog_Slider("PickUpCount".Translate(item2.LabelNoCount, item2), 1, to2, delegate (int count)
                            {
                                item2.SetForbidden(value: false, warnOnFail: false);
                                Job job7 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                                job7.count = count;
                                job7.checkEncumbrance = true;
                                pawn.jobs.TryTakeOrderedJob(job7);
                            });
                            Find.WindowStack.Add(window2);
                        }, MenuOptionPriority.High), pawn, item2));
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                Thing item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    Pawn bestPackAnimal = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn);
                    if (bestPackAnimal != null)
                    {
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, 1))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else if (item.stackCount == 1)
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimal".Translate(item.Label, item), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job6 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                job6.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job6);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, item.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotGiveToPackAnimalAll".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalAll".Translate(item.Label, item), delegate
                                {
                                    item.SetForbidden(value: false, warnOnFail: false);
                                    Job job5 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                    job5.count = item.stackCount;
                                    pawn.jobs.TryTakeOrderedJob(job5);
                                }, MenuOptionPriority.High), pawn, item));
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalSome".Translate(item.LabelNoCount, item), delegate
                            {
                                int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item), item.stackCount);
                                Dialog_Slider window = new Dialog_Slider("GiveToPackAnimalCount".Translate(item.LabelNoCount, item), 1, to, delegate (int count)
                                {
                                    item.SetForbidden(value: false, warnOnFail: false);
                                    Job job4 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                    job4.count = count;
                                    pawn.jobs.TryTakeOrderedJob(job4);
                                });
                                Find.WindowStack.Add(window);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && pawn.Map.exitMapGrid.MapUsesExitGrid)
            {
                foreach (LocalTargetInfo item16 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                {
                    Pawn p = (Pawn)item16.Thing;
                    if (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || CaravanUtility.ShouldAutoCapture(p, Faction.OfPlayer))
                    {
                        IntVec3 exitSpot;
                        if (!pawn.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
                        {
                            opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            TaggedString taggedString2 = (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony) ? "CarryToExit".Translate(p.Label, p) : "CarryToExitAndCapture".Translate(p.Label, p);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, delegate
                            {
                                Job job3 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToExit, p, exitSpot);
                                job3.count = 1;
                                job3.failIfCantJoinOrCreateCaravan = true;
                                pawn.jobs.TryTakeOrderedJob(job3);
                            }, MenuOptionPriority.High), pawn, item16));
                        }
                    }
                }
            }

            if (pawn.equipment != null && pawn.equipment.Primary != null && GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForSelf(pawn), thingsOnly: true).Any())
            {
                if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(pawn.equipment.Primary, pawn))
                {
                    opts.Add(new FloatMenuOption("CannotDrop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null));
                }
                else
                {
                    Action action5 = delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, pawn.equipment.Primary));
                    };
                    opts.Add(new FloatMenuOption("Drop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary), action5, MenuOptionPriority.Default, null, pawn));
                }
            }

            foreach (LocalTargetInfo item17 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForTrade(), thingsOnly: true))
            {
                if (!pawn.CanReach(item17, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
                {
                    opts.Add(new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null));
                }
                else if (!pawn.CanTradeWith(((Pawn)item17.Thing).Faction, ((Pawn)item17.Thing).TraderKind))
                {
                    opts.Add(new FloatMenuOption("CannotTradeMissingTitleAbility".Translate(), null));
                }
                else
                {
                    Pawn pTarg = (Pawn)item17.Thing;
                    Action action6 = delegate
                    {
                        Job job2 = JobMaker.MakeJob(JobDefOf.TradeWithPawn, pTarg);
                        job2.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job2);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                    };
                    string t2 = "";
                    if (pTarg.Faction != null)
                    {
                        t2 = " (" + pTarg.Faction.Name + ")";
                    }

                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TradeWith".Translate(pTarg.LabelShort + ", " + pTarg.TraderKind.label) + t2, action6, MenuOptionPriority.InitiateSocial, null, item17.Thing), pawn, pTarg));
                }
            }

            foreach (LocalTargetInfo casket in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForOpen(pawn), thingsOnly: true))
            {
                if (!pawn.CanReach(casket, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    opts.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "Incapable".Translate(), null));
                }
                else if (casket.Thing.Map.designationManager.DesignationOn(casket.Thing, DesignationDefOf.Open) == null)
                {
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Open".Translate(casket.Thing), delegate
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Open, casket.Thing);
                        job.ignoreDesignations = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                    }, MenuOptionPriority.High), pawn, casket.Thing));
                }
            }

            foreach (Thing item18 in pawn.Map.thingGrid.ThingsAt(c))
            {
                foreach (FloatMenuOption floatMenuOption2 in item18.GetFloatMenuOptions(pawn))
                {
                    opts.Add(floatMenuOption2);
                }
            }

            void Equip()
            {
                equipment.SetForbidden(value: false);
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, equipment));
                MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
            }
            return false;
        }



    }
}
