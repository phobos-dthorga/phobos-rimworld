using System;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SelfTestTests
{
    [Fact]
    public void PassesAndFailuresAreCounted()
    {
        var test = new SelfTest();
        test.Expect("good", true);
        test.Expect("bad", false, "why");
        test.Skip("later", "no vehicle");
        Assert.Equal(1, test.Passed);
        Assert.Equal(1, test.Failed);
        Assert.Equal(1, test.Skipped);
    }

    [Fact]
    public void AStepThatThrowsIsAFailureAndTheRunCarriesOn()
    {
        var test = new SelfTest();
        bool finished = test.Step("broken", () => throw new InvalidOperationException("boom"));
        test.Expect("next", true);
        Assert.False(finished);
        Assert.Equal(1, test.Failed);
        Assert.Equal(1, test.Passed);
        Assert.Contains("boom", test.Report());
    }

    [Fact]
    public void AStepThatWorksRecordsNothingByItself()
    {
        var test = new SelfTest();
        Assert.True(test.Step("fine", () => { }));
        Assert.Empty(test.Results);
    }

    [Fact]
    public void NearAllowsTheTolerance()
    {
        var test = new SelfTest();
        test.Near("close", 100.4, 100);
        test.Near("far", 101, 100);
        Assert.Equal(1, test.Passed);
        Assert.Contains("expected 100", test.Report());
    }

    [Fact]
    public void TheReportListsFailuresAndSkipsButNotPasses()
    {
        var test = new SelfTest();
        test.Expect("quiet pass", true);
        test.Expect("loud failure", false, "because");
        test.Skip("a skip", "reason");
        string report = test.Report();
        Assert.StartsWith("1 passed, 1 failed, 1 skipped.", report);
        Assert.Contains("loud failure: because", report);
        Assert.Contains("a skip: reason", report);
        Assert.DoesNotContain("quiet pass", report);
    }

    [Fact]
    public void ACleanRunIsOneLine() => Assert.Equal("0 passed, 0 failed, 0 skipped.", new SelfTest().Report());

    [Fact]
    public void CostPerTickNeedsTicks()
    {
        Assert.Equal(0.02, TimeBudgetRule.MsPerTick(50, 2500), 6);
        Assert.Equal(0, TimeBudgetRule.MsPerTick(50, 0));
    }

    [Theory]
    [InlineData(0.04, 0.3, true)]
    [InlineData(0.05, 0.5, true)]
    [InlineData(0.06, 0.3, false)]
    [InlineData(0.04, 0.6, false)]
    public void BothBudgetsMustHold(double perTick, double worst, bool within) =>
        Assert.Equal(within, TimeBudgetRule.Within(perTick, worst, 0.05, 0.5));
}
