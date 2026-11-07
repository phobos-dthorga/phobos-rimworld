using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Rules.Tests;

public class FireHeatRuleTests
{
    const float Full = 1.75f;
    const float Peak = 600f;
    const float Radius = 5f;

    [Fact]
    public void AFullFireGivesThePeakAtItsOwnCellAndFallsWithTheSquareOfDistancePlusOne()
    {
        Assert.Equal(600f, FireHeatRule.Rise(Full, Full, 0f, Peak, Radius), 3);
        Assert.Equal(150f, FireHeatRule.Rise(Full, Full, 1f, Peak, Radius), 3);
        Assert.Equal(600f / 9f, FireHeatRule.Rise(Full, Full, 2f, Peak, Radius), 3);
        Assert.Equal(600f / 36f, FireHeatRule.Rise(Full, Full, 5f, Peak, Radius), 3);
    }

    [Fact]
    public void ASmallerFireScalesDownAndAnOversizedOneNeverExceedsThePeak()
    {
        Assert.Equal(600f * 0.1f / 1.75f, FireHeatRule.Rise(0.1f, Full, 0f, Peak, Radius), 3);
        Assert.Equal(600f, FireHeatRule.Rise(3f, Full, 0f, Peak, Radius), 3);
    }

    [Fact]
    public void NothingBeyondTheRadiusOrWithoutAFire()
    {
        Assert.Equal(0f, FireHeatRule.Rise(Full, Full, 5.1f, Peak, Radius));
        Assert.Equal(0f, FireHeatRule.Rise(0f, Full, 0f, Peak, Radius));
        Assert.Equal(0f, FireHeatRule.Rise(Full, Full, 0f, 0f, Radius));
        Assert.Equal(0f, FireHeatRule.Rise(Full, 0f, 0f, Peak, Radius));
    }

    [Fact]
    public void ASmallRoomAddsNothingAndAHangarNearlyAll()
    {
        Assert.Equal(0f, FireHeatRule.IndoorShare(20, 30f));
        Assert.Equal(0f, FireHeatRule.IndoorShare(30, 30f));
        Assert.Equal(0.25f, FireHeatRule.IndoorShare(40, 30f), 4);
        Assert.Equal(0.9f, FireHeatRule.IndoorShare(300, 30f), 4);
        Assert.Equal(0f, FireHeatRule.IndoorShare(0, 30f));
        Assert.Equal(1f, FireHeatRule.IndoorShare(10, 0f));
    }

    [Fact]
    public void AirBurnsOnlyPastTheGamesMarginOverComfort()
    {
        Assert.False(FireHeatRule.BurnsAt(176f, 26f));
        Assert.True(FireHeatRule.BurnsAt(176.5f, 26f));
        Assert.False(FireHeatRule.BurnsAt(200f, 60f));
    }

    [Fact]
    public void SeveralFiresAddUpToTheCapAndNoFurther()
    {
        float three = 3f * FireHeatRule.Rise(Full, Full, 0f, Peak, Radius);
        Assert.Equal(900f, FireHeatRule.Cap(three, 900f));
        Assert.Equal(150f, FireHeatRule.Cap(150f, 900f));
    }
}
