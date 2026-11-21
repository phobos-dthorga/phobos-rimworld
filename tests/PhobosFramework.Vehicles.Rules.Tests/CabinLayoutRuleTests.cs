using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Walk-in cabins (docs/development/vehicle-cabins.md): reading a layout.</summary>
public class CabinLayoutRuleTests
{
    [Fact]
    public void ASeatedCabinReadsNoseFirst()
    {
        CabinPlan plan = CabinLayoutRule.Parse(new[] { "S.S", "S.S", ".E." });
        Assert.True(plan.Valid, string.Join("; ", plan.Problems));
        Assert.Equal(3, plan.Width);
        Assert.Equal(3, plan.Length);
        // The first row is the top of the map; the way out is on the bottom row.
        Assert.Equal(CabinCell.Seat, plan.Cells[0, 2]);
        Assert.Equal(1, plan.WayOutX);
        Assert.Equal(0, plan.WayOutZ);
        Assert.Equal(4, CabinLayoutRule.Count(plan, CabinCell.Seat));
    }

    [Fact]
    public void ALitterIsAHeadOverAFoot()
    {
        CabinPlan plan = CabinLayoutRule.Parse(new[] { "L.L", "l.l", ".E." });
        Assert.True(plan.Valid, string.Join("; ", plan.Problems));
        Assert.Equal(2, CabinLayoutRule.Count(plan, CabinCell.LitterFoot));
    }

    [Fact]
    public void AHalfLitterIsRefused()
    {
        Assert.False(CabinLayoutRule.Parse(new[] { "L..", "...", ".E." }).Valid);
        Assert.False(CabinLayoutRule.Parse(new[] { "...", "l..", ".E." }).Valid);
    }

    [Fact]
    public void ACabinNeedsExactlyOneWayOut()
    {
        Assert.False(CabinLayoutRule.Parse(new[] { "S.S", "..." }).Valid);
        Assert.False(CabinLayoutRule.Parse(new[] { "E.E" }).Valid);
    }

    [Fact]
    public void RowsMustBeEvenAndKnown()
    {
        Assert.False(CabinLayoutRule.Parse(new[] { "S.S", "S.", ".E." }).Valid);
        Assert.False(CabinLayoutRule.Parse(new[] { "S?S", ".E." }).Valid);
        Assert.False(CabinLayoutRule.Parse(new string[0]).Valid);
    }
}
