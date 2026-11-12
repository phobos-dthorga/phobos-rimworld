using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class TreadRulesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0.6, 0.85)]
    [InlineData(0.9, 0.7)]
    [InlineData(1, 0.5)]
    public void TheAuthoredPointsHold(double wear, double grip) => Assert.Equal(grip, TreadRules.GripFactor(wear), 6);

    [Fact]
    public void GripFallsAsTreadWears()
    {
        double last = 2;
        for (double wear = 0; wear <= 1.0001; wear += 0.05)
        {
            double grip = TreadRules.GripFactor(wear);
            Assert.True(grip <= last);
            last = grip;
        }
    }

    [Fact]
    public void ReadingsOutsideTheRangeAreClamped()
    {
        Assert.Equal(1, TreadRules.GripFactor(-0.5), 6);
        Assert.Equal(0.5, TreadRules.GripFactor(3), 6);
    }

    [Fact]
    public void SoftGroundTyresDoNothingOnFirmGround() =>
        Assert.Equal(1, TreadRules.SoftGroundFactor(TreadRules.FirmGrip, 1.35), 6);

    [Fact]
    public void SoftGroundTyresGiveTheirFullFactorOnTheSoftestGoing() =>
        Assert.Equal(1.35, TreadRules.SoftGroundFactor(TreadRules.SoftestGrip, 1.35), 6);

    [Fact]
    public void SoftGroundTyresGiveMoreTheSofterTheGround()
    {
        double last = 0;
        for (double grip = TreadRules.FirmGrip; grip >= TreadRules.SoftestGrip - 1e-9; grip -= 0.05)
        {
            double factor = TreadRules.SoftGroundFactor(grip, 1.35);
            Assert.True(factor >= last);
            last = factor;
        }
    }

    [Fact]
    public void GroundOutsideTheCurveIsClamped()
    {
        Assert.Equal(1, TreadRules.SoftGroundFactor(0.95, 1.35), 6);
        Assert.Equal(1.35, TreadRules.SoftGroundFactor(0.1, 1.35), 6);
    }

    [Fact]
    public void OrdinaryTyresChangeNothing() => Assert.Equal(1, TreadRules.SoftGroundFactor(0.3, 1), 6);
}
