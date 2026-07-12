using NightSafety.AI;

namespace NightSafety.Core
{
    public static class HarassmentThemePolicy
    {
        public const int MinimumPackSize = 2;
        public const int MaximumPackSize = 5;

        public static bool IsValid(int value) => value >= (int)HarassmentTheme.Arson && value <= (int)HarassmentTheme.Theft;

        public static int SelectPackSize(int randomOffset)
        {
            int width = MaximumPackSize - MinimumPackSize + 1;
            if (randomOffset < 0 || randomOffset >= width) return MinimumPackSize;
            return MinimumPackSize + randomOffset;
        }

        public static int RegroupDuration(int minTicks, int maxTicks, int pawnId, int currentTick)
        {
            if (minTicks < 0 || maxTicks < minTicks) return 0;
            int width = maxTicks - minTicks + 1;
            int hash = unchecked((pawnId * 397) ^ currentTick) & int.MaxValue;
            return minTicks + hash % width;
        }
    }
}
