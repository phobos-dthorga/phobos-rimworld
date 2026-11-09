using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SurveyRuleTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1.5)]
    [InlineData(2, 2.25)]
    [InlineData(3, 3.375)]
    [InlineData(6, 8)]
    [InlineData(40, 8)]
    public void TheCurveFollowsFindsToItsCap(int finds, float expected) => Assert.Equal(expected, SurveyRule.TimeFactor(finds, 1.5f, 8f), 3);

    static SurveyPower Decide(bool plugged = false, bool bigBattery = false, bool hasFuel = true, bool array = false, bool on = false, bool working = true, float charge = 1f, bool was = false) =>
        SurveyRule.Decide(plugged, bigBattery, hasFuel, array, on, working, charge, was);

    [Fact]
    public void PluggedInRunsTheArrayWithTheEngineOff()
    {
        SurveyPower power = Decide(plugged: true, array: true, on: true);
        Assert.True(power.Fast);
        Assert.False(power.EngineWanted);
    }

    [Fact]
    public void WithNoBatteryNodeTheEngineRunsWhileSomeoneWorks()
    {
        Assert.True(Decide().EngineWanted);
        Assert.False(Decide(working: false).EngineWanted);
    }

    [Fact]
    public void WithNoBatteryNodeTheArrayWaitsForAPlug()
    {
        SurveyPower power = Decide(array: true, on: true);
        Assert.False(power.Fast);
        Assert.True(power.CanScan);
        Assert.Equal(SurveyNote.ArrayNoPower, power.Note);
    }

    [Fact]
    public void OutOfFuelTheConsoleStopsAtTheLowMarkSoTheTruckCanStillStart()
    {
        Assert.True(Decide(hasFuel: false, charge: 0.5f).CanScan);
        SurveyPower power = Decide(hasFuel: false, charge: 0.3f);
        Assert.False(power.CanScan);
        Assert.Equal(SurveyNote.NoPower, power.Note);
    }

    [Fact]
    public void TheBigBatteryRunsAStandardSurveyWithTheEngineOff() => Assert.False(Decide(bigBattery: true, charge: 0.9f).EngineWanted);

    [Fact]
    public void TheEngineStartsAtTheLowMark()
    {
        SurveyPower power = Decide(bigBattery: true, charge: 0.3f);
        Assert.True(power.Recharging);
        Assert.True(power.EngineWanted);
        Assert.True(power.CanScan);
    }

    [Fact]
    public void ItKeepsChargingToTheHighMark()
    {
        Assert.True(Decide(bigBattery: true, charge: 0.6f, was: true).Recharging);
        Assert.False(Decide(bigBattery: true, charge: 0.8f, was: true).Recharging);
    }

    [Fact]
    public void TheArrayStopsWhileTheBatteryRecharges()
    {
        SurveyPower power = Decide(bigBattery: true, array: true, on: true, charge: 0.3f);
        Assert.False(power.CanScan);
        Assert.False(power.Fast);
        Assert.Equal(SurveyNote.ArrayRecharging, power.Note);
    }

    [Fact]
    public void TheArrayRunsWithTheEngineHelping()
    {
        SurveyPower power = Decide(bigBattery: true, array: true, on: true, charge: 0.9f);
        Assert.True(power.Fast);
        Assert.True(power.EngineWanted);
    }
}
