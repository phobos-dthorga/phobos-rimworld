using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FittingRuleTests
{
    [Theory]
    [InlineData(1, 2, false, 1)]
    [InlineData(2, 2, false, 2)]
    [InlineData(1, 2, true, 0)]
    [InlineData(2, 2, true, 2)]
    [InlineData(3, 4, true, 0)]
    [InlineData(0, 2, true, 0)]
    [InlineData(1, 1, true, 1)]
    public void AMatchedSetCountsOnlyOnceEveryPartCarriesIt(int fitted, int parts, bool matched, int expected) =>
        Assert.Equal(expected, FittingRule.PartsCounted(fitted, parts, matched));

    [Fact]
    public void OnePartGivesTheWholeFactor() => Assert.Equal(1.2, FittingRule.PartShare(1.2, 1), 6);

    [Fact]
    public void NoPartsCountedStillGivesTheWholeFactor() => Assert.Equal(1.2, FittingRule.PartShare(1.2, 0), 6);

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    public void FittingEveryPartGivesExactlyTheListedFactor(int parts)
    {
        double total = 1;
        for (int i = 0; i < parts; i++) total *= FittingRule.PartShare(1.15, parts);
        Assert.Equal(1.15, total, 6);
    }

    [Fact]
    public void OnePartOfSeveralGivesOnlyAShare()
    {
        double share = FittingRule.PartShare(1.5, 4);
        Assert.True(share > 1 && share < 1.5);
    }

    [Fact]
    public void AFactorBelowOneShrinksTheSameWay()
    {
        double share = FittingRule.PartShare(0.96, 2);
        Assert.True(share < 1 && share > 0.96);
    }

    [Fact]
    public void ANonsenseFactorGivesNothing() => Assert.Equal(0, FittingRule.PartShare(0, 3));

    [Fact]
    public void TheMinimumSkillIsEnough()
    {
        Assert.True(FittingRule.SkilledEnough(8, 8));
        Assert.True(FittingRule.SkilledEnough(20, 8));
        Assert.False(FittingRule.SkilledEnough(7, 8));
    }

    [Theory]
    [InlineData(100, 0.7, -30)]
    [InlineData(100, 1.3, 30)]
    [InlineData(100, 1, 0)]
    public void AGradeChangesMaximumHealthByTheDifference(double baseHealth, double factor, double expected) =>
        Assert.Equal(expected, FittingRule.GradeHealthModifier(baseHealth, factor), 6);

    [Fact]
    public void APoorerPartTakesWorseFaults()
    {
        Assert.Equal(0.3, FittingRule.FaultSeverity(0.2, 1.5), 6);
        Assert.Equal(0.12, FittingRule.FaultSeverity(0.2, 0.6), 6);
    }

    [Fact]
    public void ATuneScalesTheFuelShare() => Assert.Equal(1.15, FittingRule.TunedFuelShare(1, 1.15), 6);

    [Fact]
    public void ATankAddsItsShareOfTheCapacityWithoutIt() => Assert.Equal(125, FittingRule.FuelCapacityOffset(250, 1.5), 6);

    [Fact]
    public void TheCapacityWithTheOffsetIsTheFactorTimesTheCapacityWithout() =>
        Assert.Equal(420 * 1.75, 420 + FittingRule.FuelCapacityOffset(420, 1.75), 6);

    [Theory]
    [InlineData(250, 1)]
    [InlineData(250, 0.8)]
    [InlineData(0, 1.5)]
    [InlineData(-10, 1.5)]
    public void NoTankOrNoGainAddsNothing(double capacity, double factor) => Assert.Equal(0, FittingRule.FuelCapacityOffset(capacity, factor), 6);

    [Fact]
    public void FuelAboveTheCapacityIsTheDifference() => Assert.Equal(80, FittingRule.FuelAbove(330, 250), 6);

    [Fact]
    public void FuelWithinTheCapacityIsNotAbove() => Assert.Equal(0, FittingRule.FuelAbove(200, 250), 6);

    [Theory]
    [InlineData(80.0, 80)]
    [InlineData(80.6, 80)]
    [InlineData(0.4, 0)]
    [InlineData(0, 0)]
    [InlineData(-3, 0)]
    public void OnlyWholeUnitsAreDrained(double amount, int units) => Assert.Equal(units, FittingRule.WholeUnits(amount));

    [Fact]
    public void RenewingEveryWheelInTurnLeavesNoWear()
    {
        double wear = 0.8;
        for (int left = 4; left >= 1; left--) wear = FittingRule.WearAfterRenewing(wear, left);
        Assert.Equal(0, wear, 9);
    }

    [Fact]
    public void RenewingTwoWheelsOfFourHalvesTheWear()
    {
        double wear = FittingRule.WearAfterRenewing(FittingRule.WearAfterRenewing(0.8, 4), 3);
        Assert.Equal(0.4, wear, 9);
    }

    [Fact]
    public void RenewingTheLastOrOnlyPartClearsTheWear()
    {
        Assert.Equal(0, FittingRule.WearAfterRenewing(0.5, 1), 9);
        Assert.Equal(0, FittingRule.WearAfterRenewing(0.5, 0), 9);
    }

    [Fact]
    public void NegativeWearReadsAsNone()
    {
        Assert.Equal(0, FittingRule.WearAfterRenewing(-0.2, 4), 9);
    }

    [Theory]
    [InlineData(12, 12, 1)]
    [InlineData(24, 12, 2)]
    [InlineData(48, 12, 4)]
    [InlineData(6, 12, 1)]
    [InlineData(24, 0, 1)]
    [InlineData(0, 12, 1)]
    public void A24VoltVehicleTakesTwo12VoltUnitsInSeries(double system, double unit, int expected) =>
        Assert.Equal(expected, FittingRule.UnitsInSeries(system, unit));

    [Fact]
    public void BatteriesInSeriesHoldTheirAverageCharge() =>
        Assert.Equal(0.6, FittingRule.CarriedShare(new[] { 0.4, 0.8 }, 1), 9);

    [Fact]
    public void WithNoKitReadTheFallbackCounts()
    {
        Assert.Equal(1, FittingRule.CarriedShare(System.Array.Empty<double>(), 1), 9);
        Assert.Equal(0, FittingRule.CarriedShare(System.Array.Empty<double>(), -0.5), 9);
    }

    [Fact]
    public void AKitShareOutsideNothingToFullIsHeldInside() =>
        Assert.Equal(0.5, FittingRule.CarriedShare(new[] { 1.5, -0.5 }, 1), 9);
}
