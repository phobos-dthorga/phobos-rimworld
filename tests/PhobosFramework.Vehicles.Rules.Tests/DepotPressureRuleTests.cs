using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class DepotPressureRuleTests
{
    [Fact]
    public void ANeighbourFindsItInAFewDays() => Assert.Equal(4, DepotPressureRule.DaysToBeFound(1)!.Value, 6);

    [Fact]
    public void NextDoorIsNeverSoonerThanTheFewest() => Assert.Equal(DepotPressureRule.FewestDays, DepotPressureRule.DaysToBeFound(0)!.Value, 6);

    [Fact]
    public void SixTilesAwayTakesAboutAMonth() => Assert.Equal(24, DepotPressureRule.DaysToBeFound(6)!.Value, 6);

    [Fact]
    public void FurtherAwayNobodyFindsIt() => Assert.Null(DepotPressureRule.DaysToBeFound(7));

    [Fact]
    public void FrostWeathers() => Assert.True(DepotPressureRule.Weathers(-3, 500));

    [Fact]
    public void AWetClimateWeathers() => Assert.True(DepotPressureRule.Weathers(15, 2500));

    [Fact]
    public void AMildDryPlaceDoesNot() => Assert.False(DepotPressureRule.Weathers(15, 800));

    [Fact]
    public void FreshFuelKeeps() => Assert.Equal(0, DepotPressureRule.StaleShare(30));

    [Fact]
    public void OldFuelGoesStale() => Assert.Equal(DepotPressureRule.StaleSharePerDay, DepotPressureRule.StaleShare(120));

    [Fact]
    public void AFractionIsCarriedNotRolled()
    {
        (int units, double carry) = DepotPressureRule.WholeUnits(50, 0.01, 0);
        Assert.Equal(0, units);
        Assert.Equal(0.5, carry, 6);
        (units, carry) = DepotPressureRule.WholeUnits(50, 0.01, carry);
        Assert.Equal(1, units);
        Assert.Equal(0, carry, 6);
    }

    [Fact]
    public void NeverMoreThanTheStock() => Assert.Equal(3, DepotPressureRule.WholeUnits(3, 1, 5).Units);

    [Fact]
    public void ToppingUpFreshensInProportion() => Assert.Equal(150, DepotPressureRule.BlendClock(100, 100, 100, 200), 6);

    [Fact]
    public void AnEmptyDepotStartsFresh() => Assert.Equal(500, DepotPressureRule.BlendClock(0, 100, 20, 500), 6);
}
