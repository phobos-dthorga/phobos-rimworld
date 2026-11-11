using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The time to claim another faction's vehicle: longer for bigger vehicles, never past the limit.</summary>
public class ClaimRuleTests
{
    [Fact]
    public void BiggerVehiclesTakeLonger()
    {
        double truck = ClaimRule.Hours(15, 0.5, 0.08, 6);   // 3 by 5
        double crawler = ClaimRule.Hours(36, 0.5, 0.08, 6); // 4 by 9
        Assert.Equal(1.7, truck, 3);
        Assert.Equal(3.38, crawler, 3);
        Assert.True(crawler > truck);
    }

    [Fact]
    public void NeverPastTheLimitNorBelowNothing()
    {
        Assert.Equal(6, ClaimRule.Hours(500, 0.5, 0.08, 6), 3);
        Assert.Equal(0, ClaimRule.Hours(-4, -1, -1, 6), 3);
    }
}
