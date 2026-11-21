using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class CampTentRuleTests
{
    [Fact]
    public void TheFlyCoversThreeRowsFromTheRear() => Assert.Equal((0, 2), CampLayoutRule.FlyRows(6, tentAtRear: false));

    [Fact]
    public void TheFlyStartsPastARoofTent() => Assert.Equal((2, 4), CampLayoutRule.FlyRows(6, tentAtRear: true));

    [Fact]
    public void AShortSideGivesAShortFly() => Assert.Equal((0, 1), CampLayoutRule.FlyRows(2, tentAtRear: false));

    [Fact]
    public void NoFlyWhereTheTentTakesTheWholeSide() => Assert.Null(CampLayoutRule.FlyRows(2, tentAtRear: true));

    [Fact]
    public void TheWalledAwningRunsTheVehiclesLength()
    {
        CampLayoutRule.Room room = CampLayoutRule.AwningRoom(7, tentAtRear: false);
        Assert.Equal((0, 0, 3, 6), (room.x0, room.z0, room.x1, room.z1));
        Assert.Equal(10, room.InsideCells);
        // The door is in the outer wall, and at no corner.
        Assert.Equal(3, room.doorX);
        Assert.InRange(room.doorZ, room.z0 + 1, room.z1 - 1);
    }

    [Fact]
    public void TheWalledAwningIsNeverShorterThanFourCells()
    {
        CampLayoutRule.Room room = CampLayoutRule.AwningRoom(2, tentAtRear: false);
        Assert.Equal(3, room.z1 - room.z0);
        Assert.Equal(4, room.InsideCells);
    }

    [Fact]
    public void TheWalledAwningStartsPastARoofTent() => Assert.Equal(2, CampLayoutRule.AwningRoom(6, tentAtRear: true).z0);

    [Fact]
    public void TheCamperHoldsTwoDoubleBedsAndAWalkway()
    {
        CampLayoutRule.Room room = CampLayoutRule.Camper();
        Assert.Equal(12, room.InsideCells);
        foreach ((int x0, int z0, int x1, int z1) in CampLayoutRule.CamperBeds())
        {
            Assert.True(x0 > room.x0 && x1 < room.x1 - 1 && z0 > room.z0 && z1 < room.z1, "a bed lies inside the walls and off the walkway");
        }
        // The door opens onto the walkway, the column just inside the outer wall.
        Assert.Equal(room.x1, room.doorX);
        Assert.InRange(room.doorZ, room.z0 + 1, room.z1 - 1);
    }

    [Theory]
    [InlineData(3, -4, 0, -2)]
    [InlineData(4, -5, 0, -3)]
    [InlineData(2, -3, 0, -2)]
    public void TheRearTentStandsOnePastEachSide(int width, int x0, int x1, int doorX)
    {
        CampLayoutRule.Room room = CampLayoutRule.RearTent(width);
        Assert.Equal((x0, -5, x1, -1), (room.x0, room.z0, room.x1, room.z1));
        Assert.Equal((doorX, -5), (room.doorX, room.doorZ));
        Assert.InRange(room.doorX, room.x0 + 1, room.x1 - 1);
        Assert.Equal((width) * 3, room.InsideCells);
    }

    [Theory]
    [InlineData(-20f, -16f)]
    [InlineData(8f, 12f)]
    [InlineData(13f, 15f)]
    [InlineData(15f, 15f)]
    [InlineData(30f, 30f)]
    public void ADeckTentIsALittleWarmerThanTheAirButNeverPastComfort(float outside, float inside) =>
        Assert.Equal(inside, CampLayoutRule.TentTemperature(outside, 4f, 15f));

    [Fact]
    public void ACabinLayoutTakesBeds()
    {
        CabinPlan plan = CabinLayoutRule.Parse(new[] { "B.B", "b.b", ".E." });
        Assert.True(plan.Valid, string.Join("; ", plan.Problems));
        Assert.Equal(2, CabinLayoutRule.Count(plan, CabinCell.BedFoot));
        Assert.Equal(2, CabinLayoutRule.Places(plan));
    }

    [Fact]
    public void ABedNeedsItsHeadAboveItsFoot()
    {
        Assert.False(CabinLayoutRule.Parse(new[] { "b.E", "B.." }).Valid);
        Assert.False(CabinLayoutRule.Parse(new[] { "B.E" }).Valid);
    }

    [Fact]
    public void PolesStandAtBothEndsOfASpan() => Assert.Equal(new[] { 2, 4 }, CampLayoutRule.PoleRowsBetween(2, 4));

    [Fact]
    public void OneRowTakesOnePole() => Assert.Equal(new[] { 6 }, CampLayoutRule.PoleRowsBetween(6, 6));

    [Fact]
    public void NoSpanNoPoles() => Assert.Empty(CampLayoutRule.PoleRowsBetween(7, 6));
}
