using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class NightVisitorPolicyTests
{
    [Theory]
    [InlineData(true, true, true)]    // player home at night: suppressed
    [InlineData(true, false, false)]  // player home in daylight: allowed
    [InlineData(false, true, false)]  // non-home target at night: allowed
    [InlineData(false, false, false)]
    public void NeutralArrivalsAreSuppressedOnlyDuringLocalNightOnHomeMaps(
        bool playerHome, bool night, bool expected)
        => Assert.Equal(expected, NightVisitorPolicy.SuppressesArrival(playerHome, night));
}
