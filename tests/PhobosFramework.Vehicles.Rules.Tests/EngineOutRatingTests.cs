using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Ratings allowed only with another engine out (Framework 0.250.0, per-engine realism round two).</summary>
public class EngineOutRatingTests
{
    static TurbineSpec Spec() => new()
    {
        Ratings = new List<TurbineRating>
        {
            new() { Name = "maxContinuous", Share = 1, TemperatureLimit = 810 },
            new() { Name = "maximum", Share = 1.137, TemperatureLimit = 878, Minutes = 10 },
            new() { Name = "contingency", Share = 1.167, TemperatureLimit = 903, Minutes = 2.5, EngineOutOnly = true },
        },
    };

    [Fact]
    public void WithBothEnginesTheTopIsTheLastOrdinaryRating() => Assert.Equal("maximum", Spec().Top.Name);

    [Fact]
    public void WithAnEngineOutTheContingencyIsTheTop()
    {
        TurbineSpec spec = Spec();
        spec.EngineOut = true;
        Assert.Equal("contingency", spec.Top.Name);
    }

    [Fact]
    public void PastTheTopWithBothEnginesItRunsOverItsLimit()
    {
        TurbineState state = TurbineModel.Evaluate(Spec(), 1.15, 15, 1, 1.0, true);
        Assert.True(state.OverTop > 0);
        Assert.Equal("maximum", state.Rating);
    }

    [Fact]
    public void TheSameDemandWithAnEngineOutIsInsideTheContingency()
    {
        TurbineSpec spec = Spec();
        spec.EngineOut = true;
        TurbineState state = TurbineModel.Evaluate(spec, 1.15, 15, 1, 1.0, true);
        Assert.True(state.OverTop <= 0);
        Assert.Equal("contingency", state.Rating);
    }
}
