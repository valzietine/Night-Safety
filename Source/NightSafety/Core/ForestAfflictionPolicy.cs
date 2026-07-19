
namespace NightSafety.Core
{
    public static class ForestAfflictionPolicy
    {
        public static bool ShouldHaveAffliction(bool harasserFaction, bool harasserKind, bool stateMarker)
        {
            return harasserFaction || harasserKind || stateMarker;
        }
    }
}
