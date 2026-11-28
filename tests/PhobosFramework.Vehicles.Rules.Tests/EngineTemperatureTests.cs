using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// Each engine's own turbine temperature (Framework 0.382.0; docs/development/vehicle-schematic.md, F1): engines sharing a load work at the
/// whole's demand against their own condition, and the shown figure keeps the whole's slow cooling after a stop.
/// </summary>
public class EngineTemperatureTests
{
    static TurbineSpec OneRating() => new()
    {
        Ratings = new List<TurbineRating> { new() { Name = "maxContinuous", Share = 1, TemperatureLimit = 800 } },
    };

    [Fact]
    public void TheHotSectionConditionIsUnchanged()
    {
        Assert.Equal(1, TurbineModel.HotSectionCondition(0), 9);
        Assert.Equal(0.9, TurbineModel.HotSectionCondition(1), 9);
        Assert.Equal(0.75, TurbineModel.HotSectionCondition(2), 9);
        Assert.Equal(0.6, TurbineModel.HotSectionCondition(5), 9);
    }

    [Fact]
    public void AWornEngineRunsHotterThanItsSoundTwinAtTheSameDemand()
    {
        TurbineSpec spec = OneRating();
        double worn = TurbineModel.HotSectionCondition(1.5);
        double whole = TurbineModel.Evaluate(spec, 0.7, 15, (1 + worn) / 2, 1, true).Temperature;
        double sound = TurbineModel.Evaluate(spec, 0.7, 15, 1, 1, true).Temperature;
        double hot = TurbineModel.Evaluate(spec, 0.7, 15, worn, 1, true).Temperature;
        Assert.True(hot > whole && whole > sound);
    }

    [Fact]
    public void WhileRunningTheShownFigureIsTheEnginesOwnOnceSettled() =>
        // Settled, the whole's shown figure is its model figure, so each engine shows its own model figure.
        Assert.Equal(700, TurbineModel.EngineShownTemperature(650, 15, 650, 700), 9);

    [Fact]
    public void WhileWarmingEachEngineRisesInStepWithTheWhole() =>
        // Halfway up from 15 °C to the whole's 615 °C, an engine headed for 715 °C is halfway to it.
        Assert.Equal(365, TurbineModel.EngineShownTemperature(315, 15, 615, 715), 9);

    [Fact]
    public void AnEngineOutReadsTheOutsideAir() => Assert.Equal(15, TurbineModel.EngineShownTemperature(600, 15, 600, 15), 9);

    [Fact]
    public void WithTheEnginesStoppedEveryEngineShowsTheWholeCoolingDown() =>
        Assert.Equal(240, TurbineModel.EngineShownTemperature(240, 15, 15, 15), 9);
}
