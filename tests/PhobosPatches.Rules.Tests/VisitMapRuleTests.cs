using Phobos.VisitSettlementsPatches.Rules;
using Xunit;

namespace Phobos.VisitSettlementsPatches.Tests;

public class VisitMapRuleTests
{
    [Fact]
    public void ColonistsInATankKeepTheMap() =>
        // The owner's Barracuda's Stream defence, minus RimPacts: four colonists crewing the tank, nobody on foot.
        Assert.Equal(KeepReason.CrewUp, VisitMapRule.Decide(battleRunning: false, colonistsUpOnFoot: 0, crewUp: 4, mechsUp: 0));

    [Fact]
    public void ABattleKeepsTheMapEvenWithEveryoneDown() =>
        Assert.Equal(KeepReason.Battle, VisitMapRule.Decide(battleRunning: true, colonistsUpOnFoot: 0, crewUp: 0, mechsUp: 0));

    [Fact]
    public void AConsciousMechKeepsTheMap() =>
        Assert.Equal(KeepReason.MechUp, VisitMapRule.Decide(battleRunning: false, colonistsUpOnFoot: 0, crewUp: 0, mechsUp: 1));

    [Fact]
    public void WithNobodyUpAndNoBattleVisitSettlementsRuleStands() =>
        Assert.Equal(KeepReason.None, VisitMapRule.Decide(battleRunning: false, colonistsUpOnFoot: 0, crewUp: 0, mechsUp: 0));
}
