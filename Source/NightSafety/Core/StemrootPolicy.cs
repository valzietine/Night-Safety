namespace NightSafety.Core
{
    /// <summary>
    /// The surface condition of one stemroot cell. Plain is the resting state; the other four
    /// are the overlay states the player can act on.
    /// </summary>
    public enum StemrootState
    {
        Plain,
        Bleeding,
        Blooming,
        Fruiting,
        Flushing
    }

    /// <summary>
    /// Pure decision layer for stemroot. Everything here is deterministic and free of Verse, so
    /// the rules can be tested directly and so two peers running the same simulation land on
    /// the same answer. There is no <c>Rand</c> anywhere in this file on purpose: state rolls and
    /// shift picks are hashed from the thing ID and the game tick instead, which keeps them
    /// reproducible without touching shared random state.
    /// </summary>
    public static class StemrootPolicy
    {
        /// <summary>Light level that splits day behaviour (bloom) from night behaviour (flush).</summary>
        public const float LightThreshold = 0.51f;

        /// <summary>Stemroot bleeds once damage takes it under this share of its hit points.</summary>
        public const float BleedHitPointFraction = 0.5f;

        /// <summary>
        /// A stable value in [0,1) drawn from two seeds. Same seeds, same answer, on every
        /// machine and after every reload.
        /// </summary>
        public static float UnitRoll(int seedA, int seedB)
        {
            unchecked
            {
                uint hash = (uint)seedA * 2654435761u;
                hash ^= (uint)seedB * 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return (hash & 0xFFFFFFu) / 16777216f;
            }
        }

        public static bool DamageForcesBleed(int hitPoints, int maxHitPoints)
        {
            if (maxHitPoints <= 0) return false;
            return hitPoints * 2 <= maxHitPoints;
        }

        /// <summary>Damage past the halfway mark overwrites whatever the cell was showing.</summary>
        public static StemrootState AfterDamage(StemrootState current, int hitPoints, int maxHitPoints)
        {
            return DamageForcesBleed(hitPoints, maxHitPoints) ? StemrootState.Bleeding : current;
        }

        public static bool ShouldRegenerate(int hitPoints, int maxHitPoints, int ticksSinceDamage, int regenDelayTicks)
        {
            return hitPoints > 0 && hitPoints < maxHitPoints && ticksSinceDamage >= regenDelayTicks;
        }

        /// <summary>
        /// Conditions that end a state without its countdown running out: light moving across the
        /// threshold, or the cell being walled in by new stemroot so it is no longer open to air.
        /// </summary>
        public static StemrootState Lapse(StemrootState current, bool exposed, float glow)
        {
            if (current == StemrootState.Plain) return current;
            if (!exposed) return StemrootState.Plain;

            if (current == StemrootState.Blooming && glow < LightThreshold) return StemrootState.Plain;
            if (current == StemrootState.Flushing && glow >= LightThreshold) return StemrootState.Plain;
            return current;
        }

        /// <summary>What a state becomes when its countdown runs out.</summary>
        public static StemrootState Expire(StemrootState current)
        {
            switch (current)
            {
                case StemrootState.Blooming: return StemrootState.Fruiting;
                case StemrootState.Bleeding:
                case StemrootState.Fruiting: return StemrootState.Plain;
                default: return current;
            }
        }

        /// <summary>
        /// Flushing has no countdown: mushrooms sit there until the light comes back up, which
        /// <see cref="Lapse"/> handles.
        /// </summary>
        public static bool HasTimer(StemrootState state)
        {
            return state == StemrootState.Bleeding
                || state == StemrootState.Blooming
                || state == StemrootState.Fruiting;
        }

        public static int DurationTicks(int minTicks, int maxTicks, int seedA, int seedB)
        {
            if (maxTicks <= minTicks) return minTicks;
            int width = maxTicks - minTicks + 1;
            int offset = (int)(UnitRoll(seedA, seedB) * width);
            if (offset >= width) offset = width - 1;
            return minTicks + offset;
        }

        public static float BleedChance(bool exposed, StemrootState current, float baseChance,
            float pollution, float pollutionScale)
        {
            if (!exposed || current != StemrootState.Plain) return 0f;
            return baseChance + (Clamp01(pollution) * pollutionScale);
        }

        /// <summary>
        /// Bloom wants sun and warmth, and gets commoner as it gets more of both.
        /// </summary>
        public static float BloomChance(bool exposed, StemrootState current, float baseChance,
            float glow, float temperatureC, float minTemperatureC, float idealTemperatureC)
        {
            if (!exposed || current != StemrootState.Plain) return 0f;
            if (glow <= LightThreshold) return 0f;
            if (idealTemperatureC <= minTemperatureC) return 0f;

            float lightFactor = Clamp01((glow - LightThreshold) / (1f - LightThreshold));
            float warmthFactor = Clamp01((temperatureC - minTemperatureC) / (idealTemperatureC - minTemperatureC));
            return baseChance * lightFactor * warmthFactor;
        }

        public static float FlushChance(bool exposed, StemrootState current, float baseChance, float glow)
        {
            if (!exposed || current != StemrootState.Plain) return 0f;
            if (glow >= LightThreshold) return 0f;
            return baseChance * Clamp01((LightThreshold - glow) / LightThreshold);
        }

        /// <summary>
        /// One roll decides all three outcomes, so a cell can only ever pick up one state per
        /// check. Plain means nothing happened.
        /// </summary>
        public static StemrootState SelectSpontaneous(float roll, float bleedChance, float bloomChance,
            float flushChance)
        {
            float cumulative = bleedChance;
            if (roll < cumulative) return StemrootState.Bleeding;
            cumulative += bloomChance;
            if (roll < cumulative) return StemrootState.Blooming;
            cumulative += flushChance;
            if (roll < cumulative) return StemrootState.Flushing;
            return StemrootState.Plain;
        }

        public static bool IsHarvestable(StemrootState state)
        {
            return state == StemrootState.Bleeding
                || state == StemrootState.Fruiting
                || state == StemrootState.Flushing;
        }

        public static int HarvestCount(StemrootState state, int bleedingCount, int fruitingCount, int flushingCount)
        {
            switch (state)
            {
                case StemrootState.Bleeding: return bleedingCount;
                case StemrootState.Fruiting: return fruitingCount;
                case StemrootState.Flushing: return flushingCount;
                default: return 0;
            }
        }

        /// <summary>Harvesting takes the crop and leaves the wall standing.</summary>
        public static StemrootState AfterHarvest(StemrootState state) => StemrootState.Plain;

        /// <summary>
        /// The mood hit is for touching the tree on purpose. Fire, raiders, and anything else that
        /// kills stemroot without a colonist swinging at it leaves no memory.
        /// </summary>
        public static bool AppliesUnsettled(bool destroyedByColonist) => destroyedByColonist;

        public static int TargetCount(int growableCellCount, float densityFraction)
        {
            if (growableCellCount <= 0 || densityFraction <= 0f) return 0;
            float clamped = densityFraction > 1f ? 1f : densityFraction;
            return (int)(growableCellCount * clamped + 0.5f);
        }

        public static int GrowBudget(int currentCount, int targetCount, int budget)
        {
            int room = targetCount - currentCount;
            if (room <= 0 || budget <= 0) return 0;
            return room < budget ? room : budget;
        }

        public static int RecedeBudget(int currentCount, int budget)
        {
            if (currentCount <= 0 || budget <= 0) return 0;
            return currentCount < budget ? currentCount : budget;
        }

        /// <summary>
        /// The grace the player was promised: nothing grows inside an oven's radius, plus a couple
        /// of cells, so the safe zone cannot be walled shut from the outside.
        /// </summary>
        public static bool BlockedByOven(int distanceSquared, float ovenRadius, int graceCells)
        {
            float radius = (float.IsNaN(ovenRadius) || float.IsInfinity(ovenRadius) || ovenRadius < 0f)
                ? 0f
                : ovenRadius;
            float blocked = radius + graceCells;
            return distanceSquared <= blocked * blocked;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
