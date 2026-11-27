using System.Linq;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>Where a dipole's parts go (Radio 0.64.0): masts, wire and parts by shape, and turned with the building.</summary>
public class DipoleLayoutTests
{
    static readonly (double, double) Small = (0.3, 0.3);

    static System.Collections.Generic.List<DipolePiece> Lay(DipoleShape shape, bool alongZ = false, int length = 11, double mast = 4.5) =>
        DipoleLayout.Pieces(shape, length, mast, 1.5, DipoleLayout.Foot, alongZ, Small, Small, Small, Small);

    [Fact]
    public void AFlatDipoleHasAMastAtEachEndAndItsFeedInTheMiddle()
    {
        var pieces = Lay(DipoleShape.Flat);
        var masts = pieces.Where(p => p.Part == "mast").Select(p => p.X).OrderBy(x => x).ToList();
        Assert.Equal(new[] { -5.0, 5.0 }, masts);
        Assert.Equal(0, pieces.Single(p => p.Part == "feed").X, 3);
        Assert.Equal(2, pieces.Count(p => p.Part == "insulator"));
        Assert.DoesNotContain(pieces, p => p.Part == "coil" || p.Part == "peg");
    }

    [Fact]
    public void TheWireHangsFromTheMastTops()
    {
        var pieces = Lay(DipoleShape.Flat);
        double top = 4.5 * DipoleLayout.Foot - 4.5 * 0.03;
        var wires = pieces.Where(p => p.Part == "wire").ToList();
        Assert.All(wires, w => Assert.InRange(w.Z, top - 0.2, top));
        Assert.True(wires.Sum(w => w.Width) >= 10);
    }

    [Fact]
    public void AnInvertedVeeHasOneMastAndAPegAtEachEnd()
    {
        var pieces = Lay(DipoleShape.Vee, length: 7);
        Assert.Equal(0, pieces.Single(p => p.Part == "mast").X, 3);
        var pegs = pieces.Where(p => p.Part == "peg").Select(p => p.X).OrderBy(x => x).ToList();
        Assert.Equal(new[] { -3.15, 3.15 }, pegs.Select(x => System.Math.Round(x, 2)));
    }

    [Fact]
    public void AShortenedDipoleCarriesACoilInEachLeg() =>
        Assert.Equal(2, Lay(DipoleShape.Short, length: 4).Count(p => p.Part == "coil"));

    [Fact]
    public void TurnedEastTheStripRunsUpAndTheMastsStillStandUpright()
    {
        var pieces = Lay(DipoleShape.Flat, alongZ: true);
        var masts = pieces.Where(p => p.Part == "mast").ToList();
        Assert.All(masts, m => Assert.Equal(0, m.X, 3));
        Assert.All(masts, m => Assert.Equal(0, m.AngleDegrees, 3));
        Assert.True(masts.Max(m => m.Order) > pieces.Where(p => p.Part == "wire").Max(w => w.Order));
    }

    [Fact]
    public void ATallerMastLiftsTheWire()
    {
        double low = Lay(DipoleShape.Flat).Where(p => p.Part == "wire").Max(w => w.Z);
        double high = Lay(DipoleShape.Flat, mast: 8).Where(p => p.Part == "wire").Max(w => w.Z);
        Assert.True(high > low + 2);
    }
}
