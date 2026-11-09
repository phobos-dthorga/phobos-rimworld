using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ResearchRulesTests
{
    [Fact]
    public void AProjectAlreadyConsideredIsLeftAlone() =>
        Assert.Equal(ResearchGrantDecision.LeaveAlone, ResearchGrantRule.Decide(alreadyHandled: true, replacedCount: 2, unfinishedCount: 0));

    [Fact]
    public void AProjectThatReplacesNothingGrantsNothing() =>
        Assert.Equal(ResearchGrantDecision.RecordOnly, ResearchGrantRule.Decide(alreadyHandled: false, replacedCount: 0, unfinishedCount: 0));

    [Fact]
    public void AnUnfinishedOldGateGrantsNothing() =>
        Assert.Equal(ResearchGrantDecision.RecordOnly, ResearchGrantRule.Decide(alreadyHandled: false, replacedCount: 2, unfinishedCount: 1));

    [Fact]
    public void EveryOldGateFinishedGrantsTheProject() =>
        Assert.Equal(ResearchGrantDecision.Grant, ResearchGrantRule.Decide(alreadyHandled: false, replacedCount: 2, unfinishedCount: 0));

    [Fact]
    public void NothingNeededMeansNothingMissing()
    {
        Assert.Null(ResearchGateRule.FirstMissing<string>(null, _ => false));
        Assert.Null(ResearchGateRule.FirstMissing(new List<string>(), _ => false));
    }

    [Fact]
    public void TheFirstUnfinishedProjectIsNamed() =>
        Assert.Equal("b", ResearchGateRule.FirstMissing(new[] { "a", "b", "c" }, name => name == "a"));

    [Fact]
    public void AllFinishedMeansNothingMissing() =>
        Assert.Null(ResearchGateRule.FirstMissing(new[] { "a", "b" }, _ => true));

    [Fact]
    public void AnEmptyEntryIsIgnored() =>
        Assert.Null(ResearchGateRule.FirstMissing(new string[] { null!, "a" }, name => name == "a"));
}
