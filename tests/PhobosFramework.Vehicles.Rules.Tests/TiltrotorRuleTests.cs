using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>A tiltrotor drawn as its nacelles tip: the nearest nacelle picture, the disc foreshortened by the cosine, the rotor moving
/// forward by the sine, and the tilt through a VTOL take-off and landing.</summary>
public class TiltrotorRuleTests
{
    static readonly float[] Osprey = { 0, 30, 60, 90 };

    [Theory]
    [InlineData(0, 0)]
    [InlineData(14, 0)]
    [InlineData(16, 1)]
    [InlineData(45, 1)]
    [InlineData(75, 2)]
    [InlineData(89, 3)]
    [InlineData(90, 3)]
    public void TheNearestNacellePictureIsShown(float tilt, int expected)
    {
        Assert.Equal(expected, TiltrotorRule.NearestPicture(tilt, Osprey));
    }

    [Fact]
    public void NoPicturesGiveNone()
    {
        Assert.Equal(-1, TiltrotorRule.NearestPicture(30, new float[0]));
    }

    [Fact]
    public void AnUprightRotorIsAFullCircle()
    {
        Assert.Equal(1f, TiltrotorRule.DiscLength(0), 4);
    }

    [Fact]
    public void ATiltedRotorShortensByTheCosine()
    {
        Assert.Equal(0.5f, TiltrotorRule.DiscLength(60), 4);
    }

    [Fact]
    public void AForwardRotorStillShowsAsALine()
    {
        Assert.Equal(TiltrotorRule.ThinnestDisc, TiltrotorRule.DiscLength(90), 4);
        Assert.Equal(TiltrotorRule.ThinnestDisc, TiltrotorRule.DiscLength(120), 4);
    }

    [Fact]
    public void TheRotorMovesForwardByTheSine()
    {
        Assert.Equal(0f, TiltrotorRule.RotorForward(0, 6.8f), 4);
        Assert.Equal(3.4f, TiltrotorRule.RotorForward(30, 6.8f), 4);
        Assert.Equal(6.8f, TiltrotorRule.RotorForward(90, 6.8f), 4);
    }

    [Fact]
    public void ATakeOffGoesUpUprightThenTipsForwardThroughTheClimb()
    {
        Assert.Equal(0f, TiltrotorRule.LaunchTilt(false, 0.5f, 0f));
        Assert.Equal(0f, TiltrotorRule.LaunchTilt(false, 1f, 0f));
        Assert.Equal(45f, TiltrotorRule.LaunchTilt(false, 1f, 0.5f), 3);
        Assert.Equal(90f, TiltrotorRule.LaunchTilt(false, 1f, 1f), 3);
    }

    [Fact]
    public void ALandingTipsBackDuringTheApproachThenComesDownUpright()
    {
        Assert.Equal(90f, TiltrotorRule.LaunchTilt(true, 0f, 0f), 3);
        Assert.Equal(45f, TiltrotorRule.LaunchTilt(true, 0f, 0.5f), 3);
        Assert.Equal(0f, TiltrotorRule.LaunchTilt(true, 0.4f, 1f));
    }

    [Fact]
    public void LeftAndRightRotorsTurnOppositeWays()
    {
        Assert.Equal(40f, TiltrotorRule.SpinAngle(1, 40, 0, false), 3);
        Assert.Equal(320f, TiltrotorRule.SpinAngle(1, 40, 0, true), 3);
        Assert.Equal(60f, TiltrotorRule.SpinAngle(0, 40, 60, true), 3);
    }

    [Fact]
    public void TheSpinStaysSmoothAfterLongPlay()
    {
        // 18 million ticks is 300 in-game days: as a float product, successive ticks landed on the same angle or jumped 32 degrees.
        const int late = 18_000_000;
        float one = TiltrotorRule.SpinAngle(late, 23f, 0f, false);
        float two = TiltrotorRule.SpinAngle(late + 1, 23f, 0f, false);
        float step = (two - one + 360f) % 360f;
        Assert.InRange(step, 22.9f, 23.1f);
    }
}
