using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FieldCareRuleTests
{
    [Fact]
    public void CareThatCannotWaitIsGivenOnTheGround() => Assert.True(FieldCareRule.MayActNow(CareUrgency.CannotWait, inBed: false));

    [Fact]
    public void CareThatWaitsIsNotGivenOnTheGround() => Assert.False(FieldCareRule.MayActNow(CareUrgency.WaitsForBed, inBed: false));

    [Fact]
    public void CareThatWaitsIsGivenInBed() => Assert.True(FieldCareRule.MayActNow(CareUrgency.WaitsForBed, inBed: true));

    [Fact]
    public void AHoldOnTheGroundLetsCarriersThrough() => Assert.False(FieldCareRule.HoldKeepsCarriersAway(onTheGround: true));

    [Fact]
    public void AHoldInBedKeepsOthersAway() => Assert.True(FieldCareRule.HoldKeepsCarriersAway(onTheGround: false));

    [Fact]
    public void ANearerLitterWins() => Assert.True(LitterRule.PrefersLitter(100, 400));

    [Fact]
    public void ANearerBedWins() => Assert.False(LitterRule.PrefersLitter(400, 100));

    [Fact]
    public void AnEqualDistanceGoesToBed() => Assert.False(LitterRule.PrefersLitter(100, 100));

    [Fact]
    public void NoBedMeansTheLitter() => Assert.True(LitterRule.PrefersLitter(90000, LitterRule.NoBed));

    [Fact]
    public void CarriersOnTheWayTakeLitters() => Assert.Equal(1, LitterRule.FreeForNewPatients(3, 2));

    [Fact]
    public void MoreCarriersThanLittersLeaveNone() => Assert.Equal(0, LitterRule.FreeForNewPatients(1, 3));
}
