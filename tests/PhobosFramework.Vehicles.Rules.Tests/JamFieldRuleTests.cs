using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class JamFieldRuleTests
{
    [Theory]
    [InlineData(30, 30, 44, true)]
    [InlineData(44, 0, 44, true)]
    [InlineData(32, 32, 44, false)]
    [InlineData(0, 0, 0, false)]
    public void APointIsInsideWithinTheRadius(double dx, double dz, double radius, bool inside) => Assert.Equal(inside, JamFieldRule.Inside(dx, dz, radius));

    [Fact]
    public void OnlyTheListedCapabilitiesAreBlinded()
    {
        var jammed = new[] { "radar", "warningReceiver" };
        Assert.True(JamFieldRule.Blinds(jammed, "radar"));
        Assert.True(JamFieldRule.Blinds(jammed, "warningReceiver"));
        Assert.False(JamFieldRule.Blinds(jammed, "nightSight"));
        Assert.False(JamFieldRule.Blinds(jammed, "autopilot"));
    }
}
