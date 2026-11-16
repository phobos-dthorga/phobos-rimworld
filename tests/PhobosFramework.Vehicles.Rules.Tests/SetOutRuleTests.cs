using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class SetOutRuleTests
{
    const int Inward = 0, Outward = 1, Front = 2, Back = 3;
    const int Left = 1, Right = -1;

    [Fact]
    public void TheFirstColumnTouchesTheTrailer()
    {
        // A 3-wide trailer covers x -1 to 1: its left neighbour column is x -2, its right x 2.
        Assert.Equal((-2, 0), SetOutRule.NorthFrame(3, Left, 0, 0));
        Assert.Equal((2, 0), SetOutRule.NorthFrame(3, Right, 0, 0));
    }

    [Fact]
    public void CellsAlongTheTrailerKeepTheirPlace() => Assert.Equal((-6, 3), SetOutRule.NorthFrame(3, Left, 4, 3));

    [Theory]
    [InlineData(Left)]
    [InlineData(Right)]
    public void APieceFacingInwardIsWorkedFromTheTrailersSide(int side)
    {
        // Billy's range stands 4 out; its cook must stand 3 out, between it and the trailer, on either side.
        (int x, _) = SetOutRule.NorthFrame(3, side, 4, 0);
        (int dx, int dz) = SetOutRule.WorkerOffset(SetOutRule.Rotation(Inward, side, 0));
        Assert.Equal(SetOutRule.NorthFrame(3, side, 3, 0), (x + dx, dz));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheCookStandsBetweenPieceAndTrailerHoweverTheTrailerFaces(int trailerRotation)
    {
        (int nx, int nz) = SetOutRule.NorthFrame(3, Left, 4, 1);
        (int px, int pz) = SetOutRule.Rotate(nx, nz, trailerRotation);
        (int wx, int wz) = SetOutRule.WorkerOffset(SetOutRule.Rotation(Inward, Left, trailerRotation));
        (int ex, int ez) = SetOutRule.NorthFrame(3, Left, 3, 1);
        Assert.Equal(SetOutRule.Rotate(ex, ez, trailerRotation), (px + wx, pz + wz));
    }

    [Fact]
    public void OutwardIsTheOppositeOfInward() =>
        Assert.Equal((SetOutRule.Rotation(Inward, Left, 0) + 2) % 4, SetOutRule.Rotation(Outward, Left, 0));

    [Fact]
    public void APieceFacingTheFrontIsWorkedFromItsFront() => Assert.Equal((0, 1), SetOutRule.WorkerOffset(SetOutRule.Rotation(Front, Left, 0)));

    [Fact]
    public void APieceFacingTheBackIsWorkedFromBehind() => Assert.Equal((0, -1), SetOutRule.WorkerOffset(SetOutRule.Rotation(Back, Right, 0)));

    [Fact]
    public void TheLeftOfATrailerFacingEastIsNorth()
    {
        (int nx, int nz) = SetOutRule.NorthFrame(3, Left, 0, 0);
        Assert.Equal((0, 2), SetOutRule.Rotate(nx, nz, 1));
    }
}
