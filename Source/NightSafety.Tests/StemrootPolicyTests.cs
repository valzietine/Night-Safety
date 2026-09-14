using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests
{
    public class StemrootDamageTests
    {
        [Theory]
        [InlineData(60, 60, false)]
        [InlineData(31, 60, false)]
        [InlineData(30, 60, true)]
        [InlineData(1, 60, true)]
        public void BleedThresholdIsHalfHitPoints(int hitPoints, int maxHitPoints, bool expected)
        {
            Assert.Equal(expected, StemrootPolicy.DamageForcesBleed(hitPoints, maxHitPoints));
        }

        [Fact]
        public void DamageBelowHalfOverwritesEveryOtherState()
        {
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.AfterDamage(StemrootState.Blooming, 20, 60));
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.AfterDamage(StemrootState.Fruiting, 20, 60));
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.AfterDamage(StemrootState.Flushing, 20, 60));
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.AfterDamage(StemrootState.Plain, 20, 60));
        }

        [Fact]
        public void DamageAboveHalfLeavesTheStateAlone()
        {
            Assert.Equal(StemrootState.Fruiting, StemrootPolicy.AfterDamage(StemrootState.Fruiting, 45, 60));
        }

        [Fact]
        public void MaxHitPointsOfZeroNeverForcesBleeding()
        {
            Assert.False(StemrootPolicy.DamageForcesBleed(0, 0));
        }
    }

    public class StemrootRegenerationTests
    {
        [Fact]
        public void RegenerationWaitsOutTheDelayAfterDamage()
        {
            Assert.False(StemrootPolicy.ShouldRegenerate(30, 60, 2499, 2500));
            Assert.True(StemrootPolicy.ShouldRegenerate(30, 60, 2500, 2500));
        }

        [Fact]
        public void UndamagedStemrootDoesNotRegenerate()
        {
            Assert.False(StemrootPolicy.ShouldRegenerate(60, 60, 100000, 2500));
        }

        [Fact]
        public void DestroyedStemrootDoesNotRegenerate()
        {
            Assert.False(StemrootPolicy.ShouldRegenerate(0, 60, 100000, 2500));
        }
    }

    public class StemrootLapseTests
    {
        [Fact]
        public void BloomLapsesWhenTheLightDrops()
        {
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Lapse(StemrootState.Blooming, true, 0.4f));
            Assert.Equal(StemrootState.Blooming, StemrootPolicy.Lapse(StemrootState.Blooming, true, 0.6f));
        }

        [Fact]
        public void FlushLapsesWhenTheLightRises()
        {
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Lapse(StemrootState.Flushing, true, 0.6f));
            Assert.Equal(StemrootState.Flushing, StemrootPolicy.Lapse(StemrootState.Flushing, true, 0.4f));
        }

        [Fact]
        public void FruitAndBleedIgnoreTheLightLevel()
        {
            Assert.Equal(StemrootState.Fruiting, StemrootPolicy.Lapse(StemrootState.Fruiting, true, 0.0f));
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.Lapse(StemrootState.Bleeding, true, 1.0f));
        }

        [Fact]
        public void BuriedStemrootFallsBackToPlain()
        {
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Lapse(StemrootState.Fruiting, false, 1.0f));
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Lapse(StemrootState.Bleeding, false, 0.0f));
        }
    }

    public class StemrootExpiryTests
    {
        [Fact]
        public void BloomRipensIntoFruit()
        {
            Assert.Equal(StemrootState.Fruiting, StemrootPolicy.Expire(StemrootState.Blooming));
        }

        [Fact]
        public void BleedAndFruitFallBackToPlain()
        {
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Expire(StemrootState.Bleeding));
            Assert.Equal(StemrootState.Plain, StemrootPolicy.Expire(StemrootState.Fruiting));
        }

        [Fact]
        public void OnlyTimedStatesCarryACountdown()
        {
            Assert.True(StemrootPolicy.HasTimer(StemrootState.Bleeding));
            Assert.True(StemrootPolicy.HasTimer(StemrootState.Blooming));
            Assert.True(StemrootPolicy.HasTimer(StemrootState.Fruiting));
            Assert.False(StemrootPolicy.HasTimer(StemrootState.Flushing));
            Assert.False(StemrootPolicy.HasTimer(StemrootState.Plain));
        }

        [Fact]
        public void DurationStaysInsideTheAuthoredRange()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                int ticks = StemrootPolicy.DurationTicks(7500, 15000, seed, 17);
                Assert.InRange(ticks, 7500, 15000);
            }
        }

        [Fact]
        public void DurationIsStableForTheSameSeeds()
        {
            Assert.Equal(StemrootPolicy.DurationTicks(7500, 15000, 42, 7),
                StemrootPolicy.DurationTicks(7500, 15000, 42, 7));
        }

        [Fact]
        public void InvertedDurationRangeCollapsesToTheMinimum()
        {
            Assert.Equal(15000, StemrootPolicy.DurationTicks(15000, 7500, 3, 9));
        }
    }

    public class StemrootSpontaneousStateTests
    {
        [Fact]
        public void BuriedStemrootNeverChangesOnItsOwn()
        {
            Assert.Equal(0f, StemrootPolicy.BleedChance(false, StemrootState.Plain, 0.5f, 1f, 1f));
            Assert.Equal(0f, StemrootPolicy.BloomChance(false, StemrootState.Plain, 0.5f, 1f, 30f, 10f, 30f));
            Assert.Equal(0f, StemrootPolicy.FlushChance(false, StemrootState.Plain, 0.5f, 0f));
        }

        [Fact]
        public void OnlyPlainStemrootChangesOnItsOwn()
        {
            Assert.Equal(0f, StemrootPolicy.BleedChance(true, StemrootState.Blooming, 0.5f, 0f, 1f));
            Assert.Equal(0f, StemrootPolicy.BloomChance(true, StemrootState.Fruiting, 0.5f, 1f, 30f, 10f, 30f));
            Assert.Equal(0f, StemrootPolicy.FlushChance(true, StemrootState.Bleeding, 0.5f, 0f));
        }

        [Fact]
        public void PollutionRaisesTheBleedChance()
        {
            float clean = StemrootPolicy.BleedChance(true, StemrootState.Plain, 0.02f, 0f, 0.08f);
            float polluted = StemrootPolicy.BleedChance(true, StemrootState.Plain, 0.02f, 1f, 0.08f);
            Assert.Equal(0.02f, clean, 5);
            Assert.True(polluted > clean);
        }

        [Fact]
        public void BloomNeedsLightAboveTheThreshold()
        {
            Assert.Equal(0f, StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 0.51f, 30f, 10f, 30f));
            Assert.True(StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 1f, 30f, 10f, 30f) > 0f);
        }

        [Fact]
        public void BloomFavoursWarmBrightCells()
        {
            float cold = StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 1f, 12f, 10f, 30f);
            float warm = StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 1f, 30f, 10f, 30f);
            Assert.True(warm > cold);

            float dim = StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 0.7f, 30f, 10f, 30f);
            Assert.True(warm > dim);
        }

        [Fact]
        public void FreezingCellsNeverBloom()
        {
            Assert.Equal(0f, StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, 1f, -5f, 10f, 30f));
        }

        [Fact]
        public void FlushNeedsLightBelowTheThreshold()
        {
            Assert.Equal(0f, StemrootPolicy.FlushChance(true, StemrootState.Plain, 0.5f, 0.51f));
            Assert.True(StemrootPolicy.FlushChance(true, StemrootState.Plain, 0.5f, 0f) > 0f);
        }

        [Fact]
        public void BloomAndFlushCannotBothBeOffered()
        {
            for (float glow = 0f; glow <= 1f; glow += 0.05f)
            {
                float bloom = StemrootPolicy.BloomChance(true, StemrootState.Plain, 0.5f, glow, 30f, 10f, 30f);
                float flush = StemrootPolicy.FlushChance(true, StemrootState.Plain, 0.5f, glow);
                Assert.True(bloom == 0f || flush == 0f);
            }
        }

        [Fact]
        public void SelectionWalksTheChancesInOrder()
        {
            Assert.Equal(StemrootState.Bleeding, StemrootPolicy.SelectSpontaneous(0.05f, 0.1f, 0.2f, 0.3f));
            Assert.Equal(StemrootState.Blooming, StemrootPolicy.SelectSpontaneous(0.25f, 0.1f, 0.2f, 0.3f));
            Assert.Equal(StemrootState.Flushing, StemrootPolicy.SelectSpontaneous(0.5f, 0.1f, 0.2f, 0.3f));
            Assert.Equal(StemrootState.Plain, StemrootPolicy.SelectSpontaneous(0.9f, 0.1f, 0.2f, 0.3f));
        }

        [Fact]
        public void RollsStayInsideTheUnitInterval()
        {
            for (int a = 0; a < 64; a++)
            {
                for (int b = 0; b < 8; b++)
                {
                    float roll = StemrootPolicy.UnitRoll(a, b);
                    Assert.InRange(roll, 0f, 0.9999999f);
                }
            }
        }

        [Fact]
        public void RollsAreStableAndSeedDependent()
        {
            Assert.Equal(StemrootPolicy.UnitRoll(1234, 7), StemrootPolicy.UnitRoll(1234, 7));
            Assert.NotEqual(StemrootPolicy.UnitRoll(1234, 7), StemrootPolicy.UnitRoll(1234, 8));
        }
    }

    public class StemrootHarvestTests
    {
        [Fact]
        public void OnlyLoadedStatesCanBeHarvested()
        {
            Assert.True(StemrootPolicy.IsHarvestable(StemrootState.Bleeding));
            Assert.True(StemrootPolicy.IsHarvestable(StemrootState.Fruiting));
            Assert.True(StemrootPolicy.IsHarvestable(StemrootState.Flushing));
            Assert.False(StemrootPolicy.IsHarvestable(StemrootState.Blooming));
            Assert.False(StemrootPolicy.IsHarvestable(StemrootState.Plain));
        }

        [Fact]
        public void HarvestYieldsTheStateProduct()
        {
            Assert.Equal(20, StemrootPolicy.HarvestCount(StemrootState.Bleeding, 20, 10, 11));
            Assert.Equal(10, StemrootPolicy.HarvestCount(StemrootState.Fruiting, 20, 10, 11));
            Assert.Equal(11, StemrootPolicy.HarvestCount(StemrootState.Flushing, 20, 10, 11));
            Assert.Equal(0, StemrootPolicy.HarvestCount(StemrootState.Blooming, 20, 10, 11));
            Assert.Equal(0, StemrootPolicy.HarvestCount(StemrootState.Plain, 20, 10, 11));
        }

        [Fact]
        public void HarvestLeavesPlainStemrootBehind()
        {
            Assert.Equal(StemrootState.Plain, StemrootPolicy.AfterHarvest(StemrootState.Fruiting));
        }

        [Fact]
        public void OnlyColonistWorkCarriesTheMoodPenalty()
        {
            Assert.True(StemrootPolicy.AppliesUnsettled(true));
            Assert.False(StemrootPolicy.AppliesUnsettled(false));
        }
    }

    public class StemrootShiftTests
    {
        [Fact]
        public void GrowthStopsAtTheTargetDensity()
        {
            Assert.Equal(3, StemrootPolicy.GrowBudget(100, 200, 3));
            Assert.Equal(0, StemrootPolicy.GrowBudget(200, 200, 3));
            Assert.Equal(0, StemrootPolicy.GrowBudget(400, 200, 3));
        }

        [Fact]
        public void GrowthNeverOvershootsTheTarget()
        {
            Assert.Equal(1, StemrootPolicy.GrowBudget(199, 200, 3));
        }

        [Fact]
        public void RecessionIsBoundedByWhatExists()
        {
            Assert.Equal(2, StemrootPolicy.RecedeBudget(100, 2));
            Assert.Equal(1, StemrootPolicy.RecedeBudget(1, 2));
            Assert.Equal(0, StemrootPolicy.RecedeBudget(0, 2));
        }

        [Fact]
        public void TargetCountFollowsTheDensityFraction()
        {
            Assert.Equal(150, StemrootPolicy.TargetCount(1000, 0.15f));
            Assert.Equal(0, StemrootPolicy.TargetCount(1000, 0f));
            Assert.Equal(0, StemrootPolicy.TargetCount(0, 0.15f));
        }

        [Fact]
        public void TargetCountClampsAnAbsurdDensity()
        {
            Assert.Equal(1000, StemrootPolicy.TargetCount(1000, 5f));
            Assert.Equal(0, StemrootPolicy.TargetCount(1000, -1f));
        }

        [Fact]
        public void OvensKeepTheirRadiusPlusTwoClear()
        {
            // Radius 12 plus the two cell grace: 14 is blocked, 15 is not.
            Assert.True(StemrootPolicy.BlockedByOven(14 * 14, 12f, 2));
            Assert.False(StemrootPolicy.BlockedByOven(15 * 15, 12f, 2));
        }

        [Fact]
        public void TheOvenCellItselfIsAlwaysBlocked()
        {
            Assert.True(StemrootPolicy.BlockedByOven(0, 0f, 2));
        }

        [Fact]
        public void UnusableOvenRadiusStillBlocksNothingOutsideTheGrace()
        {
            Assert.False(StemrootPolicy.BlockedByOven(9, float.NaN, 2));
        }
        [Fact]
        public void TheMiddleOfAPatchComesOutSolid()
        {
            // The old fill thinned from the centre outwards, so even the core of a stand was
            // roughly a quarter holes. Anything inside the solid core is now certain.
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(0f, 9f, 0.55f));
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(4f, 9f, 0.55f));
        }

        [Fact]
        public void TheCoreBoundaryItselfIsStillSolid()
        {
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(4.95f, 9f, 0.55f));
        }

        [Fact]
        public void PastTheCoreTheChanceFallsOffWithoutReachingEitherEnd()
        {
            float chance = StemrootPolicy.PatchFillChance(7f, 9f, 0.55f);
            Assert.True(chance > 0f);
            Assert.True(chance < 1f);
        }

        [Fact]
        public void TheFalloffIsMonotonicAcrossTheOuterBand()
        {
            float previous = 1.1f;
            for (float distance = 5f; distance <= 9f; distance += 0.5f)
            {
                float chance = StemrootPolicy.PatchFillChance(distance, 9f, 0.55f);
                Assert.True(chance < previous);
                previous = chance;
            }
        }

        [Fact]
        public void NothingSpawnsAtOrBeyondTheRim()
        {
            Assert.Equal(0f, StemrootPolicy.PatchFillChance(9f, 9f, 0.55f));
            Assert.Equal(0f, StemrootPolicy.PatchFillChance(12f, 9f, 0.55f));
        }

        [Fact]
        public void AFullCoreFillsTheWholePatch()
        {
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(8.9f, 9f, 1f));
        }

        [Fact]
        public void NoCoreLeavesAPlainFalloffFromTheCentre()
        {
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(0f, 10f, 0f));
            Assert.Equal(0.5f, StemrootPolicy.PatchFillChance(5f, 10f, 0f), 4);
        }

        [Fact]
        public void ADegeneratePatchStillFillsItsOwnCentre()
        {
            Assert.Equal(1f, StemrootPolicy.PatchFillChance(0f, 0f, 0.55f));
        }

        [Fact]
        public void NoStretchMeasuresPlainDistance()
        {
            Assert.Equal(5f, StemrootPolicy.PatchDistance(3f, 4f, 0f, 1f), 4);
        }

        [Fact]
        public void StretchLeavesTheAlongAxisAlone()
        {
            // Axis along +x: an offset of 6 down that axis is still 6 however hard we stretch.
            Assert.Equal(6f, StemrootPolicy.PatchDistance(6f, 0f, 0f, 2.5f), 4);
        }

        [Fact]
        public void StretchPushesTheAcrossAxisOut()
        {
            // Axis along +x: an offset of 2 across it reads as 5 once stretched by 2.5, so the
            // patch runs out of radius sooner sideways than lengthways.
            Assert.Equal(5f, StemrootPolicy.PatchDistance(0f, 2f, 0f, 2.5f), 4);
        }

        [Fact]
        public void TheStretchAxisRotatesWithTheAngle()
        {
            // Same offset, axis turned a quarter turn: what was along the axis is now across it.
            float along = StemrootPolicy.PatchDistance(0f, 6f, (float)(System.Math.PI / 2.0), 2.5f);
            Assert.Equal(6f, along, 3);
        }

        [Fact]
        public void AStretchedPatchIsLongerThanItIsWide()
        {
            const float radius = 9f;
            float lengthways = StemrootPolicy.PatchDistance(8f, 0f, 0f, 2.2f);
            float sideways = StemrootPolicy.PatchDistance(0f, 8f, 0f, 2.2f);
            Assert.True(lengthways < radius);
            Assert.True(sideways > radius);
        }

        [Fact]
        public void AnUnarmedScheduleShiftsAtTheNextOpportunity()
        {
            // -1 is what a freshly constructed component carries, and it means "shift now".
            Assert.True(StemrootPolicy.ShouldShiftNow(-1, 40000));
        }

        [Fact]
        public void AnArmedScheduleWaitsForItsDeadline()
        {
            Assert.False(StemrootPolicy.ShouldShiftNow(42500, 40000));
            Assert.True(StemrootPolicy.ShouldShiftNow(42500, 42500));
            Assert.True(StemrootPolicy.ShouldShiftNow(42500, 42501));
        }

        [Fact]
        public void ATransferredMapIsArmedFromTheReceiversOwnClock()
        {
            // The sending peer's absolute tick means nothing here: the two peers run different
            // TicksGame on a shared map, so the deadline has to be built from the receiver's now.
            Assert.Equal(40000 + 2500, StemrootPolicy.ShiftTickAfterTransfer(40000, 2500));
        }

        [Fact]
        public void ATransferredMapDoesNotShiftOnArrival()
        {
            // Left unset, a received map shifts on its very next check and walks straight
            // away from the sender's copy. That is the bug this catches.
            const int now = 38001;
            int armed = StemrootPolicy.ShiftTickAfterTransfer(now, 2500);
            Assert.False(StemrootPolicy.ShouldShiftNow(armed, now));
        }

        [Fact]
        public void ATransferredMapStillShiftsOnceTheIntervalHasPassed()
        {
            const int now = 38001;
            int armed = StemrootPolicy.ShiftTickAfterTransfer(now, 2500);
            Assert.True(StemrootPolicy.ShouldShiftNow(armed, now + 2500));
        }

        [Fact]
        public void ANonsenseIntervalStillAdvancesTheDeadline()
        {
            Assert.True(StemrootPolicy.ShiftTickAfterTransfer(40000, 0) > 40000);
            Assert.True(StemrootPolicy.ShiftTickAfterTransfer(40000, -5) > 40000);
        }

    }
}
