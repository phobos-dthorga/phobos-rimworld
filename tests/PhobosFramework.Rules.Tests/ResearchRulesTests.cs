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

    [Fact]
    public void ASkilledFitPersonOnShiftWorks() =>
        Assert.Equal(FieldResearcherState.Working, FieldResearchRule.Researcher(capable: true, intellectual: 7, minimumIntellectual: 7, fit: true, workTime: true));

    [Fact]
    public void SevenIsEnoughAndSixIsNot() =>
        Assert.Equal(FieldResearcherState.Unskilled, FieldResearchRule.Researcher(capable: true, intellectual: 6, minimumIntellectual: 7, fit: true, workTime: true));

    [Fact]
    public void SomeoneWhoNeverResearchesSaysSoFirst() =>
        Assert.Equal(FieldResearcherState.Incapable, FieldResearchRule.Researcher(capable: false, intellectual: 0, minimumIntellectual: 7, fit: false, workTime: false));

    [Fact]
    public void ADownedOrSleepingPersonDoesNothing() =>
        Assert.Equal(FieldResearcherState.Unfit, FieldResearchRule.Researcher(capable: true, intellectual: 10, minimumIntellectual: 7, fit: false, workTime: true));

    [Fact]
    public void OffShiftMeansNoResearch() =>
        Assert.Equal(FieldResearcherState.OffShift, FieldResearchRule.Researcher(capable: true, intellectual: 10, minimumIntellectual: 7, fit: true, workTime: false));

    [Fact]
    public void AtRestTheFullRate() => Assert.Equal(1.2f, FieldResearchRule.Rate(1.5f, 0.8f, underWay: false, underWayFactor: 0.5f), 4);

    [Fact]
    public void UnderWayHalfTheRate() => Assert.Equal(0.6f, FieldResearchRule.Rate(1.5f, 0.8f, underWay: true, underWayFactor: 0.5f), 4);

    [Fact]
    public void NoLaboratoryNoResearch() => Assert.Equal(0f, FieldResearchRule.Rate(1.5f, 0f, underWay: false, underWayFactor: 0.5f));

    [Fact]
    public void AnUnderWayFactorAboveOneNeverSpeedsItUp() => Assert.Equal(1.2f, FieldResearchRule.Rate(1.5f, 0.8f, underWay: true, underWayFactor: 3f), 4);

    [Fact]
    public void LearningIsTheBenchRate() => Assert.Equal(25f, FieldResearchRule.Experience(250), 4);

    [Fact]
    public void NoTimeNoLearning() => Assert.Equal(0f, FieldResearchRule.Experience(-5));

    [Theory]
    [InlineData(0, false, false, false, FieldLabVerdict.NoLab)]
    [InlineData(1, false, false, false, FieldLabVerdict.Allowed)]
    [InlineData(1, true, true, false, FieldLabVerdict.NeedsBetterLab)]
    [InlineData(1, false, false, true, FieldLabVerdict.NeedsBetterLab)]
    [InlineData(2, true, true, true, FieldLabVerdict.Allowed)]
    [InlineData(2, true, false, false, FieldLabVerdict.NeedsOtherBench)]
    public void TheLaboratoryGradeDecidesWhichProjects(int grade, bool anyBench, bool hiTech, bool facilities, FieldLabVerdict expected) =>
        Assert.Equal(expected, FieldResearchRule.Lab(grade, anyBench, hiTech, facilities));
}
