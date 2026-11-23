using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Per-engine output (Framework 0.247.0): a damaged engine gives part of its share.</summary>
public class EngineOutputTests
{
    [Fact]
    public void ASoundEngineGivesAll() => Assert.Equal(1, TurbineModel.EngineOutput(1), 6);

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.2)]
    public void AnEngineAtOrBelowTheLineIsOut(double order) => Assert.Equal(0, TurbineModel.EngineOutput(order));

    [Fact]
    public void JustAboveTheLineItGivesAboutHalf() =>
        Assert.InRange(TurbineModel.EngineOutput(0.21), TurbineModel.DamagedEngineFloor, TurbineModel.DamagedEngineFloor + 0.02);

    [Fact]
    public void OutputRisesWithCondition() => Assert.True(TurbineModel.EngineOutput(0.8) > TurbineModel.EngineOutput(0.5));

    [Fact]
    public void ByCountAHalfDamagedTwinStillGivesEverything() =>
        Assert.Equal(1, TurbineModel.OutputShare(new[] { 1.0, 0.5 }, byHealth: false), 6);

    [Fact]
    public void ByHealthAHalfDamagedTwinGivesLess()
    {
        double share = TurbineModel.OutputShare(new[] { 1.0, 0.5 }, byHealth: true);
        Assert.True(share < 1 && share > 0.5);
    }

    [Fact]
    public void OneEngineOutOfTwoGivesHalfEitherWay()
    {
        Assert.Equal(0.5, TurbineModel.OutputShare(new[] { 1.0, 0.1 }, byHealth: true), 6);
        Assert.Equal(0.5, TurbineModel.OutputShare(new[] { 1.0, 0.1 }, byHealth: false), 6);
    }

    [Fact]
    public void NoEnginesListedChangesNothing() => Assert.Equal(1, TurbineModel.OutputShare(System.Array.Empty<double>(), byHealth: true));

    [Fact]
    public void TheShareOverloadMatchesTheCountOne()
    {
        var spec = new TurbineSpec();
        TurbineState byCount = TurbineModel.Evaluate(spec, 0.6, 15, 1, 1, 2, true);
        TurbineState byShare = TurbineModel.Evaluate(spec, 0.6, 15, 1, 0.5, true);
        Assert.Equal(byCount.Available, byShare.Available, 6);
        Assert.Equal(byCount.Temperature, byShare.Temperature, 6);
    }
}
