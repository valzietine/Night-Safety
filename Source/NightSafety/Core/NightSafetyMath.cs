
namespace NightSafety.Core
{
    public static class NightSafetyMath
    {
        public static bool IsWithinRadius(int cellX, int cellZ, int centerX, int centerZ, float radius)
        {
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
            {
                return false;
            }

            long dx = cellX - centerX;
            long dz = cellZ - centerZ;
            return (dx * dx) + (dz * dz) <= radius * radius;
        }

        public static bool IsNight(float hour, float startHour, float endHour)
        {
            if (!IsHour(hour) || !IsHour(startHour) || !IsHour(endHour) || startHour == endHour)
            {
                return false;
            }

            return startHour < endHour
                ? hour >= startHour && hour < endHour
                : hour >= startHour || hour < endHour;
        }

        public static int CompareCandidates(float distanceA, int stableIdA, float distanceB, int stableIdB)
        {
            int distanceComparison = distanceA.CompareTo(distanceB);
            return distanceComparison != 0 ? distanceComparison : stableIdA.CompareTo(stableIdB);
        }

        private static bool IsHour(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value < 24f;
        }
    }
}
