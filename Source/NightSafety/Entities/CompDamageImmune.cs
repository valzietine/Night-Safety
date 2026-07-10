using Verse;

namespace NightSafety.Entities
{
    public sealed class CompDamageImmune : ThingComp
    {
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            // The Spirit is a boundary pressure mechanic, not a combat encounter; all incoming damage is absorbed.
            absorbed = true;
        }
    }
}
