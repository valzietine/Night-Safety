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
}
