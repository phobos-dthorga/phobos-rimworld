using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class CampLayoutRuleTests
{
    // A 2-wide, 5-long car standing on x 10..11, z 20..24.
    static CampLayoutRule.Frame Car(int facing, int side) => facing % 2 == 0
        ? new CampLayoutRule.Frame(10, 20, 11, 24, facing, side)
        : new CampLayoutRule.Frame(10, 20, 14, 21, facing, side);

    [Fact]
    public void FacingNorthTheLeftCampTouchesTheWestSideFromTheRear()
    {
        CampLayoutRule.Frame frame = Car(0, CampLayoutRule.Left);
        Assert.Equal((9, 20), frame.ToMap(0, 0));
        Assert.Equal((8, 24), frame.ToMap(1, 4));
        Assert.Equal(5, frame.length);
    }

    [Fact]
    public void FacingNorthTheRightCampTouchesTheEastSideOfAnEvenWidth()
    {
        // The car covers x 10 and 11: its right neighbour column is 12, not inside it.
        Assert.Equal((12, 20), Car(0, CampLayoutRule.Right).ToMap(0, 0));
    }

    [Theory]
    [InlineData(1, CampLayoutRule.Left, 10, 22)]
    [InlineData(1, CampLayoutRule.Right, 10, 19)]
    [InlineData(2, CampLayoutRule.Left, 12, 24)]
    [InlineData(3, CampLayoutRule.Left, 14, 19)]
    public void EveryFacingStartsAtTheRearCornerOnItsSide(int facing, int side, int x, int z) => Assert.Equal((x, z), Car(facing, side).ToMap(0, 0));

    [Theory]
    [InlineData(0, CampLayoutRule.Left)]
    [InlineData(1, CampLayoutRule.Right)]
    [InlineData(2, CampLayoutRule.Left)]
    [InlineData(3, CampLayoutRule.Right)]
    public void NoCampCellIsInsideTheVehicle(int facing, int side)
    {
        CampLayoutRule.Frame frame = Car(facing, side);
        (int minX, int minZ, int maxX, int maxZ) = facing % 2 == 0 ? (10, 20, 11, 24) : (10, 20, 14, 21);
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < frame.length; z++)
            {
                (int mx, int mz) = frame.ToMap(x, z);
                Assert.False(mx >= minX && mx <= maxX && mz >= minZ && mz <= maxZ);
            }
        }
    }

    [Theory]
    [InlineData(0, CampLayoutRule.Left)]
    [InlineData(1, CampLayoutRule.Left)]
    [InlineData(2, CampLayoutRule.Right)]
    [InlineData(3, CampLayoutRule.Right)]
    public void FromMapUndoesToMap(int facing, int side)
    {
        CampLayoutRule.Frame frame = Car(facing, side);
        foreach ((int x, int z) in new[] { (0, 0), (1, 3), (4, -1), (2, 6) })
        {
            (int mx, int mz) = frame.ToMap(x, z);
            Assert.Equal((x, z), frame.FromMap(mx, mz));
        }
    }

    [Theory]
    [InlineData(0, CampLayoutRule.Left)]
    [InlineData(1, CampLayoutRule.Right)]
    [InlineData(2, CampLayoutRule.Left)]
    [InlineData(3, CampLayoutRule.Right)]
    public void RotationsRoundTripAndInPointsAtTheVehicle(int facing, int side)
    {
        CampLayoutRule.Frame frame = Car(facing, side);
        for (int direction = 0; direction < 4; direction++) Assert.Equal(direction, frame.FromMapRotation(frame.ToMapRotation(direction)));
        (int sx, int sz) = CampLayoutRule.Step(frame.ToMapRotation(CampLayoutRule.In));
        (int x, int z) = frame.ToMap(1, 2);
        Assert.Equal(frame.ToMap(0, 2), (x + sx, z + sz));
        Assert.Equal(facing, frame.ToMapRotation(CampLayoutRule.Forward));
    }

    [Fact]
    public void SwagsFillTheFirstBandFromTheRearBeforeTheNext()
    {
        var slots = CampLayoutRule.Slots(length: 3, along: 1, outward: 2, maxOut: 6).ToList();
        Assert.Equal((0, 0), slots[0]);
        Assert.Equal((0, 2), slots[2]);
        Assert.Equal((2, 0), slots[3]);
        Assert.DoesNotContain(slots, s => s.x + 2 > 6);
    }

    [Fact]
    public void AWidePieceStillHasASlotBesideAShortVehicle() => Assert.Contains((0, 0), CampLayoutRule.Slots(1, 2, 2, 6));

    [Theory]
    [InlineData(1, false, new[] { 0 })]
    [InlineData(5, false, new[] { 0, 4 })]
    [InlineData(10, false, new[] { 0, 4, 9 })]
    [InlineData(5, true, new[] { 2, 4 })]
    [InlineData(2, true, new int[0])]
    public void PolesStandAtTheEndsAndNoFurtherApartThanTheSpacing(int length, bool tent, int[] expected)
    {
        var rows = CampLayoutRule.PoleRows(length, tent);
        Assert.Equal(expected, rows);
        for (int index = 1; index < rows.Count; index++) Assert.True(rows[index] - rows[index - 1] <= CampLayoutRule.PoleSpacing);
    }

    [Fact]
    public void ATentNeedsTwoRows()
    {
        Assert.False(CampLayoutRule.TentFits(1));
        Assert.True(CampLayoutRule.TentFits(2));
    }
}
