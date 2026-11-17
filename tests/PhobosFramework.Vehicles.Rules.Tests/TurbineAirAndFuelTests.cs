using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Framework 0.132.0: thin air on a turbine and a rotor, and the fuel an aircraft's engines owe.</summary>
public class TurbineAirAndFuelTests
{
    /// <summary>The air's pressure at about 1,900 m against sea level (the standard atmosphere), a round figure for these tests.</summary>
    const double ThinAir = 0.8;

    [Fact]
    public void ThinAirLeavesATurbineLessToGive()
    {
        var spec = new TurbineSpec();
        Assert.Equal(1, TurbineModel.Derate(spec, 15), 9);
        Assert.Equal(ThinAir, TurbineModel.Derate(spec, 15, ThinAir), 9);
        // Heat and height multiply.
        Assert.Equal(TurbineModel.Derate(spec, 35) * ThinAir, TurbineModel.Derate(spec, 35, ThinAir), 9);
        // Sea-level air is the default, so every figure worked out before is unchanged.
        Assert.Equal(TurbineModel.Evaluate(spec, 0.7, 15, 1, 2, 2, true).Temperature, TurbineModel.Evaluate(spec, 0.7, 15, 1, 2, 2, true, 1).Temperature, 9);
    }

    [Fact]
    public void TheSameWorkRunsHotterInThinAir()
    {
        var spec = new TurbineSpec();
        TurbineState sea = TurbineModel.Evaluate(spec, 0.7, 15, 1, 2, 2, true);
        TurbineState high = TurbineModel.Evaluate(spec, 0.7, 15, 1, 2, 2, true, ThinAir);
        Assert.Equal(ThinAir, high.Available, 9);
        Assert.True(high.Temperature > sea.Temperature);
        Assert.True(high.SpeedPercent > sea.SpeedPercent);
    }

    [Fact]
    public void AHelicopterNeedsMoreAndHasLessUpHigh()
    {
        var spec = new TurbineSpec();
        double sea = TurbineModel.HoverPowerNeeded(0.9, 1, 15);
        double high = TurbineModel.HoverPowerNeeded(0.9, 1, 15, ThinAir);
        Assert.Equal(sea / System.Math.Sqrt(ThinAir), high, 9);
        Assert.Equal(TurbineModel.LiftPowerAvailable(spec, 15, 1, 1) * ThinAir, TurbineModel.LiftPowerAvailable(spec, 15, 1, 1, ThinAir), 9);
    }

    [Fact]
    public void AJetsThrustFallsWithTheAir()
    {
        var spec = new TurbineSpec { DeratePerDegree = 0, Ratings = { new TurbineRating { Name = "lift", ThrustKn = 100 } } };
        spec.Ratings.RemoveAt(0);
        double sea = TurbineModel.HoverMargin(spec, 8_000, 15, 1).dry;
        Assert.Equal(sea * ThinAir, TurbineModel.HoverMargin(spec, 8_000, 15, 1, ThinAir).dry, 9);
    }

    [Fact]
    public void FuelRunsFromTheIdleShareUpAndPastMaximumContinuous()
    {
        Assert.Equal(0.25, TurbineModel.FuelFraction(0.25, 0), 9);
        Assert.Equal(1, TurbineModel.FuelFraction(0.25, 1), 9);
        // Without a higher rating named, it stops at maximum continuous, as it always did.
        Assert.Equal(1, TurbineModel.FuelFraction(0.25, 1.2), 9);
        Assert.Equal(0.25 + 0.75 * 1.2, TurbineModel.FuelFraction(0.25, 1.2, 1.34), 9);
        Assert.Equal(0.25 + 0.75 * 1.34, TurbineModel.FuelFraction(0.25, 2, 1.34), 9);
    }

    [Fact]
    public void AWornHotSectionBurnsALittleMoreForTheSameWork()
    {
        Assert.Equal(1, TurbineModel.WearFuelFactor(1, 0.3), 9);
        Assert.Equal(1.03, TurbineModel.WearFuelFactor(0.9, 0.3), 9);
        Assert.Equal(1.12, TurbineModel.WearFuelFactor(0.6, 0.3), 9);
        Assert.Equal(1, TurbineModel.WearFuelFactor(1.2, 0.3), 9);
    }

    [Fact]
    public void TheAuthorsRateHoldsAtTheReferenceAndAHoverBurnsMore()
    {
        // Flying about at half of maximum continuous is what the author's rate stands for.
        Assert.Equal(1, TurbineModel.FuelShare(0.25, 0.5, 0.5, 1.34, 1), 9);
        // A hover at 1.1 of maximum continuous: (0.25 + 0.75 x 1.1) / (0.25 + 0.75 x 0.5) = 1.72 times it.
        Assert.Equal(1.72, TurbineModel.FuelShare(0.25, 1.1, 0.5, 1.34, 1), 9);
        // Idling at 0.08: about half.
        Assert.InRange(TurbineModel.FuelShare(0.25, 0.08, 0.5, 1.34, 1), 0.49, 0.5);
        // A worn engine burns more at the reference too.
        Assert.Equal(1.03, TurbineModel.FuelShare(0.25, 0.5, 0.5, 1.34, 1.03), 9);
        // A reference that burns nothing leaves the rate alone.
        Assert.Equal(1, TurbineModel.FuelShare(0, 0.5, 0, 1, 1), 9);
    }

    [Fact]
    public void TheCorrectionIsWhatIsOwedLessWhatTheTankAlreadyLost()
    {
        Assert.Equal(4, TurbineModel.FuelCorrection(10, 6, leaking: false), 9);
        // The frameworks burned more than the engines owed (an idle): the difference goes back.
        Assert.Equal(-3, TurbineModel.FuelCorrection(3, 6, leaking: false), 9);
        // A leaking tank's loss is not all engine: more is still burned where owed, but nothing is ever handed back.
        Assert.Equal(4, TurbineModel.FuelCorrection(10, 6, leaking: true), 9);
        Assert.Equal(0, TurbineModel.FuelCorrection(3, 6, leaking: true), 9);
    }
}
