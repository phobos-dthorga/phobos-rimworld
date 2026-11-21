using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class NightDriveRuleTests
{
    [Theory]
    [InlineData(0.31, true)]
    [InlineData(0.3, false)]
    [InlineData(0.05, false)]
    public void ADriverIsFitAboveTheLine(double rest, bool fit) => Assert.Equal(fit, NightDriveRule.Fit(rest, 0.3));

    [Fact]
    public void SomeoneWhoNeverTiresIsAlwaysFit() => Assert.True(NightDriveRule.Fit(null, 0.3));

    [Fact]
    public void TheMostRestedDriverTakesOver()
    {
        var riders = new List<(double?, bool)> { (0.7, true), (0.95, false), (0.9, true), (0.5, true) };
        Assert.Equal(2, NightDriveRule.Relief(riders, 0.6));
    }

    [Fact]
    public void NobodyRestedEnoughMeansNoRelief()
    {
        var riders = new List<(double?, bool)> { (0.6, true), (0.4, true), (0.99, false) };
        Assert.Equal(-1, NightDriveRule.Relief(riders, 0.6));
    }

    [Fact]
    public void ARiderWhoNeverTiresCanTakeOver() => Assert.Equal(0, NightDriveRule.Relief(new List<(double?, bool)> { (null, true) }, 0.6));

    [Fact]
    public void TheBunkGoesToTheMostTiredPassenger()
    {
        var riders = new List<(double?, bool, bool)> { (0.2, true, false), (0.5, false, false), (0.3, false, false), (0.1, false, true) };
        // The driver (crew) and the one already in a bed are passed over.
        Assert.Equal(2, NightDriveRule.BunkFor(riders, 0.95));
    }

    [Fact]
    public void NobodyTiredLeavesTheBunkEmpty() => Assert.Equal(-1, NightDriveRule.BunkFor(new List<(double?, bool, bool)> { (0.97, false, false) }, 0.95));

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void ABunkTakesASeatOnlyInALargerCab(int seats, bool takes) => Assert.Equal(takes, NightDriveRule.BunkTakesSeat(seats));
}
