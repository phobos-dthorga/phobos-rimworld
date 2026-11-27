using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>An intake separator and a barrier filter that clogs (Framework 0.365.0).</summary>
public class SeparatorRuleTests
{
    [Theory]
    [InlineData(SeparatorMode.Automatic, true, 0.5, true)]
    [InlineData(SeparatorMode.Automatic, true, 0.0, false)]
    [InlineData(SeparatorMode.Automatic, false, 1.0, false)]
    [InlineData(SeparatorMode.On, false, 0.0, true)]
    [InlineData(SeparatorMode.Off, true, 1.0, false)]
    public void AnAutomaticSeparatorFollowsTheGroundAndAHeldOneDoesNot(SeparatorMode mode, bool onMap, double rough, bool on) =>
        Assert.Equal(on, SeparatorRule.On(mode, onMap, rough));

    [Fact]
    public void OnItKeepsOutHalf() => Assert.Equal(0.5, SeparatorRule.Reaching(true), 6);

    [Fact]
    public void OffEverythingReachesTheEngine() => Assert.Equal(1, SeparatorRule.Reaching(false), 6);

    [Fact]
    public void ASeparatorCostsThreePercent() => Assert.Equal(0.03, SeparatorRule.ExtraDemand(true, 0, 0), 6);

    [Fact]
    public void AWhollyCloggedBarrierCostsItsFittedShareAgain() => Assert.Equal(0.04, SeparatorRule.ExtraDemand(false, 0.04, 1), 6);

    [Fact]
    public void ACleanBarrierCostsNothingExtra() => Assert.Equal(0, SeparatorRule.ExtraDemand(false, 0.04, 0), 6);

    [Fact]
    public void ThePressesGoRound()
    {
        Assert.Equal(SeparatorMode.On, SeparatorRule.Next(SeparatorMode.Automatic));
        Assert.Equal(SeparatorMode.Off, SeparatorRule.Next(SeparatorMode.On));
        Assert.Equal(SeparatorMode.Automatic, SeparatorRule.Next(SeparatorMode.Off));
    }
}
