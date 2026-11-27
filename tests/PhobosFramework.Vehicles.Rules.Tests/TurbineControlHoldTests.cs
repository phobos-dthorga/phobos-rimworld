using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// The engine's control holds a turbine just inside its top rating outside a hover (Framework 0.372.1; docs/development/vehicle-schematic.md,
/// F6). The worked case is the C-27J's: two AE2100s with one rating, 800 °C, taking off at demand 1.0 (Vehicle_C27J.xml).
/// </summary>
public class TurbineControlHoldTests
{
    static TurbineSpec OneRating() => new()
    {
        Ratings = new List<TurbineRating> { new() { Name = "maxContinuous", Share = 1, TemperatureLimit = 800 } },
    };

    static TurbineSpec Ladder() => new()
    {
        Ratings = new List<TurbineRating>
        {
            new() { Name = "maxContinuous", Share = 1, TemperatureLimit = 775 },
            new() { Name = "takeoff", Share = 1.1, TemperatureLimit = 815, Minutes = 5 },
        },
    };

    [Fact]
    public void WithoutTheHoldAWornEngineTakesOffOverEveryLimit()
    {
        // The record's finding: a little wear (condition 0.98) and take-off's 1.0 settles at about 808 °C, over its one limit.
        TurbineState state = TurbineModel.Evaluate(OneRating(), 1.0, 15, 0.98, 1.0, true);
        Assert.True(state.OverTop > 0);
    }

    [Fact]
    public void HeldTheSameTakeOffStaysInsideItsLimit()
    {
        double held = TurbineModel.HeldDemand(OneRating(), 1.0, 15, 0.98, 1.0);
        Assert.True(held < 1.0);
        TurbineState state = TurbineModel.Evaluate(OneRating(), held, 15, 0.98, 1.0, true);
        Assert.True(state.OverTop < 0);
        Assert.True(state.OverTop > -5, "held a couple of degrees under the limit, not far below it");
    }

    [Fact]
    public void ASoundEngineOnAStandardDayIsNotHeld() =>
        Assert.Equal(0.9, TurbineModel.HeldDemand(OneRating(), 0.9, 15, 1, 1.0));

    [Fact]
    public void OnAHotDayTheHoldUsesTheTopRatingNotTheContinuousOne()
    {
        // 35 °C: 14 % derated, so 1.0 asks 1.16 of what is there; the take-off rating (1.1) is the top, so it holds at 1.1 × 0.995 of it.
        TurbineSpec spec = Ladder();
        double held = TurbineModel.HeldDemand(spec, 1.0, 35, 1, 1.0);
        double available = TurbineModel.Available(spec, 35, 1, 1.0);
        Assert.Equal(available * 1.1 * TurbineModel.ControlHoldShare, held, 9);
        TurbineState state = TurbineModel.Evaluate(spec, held, 35, 1, 1.0, true);
        Assert.Equal("takeoff", state.Rating);
        Assert.True(state.OverTop < 0);
    }

    [Fact]
    public void WithAnEngineOutTheOtherIsHeldAtItsTop()
    {
        // The record's F8: one engine of two out at cruise read 892 °C. Held, the one left gives what its top rating allows.
        double held = TurbineModel.HeldDemand(OneRating(), 0.6, 15, 1, 0.5);
        Assert.Equal(0.5 * TurbineModel.ControlHoldShare, held, 9);
        Assert.True(TurbineModel.Evaluate(OneRating(), held, 15, 1, 0.5, true).OverTop < 0);
    }

    [Fact]
    public void AvailableIsWhatEvaluateReports()
    {
        TurbineSpec spec = Ladder();
        TurbineState state = TurbineModel.Evaluate(spec, 0.7, 30, 0.9, 0.5, true, 0.8);
        Assert.Equal(state.Available, TurbineModel.Available(spec, 30, 0.9, 0.5, 0.8), 12);
    }
}
