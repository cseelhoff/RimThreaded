#region Assembly Assembly-CSharp, Version=1.2.7558.21380, Culture=neutral, PublicKeyToken=null
// C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    public class Fire_Target : AttachableThing, ISizeReporter
    {
        private int ticksSinceSpawn;

        public float fireSize = 0.1f;

        private int ticksSinceSpread;

        private float flammabilityMax = 0.5f;

        private int ticksUntilSmoke;

        private Sustainer sustainer;

        private static List<Thing> flammableList = new List<Thing>();

        private static int fireCount;

        private static int lastFireCountUpdateTick;

        public const float MinFireSize = 0.1f;

        private const float MinSizeForSpark = 1f;

        private const float TicksBetweenSparksBase = 150f;

        private const float TicksBetweenSparksReductionPerFireSize = 40f;

        private const float MinTicksBetweenSparks = 75f;

        private const float MinFireSizeToEmitSpark = 1f;

        public const float MaxFireSize = 1.75f;

        private const int TicksToBurnFloor = 7500;

        private const int ComplexCalcsInterval = 150;

        private const float CellIgniteChancePerTickPerSize = 0.01f;

        private const float MinSizeForIgniteMovables = 0.4f;

        private const float FireBaseGrowthPerTick = 0.00055f;

        private static readonly IntRange SmokeIntervalRange = new IntRange(130, 200);

        private const int SmokeIntervalRandomAddon = 10;

        private const float BaseSkyExtinguishChance = 0.04f;

        private const int BaseSkyExtinguishDamage = 10;

        private const float HeatPerFireSizePerInterval = 160f;

        private const float HeatFactorWhenDoorPresent = 0.15f;

        private const float SnowClearRadiusPerFireSize = 3f;

        private const float SnowClearDepthFactor = 0.1f;

        private const int FireCountParticlesOff = 15;

        public int TicksSinceSpawn => ticksSinceSpawn;

        public override string Label
        {
            get
            {
                if (parent != null)
                {
                    return "FireOn".Translate(parent.LabelCap, parent);
                }

                return def.label;
            }
        }

        public override string InspectStringAddon => "Burning".Translate() + " (" + "FireSizeLower".Translate((fireSize * 100f).ToString("F0")) + ")";

        private float SpreadInterval
        {
            get
            {
                float num = 150f - (fireSize - 1f) * 40f;
                if (num < 75f)
                {
                    num = 75f;
                }

                return num;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceSpawn, "ticksSinceSpawn", 0);
            Scribe_Values.Look(ref fireSize, "fireSize", 0f);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RecalcPathsOnAndAroundMe(map);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.HomeArea, this, OpportunityType.Important);
            ticksSinceSpread = (int)(SpreadInterval * Rand.Value);
        }

        public float CurrentSize()
        {
            return fireSize;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (sustainer != null)
            {
                if (sustainer.externalParams.sizeAggregator == null)
                {
                    sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
                }

                sustainer.externalParams.sizeAggregator.RemoveReporter(this);
            }

            Map map = base.Map;
            base.DeSpawn(mode);
            RecalcPathsOnAndAroundMe(map);
        }

        private void RecalcPathsOnAndAroundMe(Map map)
        {
            IntVec3[] adjacentCellsAndInside = GenAdj.AdjacentCellsAndInside;
            for (int i = 0; i < adjacentCellsAndInside.Length; i++)
            {
                IntVec3 c = base.Position + adjacentCellsAndInside[i];
                if (c.InBounds(map))
                {
                    map.pathGrid.RecalculatePerceivedPathCostAt(c);
                }
            }
        }

        public override void AttachTo(Thing parent)
        {
            base.AttachTo(parent);
            Pawn pawn = parent as Pawn;
            if (pawn != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.WasOnFire, pawn);
            }
        }

        public override void Tick()
        {
            ticksSinceSpawn++;
            if (lastFireCountUpdateTick != Find.TickManager.TicksGame)
            {
                fireCount = base.Map.listerThings.ThingsOfDef(def).Count;
                lastFireCountUpdateTick = Find.TickManager.TicksGame;
            }

            if (sustainer != null)
            {
                sustainer.Maintain();
            }
            else if (!base.Position.Fogged(base.Map))
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, base.Map), MaintenanceType.PerTick);
                sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, SoundDefOf.FireBurning, info);
            }

            ticksUntilSmoke--;
            if (ticksUntilSmoke <= 0)
            {
                SpawnSmokeParticles();
            }

            if (fireCount < 15 && fireSize > 0.7f && Rand.Value < fireSize * 0.01f)
            {
                MoteMaker.ThrowMicroSparks(DrawPos, base.Map);
            }

            if (fireSize > 1f)
            {
                ticksSinceSpread++;
                if ((float)ticksSinceSpread >= SpreadInterval)
                {
                    TrySpread();
                    ticksSinceSpread = 0;
                }
            }

            if (this.IsHashIntervalTick(150))
            {
                DoComplexCalcs();
            }

            if (ticksSinceSpawn >= 7500)
            {
                TryBurnFloor();
            }
        }

        private void SpawnSmokeParticles()
        {
            if (fireCount < 15)
            {
                MoteMaker.ThrowSmoke(DrawPos, base.Map, fireSize);
            }

            if (fireSize > 0.5f && parent == null)
            {
                MoteMaker.ThrowFireGlow(base.Position, base.Map, fireSize);
            }

            float num = fireSize / 2f;
            if (num > 1f)
            {
                num = 1f;
            }

            num = 1f - num;
            ticksUntilSmoke = SmokeIntervalRange.Lerped(num) + (int)(10f * Rand.Value);
        }

        private void DoComplexCalcs()
        {
            List<Thing> flammableList = new List<Thing>();
            bool flag = false;
            //flammableList.Clear();
            flammabilityMax = 0f;
            if (!base.Position.GetTerrain(base.Map).extinguishesFire)
            {
                if (parent == null)
                {
                    if (base.Position.TerrainFlammableNow(base.Map))
                    {
                        flammabilityMax = base.Position.GetTerrain(base.Map).GetStatValueAbstract(StatDefOf.Flammability);
                    }

                    List<Thing> list = base.Map.thingGrid.ThingsListAt(base.Position);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing thing = list[i];
                        if (thing is Building_Door)
                        {
                            flag = true;
                        }

                        float statValue = thing.GetStatValue(StatDefOf.Flammability);
                        if (!(statValue < 0.01f))
                        {
                            flammableList.Add(list[i]);
                            if (statValue > flammabilityMax)
                            {
                                flammabilityMax = statValue;
                            }

                            if (parent == null && fireSize > 0.4f && list[i].def.category == ThingCategory.Pawn && Rand.Chance(FireUtility.ChanceToAttachFireCumulative(list[i], 150f)))
                            {
                                list[i].TryAttachFire(fireSize * 0.2f);
                            }
                        }
                    }
                }
                else
                {
                    flammableList.Add(parent);
                    flammabilityMax = parent.GetStatValue(StatDefOf.Flammability);
                }
            }

            if (flammabilityMax < 0.01f)
            {
                Destroy();
                return;
            }

            Thing thing2 = (parent != null) ? parent : ((flammableList.Count <= 0) ? null : flammableList.RandomElement());
            if (thing2 != null && (!(fireSize < 0.4f) || thing2 == parent || thing2.def.category != ThingCategory.Pawn))
            {
                DoFireDamage(thing2);
            }

            if (base.Spawned)
            {
                float num = fireSize * 160f;
                if (flag)
                {
                    num *= 0.15f;
                }

                GenTemperature.PushHeat(base.Position, base.Map, num);
                if (Rand.Value < 0.4f)
                {
                    float radius = fireSize * 3f;
                    SnowUtility.AddSnowRadial(base.Position, base.Map, radius, 0f - fireSize * 0.1f);
                }

                fireSize += 0.00055f * flammabilityMax * 150f;
                if (fireSize > 1.75f)
                {
                    fireSize = 1.75f;
                }

                if (base.Map.weatherManager.RainRate > 0.01f && VulnerableToRain() && Rand.Value < 6f)
                {
                    TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f));
                }
            }
        }

        private void TryBurnFloor()
        {
            if (parent == null && base.Spawned && base.Position.TerrainFlammableNow(base.Map))
            {
                base.Map.terrainGrid.Notify_TerrainBurned(base.Position);
            }
        }

        private bool VulnerableToRain()
        {
            if (!base.Spawned)
            {
                return false;
            }

            RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
            if (roofDef == null)
            {
                return true;
            }

            if (roofDef.isThickRoof)
            {
                return false;
            }

            return base.Position.GetEdifice(base.Map)?.def.holdsRoof ?? false;
        }

        private void DoFireDamage(Thing targ)
        {
            int num = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * fireSize, 0.0125f, 0.05f) * 150f);
            if (num < 1)
            {
                num = 1;
            }

            Pawn pawn = targ as Pawn;
            if (pawn != null)
            {
                BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire);
                Find.BattleLog.Add(battleLogEntry_DamageTaken);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this);
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                targ.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
                if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out Apparel result))
                {
                    result.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this));
                }
            }
            else
            {
                targ.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this));
            }
        }

        protected void TrySpread()
        {
            IntVec3 position = base.Position;
            bool flag;
            if (Rand.Chance(0.8f))
            {
                position = base.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
                flag = true;
            }
            else
            {
                position = base.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
                flag = false;
            }

            if (!position.InBounds(base.Map) || !Rand.Chance(FireUtility.ChanceToStartFireIn(position, base.Map)))
            {
                return;
            }

            if (!flag)
            {
                CellRect startRect = CellRect.SingleCell(base.Position);
                CellRect endRect = CellRect.SingleCell(position);
                if (GenSight.LineOfSight(base.Position, position, base.Map, startRect, endRect))
                {
                    ((Spark)GenSpawn.Spawn(ThingDefOf.Spark, base.Position, base.Map)).Launch(this, position, position, ProjectileHitFlags.All);
                }
            }
            else
            {
                FireUtility.TryStartFireIn(position, base.Map, 0.1f);
            }
        }
    }
}
#if false // Decompilation log
'18' items in cache
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
Found single assembly: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'
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
