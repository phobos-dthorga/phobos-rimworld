using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class FeedRuleTests
{
    [Fact]
    public void AFittingWithNothingToAskForRuns() =>
        Assert.Equal(FeedShortfall.None, FeedRule.Check(false, false, false, false, false, 0, 0));

    [Fact]
    public void AWinchOnAFlatBatteryHasNoPower() =>
        Assert.Equal(FeedShortfall.NoPower, FeedRule.Check(false, false, true, false, false, 0, 0));

    [Fact]
    public void TheEngineIsNamedFirst() =>
        Assert.Equal(FeedShortfall.EngineOff, FeedRule.Check(true, false, true, false, true, 0, 0.2));

    [Fact]
    public void TooLittleFluidIsShort() =>
        Assert.Equal(FeedShortfall.Short, FeedRule.Check(false, false, false, false, true, 0.1, 0.2));

    [Fact]
    public void JustEnoughIsEnough() =>
        Assert.Equal(FeedShortfall.None, FeedRule.Check(true, true, true, true, true, 0.2, 0.2));
}
