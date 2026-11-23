using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class PhaseReachRuleTests
{
    /// <summary>The fixed-wing jet profile's shape: a vertical landing read from the jump jet's own part, the rest from the vehicle.</summary>
    static readonly (string phase, string? part)[] JetRules =
    {
        ("VerticalLanding", "CompHarrier"),
        ("TakeoffRoll", null),
        ("RunwayLanding", ""),
    };

    [Fact]
    public void AJumpJetCanBeInItsVerticalLanding()
    {
        Assert.True(PhaseReachRule.Reachable("VerticalLanding", JetRules, new List<string> { "CompFueledTravel", "CompHarrier" }));
    }

    [Fact]
    public void AJetWithoutTheLandingPartNeverIs()
    {
        Assert.False(PhaseReachRule.Reachable("VerticalLanding", JetRules, new List<string> { "CompFueledTravel" }));
        Assert.False(PhaseReachRule.Reachable("VerticalLanding", JetRules, new List<string>()));
    }

    [Fact]
    public void APhaseReadFromTheVehicleItselfIsAlwaysReachable()
    {
        Assert.True(PhaseReachRule.Reachable("TakeoffRoll", JetRules, new List<string>()));
        Assert.True(PhaseReachRule.Reachable("RunwayLanding", JetRules, new List<string>()));
    }

    [Fact]
    public void APhaseNoRuleReportsIsNot()
    {
        Assert.False(PhaseReachRule.Reachable("Hover", JetRules, new List<string> { "CompHarrier" }));
    }

    [Fact]
    public void AnyOneRuleForThePhaseIsEnough()
    {
        var rules = new (string phase, string? part)[] { ("Hover", "CompMissing"), ("Hover", null) };
        Assert.True(PhaseReachRule.Reachable("Hover", rules, new List<string>()));
    }
}
