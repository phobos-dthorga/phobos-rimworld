using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>"Any one of" upgrade prerequisites, on the tanker's tree: compartments need a liquid tank and hold it on; the hose reel needs any
/// tank and lets it swap.</summary>
public class UpgradeAnyOfRuleTests
{
    static readonly string[] Liquid = { "LiquidTank", "InsulatedLiquidTank", "HighVolumeLiquidTank" };
    static readonly string[] Any = { "LiquidTank", "InsulatedLiquidTank", "HighVolumeLiquidTank", "PressureVessel" };

    static readonly UpgradeAnyOfLink[] Links =
    {
        new("TwoCompartments", Liquid, HoldsParent: true),
        new("HoseReel", Any, HoldsParent: false),
    };

    static System.Func<string, bool> Fitted(params string[] keys) => new HashSet<string>(keys).Contains;

    [Fact]
    public void NoTankLeavesTheCompartmentsUnmet()
    {
        Assert.NotNull(UpgradeAnyOfRule.Unmet("TwoCompartments", Links, Fitted()));
    }

    [Fact]
    public void AnyOneLiquidTankMeetsTheCompartments()
    {
        foreach (string tank in Liquid) Assert.Null(UpgradeAnyOfRule.Unmet("TwoCompartments", Links, Fitted(tank)));
    }

    [Fact]
    public void AGasVesselDoesNotMeetTheCompartmentsButMeetsTheHoseReel()
    {
        Assert.NotNull(UpgradeAnyOfRule.Unmet("TwoCompartments", Links, Fitted("PressureVessel")));
        Assert.Null(UpgradeAnyOfRule.Unmet("HoseReel", Links, Fitted("PressureVessel")));
    }

    [Fact]
    public void ANodeWithNoLinkIsAlwaysMet()
    {
        Assert.Null(UpgradeAnyOfRule.Unmet("ElectricPump", Links, Fitted()));
    }

    [Fact]
    public void CompartmentsHoldTheirTankOn()
    {
        Assert.Equal("TwoCompartments", UpgradeAnyOfRule.HeldBy("InsulatedLiquidTank", Links, Fitted("InsulatedLiquidTank", "TwoCompartments")));
    }

    [Fact]
    public void AHoseReelLetsTheTankSwap()
    {
        Assert.Null(UpgradeAnyOfRule.HeldBy("LiquidTank", Links, Fitted("LiquidTank", "HoseReel")));
    }

    [Fact]
    public void AnotherFittedParentFreesTheFirst()
    {
        Assert.Null(UpgradeAnyOfRule.HeldBy("LiquidTank", Links, Fitted("LiquidTank", "HighVolumeLiquidTank", "TwoCompartments")));
    }

    [Fact]
    public void AnUnfittedParentIsNeverHeld()
    {
        Assert.Null(UpgradeAnyOfRule.HeldBy("LiquidTank", Links, Fitted("HighVolumeLiquidTank", "TwoCompartments")));
    }
}
