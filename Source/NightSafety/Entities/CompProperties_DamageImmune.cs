using Verse;

namespace NightSafety.Entities
{
    public sealed class CompProperties_DamageImmune : CompProperties
    {
        public CompProperties_DamageImmune()
        {
            compClass = typeof(CompDamageImmune);
        }
    }
}
