using System.Collections.Generic;
using System.Linq;

namespace NightSafety.Core
{
    public static class SpiritOwnershipPolicy
    {
        public static int? SelectOwnerId(IEnumerable<int> candidateIds)
        {
            if (candidateIds == null) return null;
            int[] ordered = candidateIds.OrderBy(id => id).ToArray();
            return ordered.Length == 0 ? (int?)null : ordered[0];
        }
    }
}
