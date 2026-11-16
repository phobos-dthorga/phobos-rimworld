using Phobos.RimPactsPatches.Rules;
using Xunit;

namespace Phobos.RimPactsPatches.Tests;

public class AssaultTargetRuleTests
{
    [Fact]
    public void AnEmptyVehicleIsNoLongerATarget() => Assert.False(AssaultTargetRule.StillLive(liveByRimPacts: true, unmannedVehicle: true));

    [Fact]
    public void ACrewedVehicleOrAPersonStaysATarget() => Assert.True(AssaultTargetRule.StillLive(liveByRimPacts: true, unmannedVehicle: false));

    [Fact]
    public void WhatRimPactsAlreadyDroppedStaysDropped()
    {
        Assert.False(AssaultTargetRule.StillLive(liveByRimPacts: false, unmannedVehicle: false));
        Assert.False(AssaultTargetRule.StillLive(liveByRimPacts: false, unmannedVehicle: true));
    }
}
