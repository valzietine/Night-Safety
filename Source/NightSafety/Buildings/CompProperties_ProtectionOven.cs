using System.Collections.Generic;
using Verse;

namespace NightSafety.Buildings
{
    public sealed class CompProperties_ProtectionOven : CompProperties
    {
        public float radius = 12f;

        public CompProperties_ProtectionOven()
        {
            compClass = typeof(CompProtectionOven);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
            {
                yield return $"{parentDef.defName}: protection radius must be finite and greater than zero.";
            }
        }
    }
}
