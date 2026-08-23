using Verse;

namespace NightSafety.Core
{
    // Every stemroot number lives here rather than in C# literals, so a balance pass is a Def
    // edit instead of a rebuild.
    public sealed class StemrootConfigDef : Def
    {
        // Regrowth after damage.
        public int regenDelayTicks = 2500;
        public int regenTicksPerHitPoint = 60;

        // Spontaneous state rolls.
        public int stateCheckIntervalTicks = 2500;
        public float bleedChance = 0.02f;
        public float bleedPollutionScale = 0.08f;
        public float bloomChance = 0.06f;
        public float bloomMinTemperature = 10f;
        public float bloomIdealTemperature = 30f;
        public float flushChance = 0.05f;

        // State countdowns.
        public int bleedDurationMinTicks = 7500;
        public int bleedDurationMaxTicks = 15000;
        public int bloomDurationMinTicks = 7500;
        public int bloomDurationMaxTicks = 15000;
        public int fruitDurationTicks = 120000;
        public int sapFilthIntervalTicks = 900;
    }
}
