using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class SystemIntegratorTests
{
    static readonly double[] Wide = { -1000 };
    static readonly double[] WideMax = { 1000 };

    [Fact]
    public void ASourceAddsItsRatePerHour()
    {
        var values = new[] { 10.0 };
        SystemIntegrator.Advance(values, Wide, WideMax, new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Into, Rate = 4 } }, 2.5);
        Assert.Equal(20.0, values[0], 6);
    }

    [Fact]
    public void ATowardFlowNeverOvershootsItsTarget()
    {
        var values = new[] { 100.0 };
        SystemIntegrator.Advance(values, Wide, WideMax, new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Toward, Rate = 500, Target = 20 } }, 1);
        Assert.Equal(20.0, values[0], 6);
    }

    [Fact]
    public void ATowardFlowFollowsTheExponential()
    {
        var values = new[] { 120.0 };
        SystemIntegrator.Advance(values, Wide, WideMax, new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Toward, Rate = 1, Target = 20 } }, 1);
        Assert.Equal(20 + 100 * System.Math.Exp(-1), values[0], 3);
    }

    [Fact]
    public void QuantitiesStayInsideTheirLimits()
    {
        var values = new[] { 0.5 };
        SystemIntegrator.Advance(values, new[] { 0.0 }, new[] { 1.0 }, new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.From, Rate = 10 } }, 1);
        Assert.Equal(0.0, values[0]);
    }

    [Fact]
    public void AGateOpensAcrossItsBand()
    {
        var flow = new FlowSample { OpenAbove = 88, Band = 6 };
        Assert.Equal(0.0, SystemIntegrator.Opening(85, flow));
        Assert.Equal(0.5, SystemIntegrator.Opening(91, flow), 6);
        Assert.Equal(1.0, SystemIntegrator.Opening(100, flow));
        var below = new FlowSample { OpenBelow = 0.5, Band = 0 };
        Assert.Equal(1.0, SystemIntegrator.Opening(0.2, below));
        Assert.Equal(0.0, SystemIntegrator.Opening(0.6, below));
    }
}

/// <summary>
/// The shipped cooling figures (Systems_Cooling.xml), run through the integrator: an engine at full load warms past its thermostat
/// within about a quarter hour, settles in the warm band, and overheats within the hour once the radiator is gone. If the XML
/// changes, change these figures with it.
/// </summary>
public class CoolingFigureTests
{
    const double Ambient = 20;
    const double HeatAtFullLoad = 420;   // degrees an hour at full load (authored)
    const double RadiatorRate = 6;       // share of the gap to the air closed an hour, thermostat open (authored)
    const double SoakRate = 0.6;         // share of the gap closed an hour, always (authored)
    const double ThermostatOpens = 88;
    const double ThermostatBand = 8;

    static List<FlowSample> Flows(double load, double radiatorEfficiency) => new()
    {
        new() { Quantity = 0, Kind = FlowKind.Into, Rate = HeatAtFullLoad * load },
        new() { Quantity = 0, Kind = FlowKind.Toward, Rate = RadiatorRate * radiatorEfficiency, Target = Ambient, OpenAbove = ThermostatOpens, Band = ThermostatBand },
        new() { Quantity = 0, Kind = FlowKind.Toward, Rate = SoakRate, Target = Ambient },
    };

    static double Run(double start, double load, double radiatorEfficiency, double hours)
    {
        var values = new[] { start };
        SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 200.0 }, Flows(load, radiatorEfficiency), hours);
        return values[0];
    }

    [Fact]
    public void AColdEngineWarmsWithinAQuarterHour() => Assert.True(Run(Ambient, 1, 1, 0.25) >= 80);

    [Fact]
    public void AtFullLoadItSettlesInTheWarmBand() => Assert.InRange(Run(Ambient, 1, 1, 3), 88, 110);

    // Idling, the radiator works at 0.7 (no airflow from moving).
    [Fact]
    public void IdlingItSettlesWarmToo() => Assert.InRange(Run(Ambient, 0.5, 0.7, 3), 85, 110);

    [Fact]
    public void IdlingInTheHeatItStillHolds() => Assert.InRange(RunAt(45, 0.5, 0.7, 3), 88, 115);

    static double RunAt(double air, double load, double radiator, double hours)
    {
        var values = new[] { air };
        var flows = new List<FlowSample>
        {
            new() { Quantity = 0, Kind = FlowKind.Into, Rate = HeatAtFullLoad * load },
            new() { Quantity = 0, Kind = FlowKind.Toward, Rate = RadiatorRate * radiator, Target = air, OpenAbove = ThermostatOpens, Band = ThermostatBand },
            new() { Quantity = 0, Kind = FlowKind.Toward, Rate = SoakRate, Target = air },
        };
        SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 200.0 }, flows, hours);
        return values[0];
    }

    [Fact]
    public void WithoutARadiatorItOverheatsWithinTheHour() => Assert.True(Run(90, 1, 0, 1) > 120);

    [Fact]
    public void SwitchedOffItCoolsTowardTheAir() => Assert.InRange(Run(95, 0, 1, 4), 20, 30);

    // Crosslink 3 (Systems_Cooling.xml, Framework 0.68.0): a worn engine puts a fifth more heat into its coolant at 40% health. A
    // quarter more reached 120.1 degrees C here, just into "overheating", which wears the engine.
    const double WornEngineHeat = 1.2;

    [Fact]
    public void AWornEngineWorkedHardOnAHotDayRunsHotButDoesNotBoil()
    {
        Assert.True(RunAt(35, 1, 1, 3) < 110, "a sound engine at full load in 35 °C stays in the warm band");
        double worn = RunAt(35, WornEngineHeat, 1, 3);
        Assert.InRange(worn, 110, 120);   // "running hot": seen on the gauge, no damage yet
    }

    [Fact]
    public void TwiceTheHeatWouldBoilAWornEngineOverWhichIsWhyItIsAFifth() => Assert.True(RunAt(35, 2, 1, 3) > 130);
}

/// <summary>
/// Crosslink 4 (Systems_Cooling.xml, Framework 0.69.0), the owner's rule of 10 Nov 2026: a fire put out within an in-game hour leaves
/// the engine hot but undamaged, one left to burn boils it over. Fire heats the coolant at 180 an hour and cooks the radiator at half
/// its health an hour; the radiator cools by Vehicle Framework's default efficiency curve on its health. A parked engine, stopped
/// (radiator at 0.7 for no airflow and 0.6 for no fan), in 20 degrees C air. Change these with the Defs.
/// </summary>
public class FireFigureTests
{
    const double Air = 20, FireHeat = 180, RadiatorCookedPerHour = 0.5, ParkedRadiator = 6 * 0.7 * 0.6, SoakRate = 0.6;
    const double ThermostatOpens = 88, ThermostatBand = 8, OverheatingFrom = 120, BoilingFrom = 130;

    // Vehicle Framework's default efficiency curve for a part with none of its own (read from its 1.6 source).
    static readonly (double health, double efficiency)[] DefaultEfficiency = { (0, 0), (0.25, 0), (0.4, 0.4), (0.7, 0.7), (0.85, 1), (1, 1) };

    static double Efficiency(double health)
    {
        for (int i = 1; i < DefaultEfficiency.Length; i++)
        {
            (double x1, double y1) = DefaultEfficiency[i];
            if (health > x1) continue;
            (double x0, double y0) = DefaultEfficiency[i - 1];
            return y0 + (y1 - y0) * (health - x0) / (x1 - x0);
        }
        return 1;
    }

    /// <summary>The hottest the coolant gets, burning for <paramref name="burnHours"/> and then cooling, over three hours.</summary>
    static double Hottest(double start, double burnHours)
    {
        const double Step = 0.02;
        var values = new[] { start };
        double radiatorHealth = 1, hottest = start;
        for (double t = 0; t < 3; t += Step)
        {
            bool burning = t < burnHours;
            if (burning) radiatorHealth = System.Math.Max(0, radiatorHealth - RadiatorCookedPerHour * Step);
            var flows = new List<FlowSample>
            {
                new() { Quantity = 0, Kind = FlowKind.Toward, Rate = ParkedRadiator * Efficiency(radiatorHealth), Target = Air, OpenAbove = ThermostatOpens, Band = ThermostatBand },
                new() { Quantity = 0, Kind = FlowKind.Toward, Rate = SoakRate, Target = Air },
            };
            if (burning) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Into, Rate = FireHeat });
            SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 200.0 }, flows, Step);
            hottest = System.Math.Max(hottest, values[0]);
        }
        return hottest;
    }

    [Theory]
    [InlineData(20)]   // a cold, parked vehicle
    [InlineData(90)]   // one just driven
    public void AFirePutOutWithinTheHourLeavesTheEngineHotButUndamaged(double start)
    {
        double hottest = Hottest(start, 1);
        Assert.True(hottest > ThermostatOpens, "the engine is hot: " + hottest);
        Assert.True(hottest < OverheatingFrom, "but nothing is damaged: " + hottest);
    }

    [Fact]
    public void AFireLeftToBurnBoilsTheEngineOver() => Assert.True(Hottest(90, 2) > BoilingFrom);
}

public class StageRuleTests
{
    static readonly double[] From = { double.NaN, 40, 110, 120 };
    static readonly double[] To = { 40, 110, 120, double.NaN };

    [Theory]
    [InlineData(-30, 0)]
    [InlineData(40, 1)]
    [InlineData(109.9, 1)]
    [InlineData(110, 2)]
    [InlineData(500, 3)]
    public void AValueFallsInOneStage(double value, int stage) => Assert.Equal(stage, StageRule.StageIndex(value, From, To));

    [Fact]
    public void AValueInAGapIsInNoStage() => Assert.Equal(-1, StageRule.StageIndex(5, new[] { 10.0 }, new[] { 20.0 }));

    [Fact]
    public void OrderIsChecked()
    {
        Assert.True(StageRule.Ordered(From, To));
        Assert.False(StageRule.Ordered(new[] { 50.0, 40 }, new[] { 60.0, 45 }));
        Assert.False(StageRule.Ordered(new[] { 10.0 }, new[] { 10.0 }));
    }
}

public class SmallRuleTests
{
    [Fact]
    public void TheAlternatorCoversTheLoadsBeforeTheBattery()
    {
        Assert.Equal(60, ElectricalBalance.Surplus(90, 30));
        Assert.Equal(0, ElectricalBalance.Deficit(90, 30));
        Assert.Equal(25, ElectricalBalance.Deficit(0, 25));
    }

    [Fact]
    public void FailureOddsFollowTheMeanTime()
    {
        Assert.Equal(0, FailureRule.ChanceIn(0, 100));
        Assert.Equal(1 - System.Math.Exp(-1), FailureRule.ChanceIn(100, 100), 9);
        Assert.Equal(1, FailureRule.ChanceIn(1, 0));
    }

    [Fact]
    public void ANamedVehicleBeatsAModWhichBeatsAnEffect()
    {
        int pick = LoadoutChoice.Pick(new[] { LoadoutPriority.Default, LoadoutPriority.Vehicle, LoadoutPriority.Mod }, out bool tied);
        Assert.Equal(1, pick);
        Assert.False(tied);
    }

    [Fact]
    public void AmongEqualsTheLastLoadedWinsAndSaysSo()
    {
        int pick = LoadoutChoice.Pick(new[] { LoadoutPriority.Mod, LoadoutPriority.Mod }, out bool tied);
        Assert.Equal(1, pick);
        Assert.True(tied);
        Assert.Equal(-1, LoadoutChoice.Pick(new LoadoutPriority[0], out _));
    }

    [Fact]
    public void RankBreaksATieWithinAPriorityButNeverBeatsAHigherPriority()
    {
        // A tracked heavy vehicle matches the heavy and the tracked loadouts (both Effect): the tracked one's rank wins, untied.
        int pick = LoadoutChoice.Pick(new[] { LoadoutPriority.Effect, LoadoutPriority.Effect }, new[] { 1, 0 }, out bool tied);
        Assert.Equal(0, pick);
        Assert.False(tied);
        // A named loadout (Vehicle) beats any rank below it.
        Assert.Equal(1, LoadoutChoice.Pick(new[] { LoadoutPriority.Effect, LoadoutPriority.Vehicle }, new[] { 5, 0 }, out _));
        // Equal priority and rank: the last loaded wins and says so.
        Assert.Equal(1, LoadoutChoice.Pick(new[] { LoadoutPriority.Effect, LoadoutPriority.Effect }, new[] { 1, 1 }, out tied));
        Assert.True(tied);
    }

    [Fact]
    public void OverridesReplaceDefaultsAndUnknownNamesAreReported()
    {
        var unknown = new List<string>();
        var resolved = ParameterRule.Resolve(
            new Dictionary<string, double> { ["batteryAmpHours"] = 45, ["alternatorAmps"] = 60 },
            new[] { new KeyValuePair<string, double>("alternatorAmps", 140), new KeyValuePair<string, double>("typo", 1) }, unknown);
        Assert.Equal(45, resolved["batteryAmpHours"]);
        Assert.Equal(140, resolved["alternatorAmps"]);
        Assert.Equal(new[] { "typo" }, unknown);
    }

    [Fact]
    public void AHitCountsBySharesSoBigAndSmallPartsAgree()
    {
        Assert.Equal(-0.45, PartHitMath.Amount(0, -0.9, 15, 30), 9);
        Assert.Equal(-0.45, PartHitMath.Amount(0, -0.9, 95, 190), 9);
    }

    [Fact]
    public void AHitCanStillCountByPointsAndNothingLostIsNothing()
    {
        Assert.Equal(-0.15, PartHitMath.Amount(-0.015, 0, 10, 30), 9);
        Assert.Equal(0, PartHitMath.Amount(-0.015, -0.9, 0, 30));
        Assert.Equal(-0.15, PartHitMath.Amount(-0.015, -0.9, 10, 0), 9);
    }

    [Fact]
    public void ElapsedHoursAreCapped()
    {
        Assert.Equal(0, ElapsedRule.Hours(1000, 1000, 24));
        Assert.Equal(0, ElapsedRule.Hours(1000, -1, 24));
        Assert.Equal(0.1, ElapsedRule.Hours(1250, 1000, 24), 9);
        Assert.Equal(24, ElapsedRule.Hours(10_000_000, 0, 24));
    }
}
