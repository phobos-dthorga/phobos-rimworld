using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class AxleRuleTests
{
    [Fact]
    public void FourWheelsMakeTwoAxlesFrontFirst()
    {
        // Back left, front left, back right, front right.
        List<List<int>> axles = AxleRule.Group(new[] { -1.5f, 1.5f, -1.5f, 1.5f }, 0.5f);
        Assert.Equal(2, axles.Count);
        Assert.Equal(new[] { 1, 3 }, axles[0].ToArray());
        Assert.Equal(new[] { 0, 2 }, axles[1].ToArray());
    }

    [Fact]
    public void ThreeAxlesOfASixWheeler() => Assert.Equal(3, AxleRule.Group(new[] { 2f, 2f, 0f, 0f, -2f, -2f }, 0.5f).Count);

    [Fact]
    public void WheelsNearlyLevelShareAnAxle() => Assert.Single(AxleRule.Group(new[] { 1.0f, 1.3f }, 0.5f));

    [Fact]
    public void ATrackPairIsOneGroup() => Assert.Single(AxleRule.Group(new[] { 0f, 0f }, 0.5f));

    [Fact]
    public void NoWheelsNoAxles() => Assert.Empty(AxleRule.Group(new float[0], 0.5f));
}
