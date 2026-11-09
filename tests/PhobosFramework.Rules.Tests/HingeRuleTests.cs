using System.Linq;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class HingeRuleTests
{
    [Fact]
    public void AQuarterTurnTakesNorthToEast()
    {
        (float x, float z) = HingeRule.Rotate(0f, 1f, 90f);
        Assert.Equal(1f, x, 4);
        Assert.Equal(0f, z, 4);
    }

    [Fact]
    public void AtRestNothingMoves()
    {
        (float x, float z) = HingeRule.PivotOffset(0f, -0.5f, 0f);
        Assert.Equal(0f, x, 4);
        Assert.Equal(0f, z, 4);
    }

    [Fact]
    public void TheHingeStaysPutWhenThePictureTurns()
    {
        (float px, float pz) = (0.3f, -0.5f);
        (float x, float z) offset = HingeRule.PivotOffset(px, pz, 90f);
        (float x, float z) turned = HingeRule.Rotate(px, pz, 90f);
        Assert.Equal(px, turned.x + offset.x, 4);
        Assert.Equal(pz, turned.z + offset.z, 4);
    }

    [Fact]
    public void EasingStartsAndEndsAtTheEnds()
    {
        Assert.Equal(0f, HingeRule.Eased(0f));
        Assert.Equal(1f, HingeRule.Eased(1f));
        Assert.Equal(0.5f, HingeRule.Eased(0.5f), 4);
        Assert.Equal(1f, HingeRule.Eased(2f));
    }

    [Fact]
    public void TheSweepReachesTheTipAtRestAndAtFullSwing()
    {
        var points = HingeRule.Swept(0f, -0.5f, 2.8f, 90f).ToList();
        Assert.Contains(points, p => System.Math.Abs(p.x) < 0.01f && System.Math.Abs(p.z + 3.3f) < 0.01f);
        Assert.Contains(points, p => System.Math.Abs(p.x + 2.8f) < 0.01f && System.Math.Abs(p.z + 0.5f) < 0.01f);
    }

    [Fact]
    public void AClockwiseSwingGoesToTheVehiclesLeft() => Assert.True(HingeRule.Swept(0f, 0f, 3f, 90f).Min(p => p.x) < -2.9f);

    [Fact]
    public void ACounterClockwiseSwingGoesToTheRight() => Assert.True(HingeRule.Swept(0f, 0f, 3f, -90f).Max(p => p.x) > 2.9f);
}
