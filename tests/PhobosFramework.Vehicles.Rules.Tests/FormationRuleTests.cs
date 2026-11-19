using System;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class FormationRuleTests
{
    [Fact]
    public void TheFirstGoesToTheSpot()
    {
        Assert.Equal((0.0, 0.0), FormationRule.Offset(0, 10));
    }

    [Fact]
    public void TheNextSixRingTheSpotOneSpacingOut()
    {
        for (int i = 1; i <= 6; i++)
        {
            (double x, double z) = FormationRule.Offset(i, 10);
            Assert.Equal(10, Math.Sqrt(x * x + z * z), 6);
        }
        (double nx, double nz) = FormationRule.Offset(1, 10);
        Assert.Equal(0, nx, 6);
        Assert.Equal(10, nz, 6);
    }

    [Fact]
    public void TheSeventhAircraftStartsTheSecondRing()
    {
        (double x, double z) = FormationRule.Offset(7, 10);
        Assert.Equal(20, Math.Sqrt(x * x + z * z), 6);
        (double x19, double z19) = FormationRule.Offset(19, 10);
        Assert.Equal(30, Math.Sqrt(x19 * x19 + z19 * z19), 6);
    }

    [Fact]
    public void NoTwoInARingShareAPlace()
    {
        (double ax, double az) = FormationRule.Offset(2, 10);
        (double bx, double bz) = FormationRule.Offset(3, 10);
        Assert.True(Math.Abs(ax - bx) + Math.Abs(az - bz) > 1);
    }
}
