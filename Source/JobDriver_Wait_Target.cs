#region Assembly Assembly-CSharp, Version=1.2.7558.21380, Culture=neutral, PublicKeyToken=null
// C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using RimWorld;
using System;
using System.Collections.Generic;

namespace Verse.AI
{
    public class JobDriver_Wait_Target : JobDriver
    {
        private const int TargetSearchInterval = 4;

        public override string GetReport()
        {
            if (job.def == JobDefOf.Wait_Combat)
            {
                if (pawn.RaceProps.Humanlike && pawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    return "ReportStanding".Translate();
                }

                return base.GetReport();
            }

            return base.GetReport();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                base.Map.pawnDestinationReservationManager.Reserve(pawn, job, pawn.Position);
                pawn.pather.StopDead();
                CheckForAutoAttack();
            };
            toil.tickAction = delegate
            {
                if (job.expiryInterval == -1 && job.def == JobDefOf.Wait_Combat && !pawn.Drafted)
                {
                    Log.Error(pawn + " in eternal WaitCombat without being drafted.");
                    ReadyForNextToil();
                }
                else if ((Find.TickManager.TicksGame + pawn.thingIDNumber) % 4 == 0)
                {
                    CheckForAutoAttack();
                }
            };
            DecorateWaitToil(toil);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            if (pawn.mindState != null && pawn.mindState.duty != null && pawn.mindState.duty.focus != null)
            {
                LocalTargetInfo focusLocal = pawn.mindState.duty.focus;
                toil.handlingFacing = false;
                toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceTarget(focusLocal);
                });
            }

            yield return toil;
        }

        public virtual void DecorateWaitToil(Toil wait)
        {
        }

        public override void Notify_StanceChanged()
        {
            if (pawn.stances.curStance is Stance_Mobile)
            {
                CheckForAutoAttack();
            }
        }

        private void CheckForAutoAttack()
        {
            if (base.pawn.Downed || base.pawn.stances.FullBodyBusy)
            {
                return;
            }

            collideWithPawns = false;
            bool flag = !base.pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool flag2 = base.pawn.RaceProps.ToolUser && base.pawn.Faction == Faction.OfPlayer && !base.pawn.WorkTagIsDisabled(WorkTags.Firefighting);
            if (!(flag | flag2))
            {
                return;
            }

            Fire fire = null;
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = base.pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!c.InBounds(base.pawn.Map))
                {
                    continue;
                }

                List<Thing> thingList = c.GetThingList(base.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing;
                    try
                    {
                        thing = thingList[j];
                    } catch (ArgumentOutOfRangeException) { break; }
                    if (flag)
                    {
                        Pawn pawn = thing as Pawn;
                        if (pawn != null && !pawn.Downed && base.pawn.HostileTo(pawn) && GenHostility.IsActiveThreatTo(pawn, base.pawn.Faction))
                        {
                            base.pawn.meleeVerbs.TryMeleeAttack(pawn);
                            collideWithPawns = true;
                            return;
                        }
                    }

                    if (flag2)
                    {
                        Fire fire2 = thing as Fire;
                        if (fire2 != null && (fire == null || fire2.fireSize < fire.fireSize || i == 8) && (fire2.parent == null || fire2.parent != base.pawn))
                        {
                            fire = fire2;
                        }
                    }
                }
            }

            if (fire != null && (!base.pawn.InMentalState || base.pawn.MentalState.def.allowBeatfire))
            {
                base.pawn.natives.TryBeatFire(fire);
            }
            else
            {
                if (!flag || !job.canUseRangedWeapon || base.pawn.Faction == null || job.def != JobDefOf.Wait_Combat || (base.pawn.drafter != null && !base.pawn.drafter.FireAtWill))
                {
                    return;
                }

                Verb currentEffectiveVerb = base.pawn.CurrentEffectiveVerb;
                if (currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack)
                {
                    TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
                    if (currentEffectiveVerb.IsIncendiary())
                    {
                        targetScanFlags |= TargetScanFlags.NeedNonBurning;
                    }

                    Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(base.pawn, targetScanFlags);
                    if (thing != null)
                    {
                        base.pawn.TryStartAttack(thing);
                        collideWithPawns = true;
                    }
                }
            }
        }
    }
}
#if false // Decompilation log
'17' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll'
------------------
Resolve: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'
------------------
Resolve: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AudioModule.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Core.dll'
------------------
Resolve: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'
------------------
Resolve: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.dll'
------------------
Resolve: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.Linq.dll'
------------------
Resolve: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AssetBundleModule.dll'
------------------
Resolve: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
#endif
