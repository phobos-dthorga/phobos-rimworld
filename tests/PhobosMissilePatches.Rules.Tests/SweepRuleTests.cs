using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Rules.Tests;

public class SweepBudgetRuleTests
{
    [Fact]
    public void AFrameAlwaysHitsTheMinimumEvenPastItsBudget()
    {
        Assert.True(SweepBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisFrame: 0));
        Assert.True(SweepBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisFrame: SweepBudgetRule.MinimumThingsPerFrame - 1));
        Assert.False(SweepBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisFrame: SweepBudgetRule.MinimumThingsPerFrame));
    }

    [Fact]
    public void WithinTheBudgetAFrameKeepsGoing()
    {
        Assert.True(SweepBudgetRule.MayContinue(elapsedMs: 11.9, budgetMs: 12f, doneThisFrame: 5000));
        Assert.False(SweepBudgetRule.MayContinue(elapsedMs: 12.0, budgetMs: 12f, doneThisFrame: 5000));
    }

    [Fact]
    public void TheBudgetStaysWithinItsBounds()
    {
        Assert.Equal(SweepBudgetRule.MinFrameBudgetMs, SweepBudgetRule.ClampBudget(0f));
        Assert.Equal(SweepBudgetRule.MaxFrameBudgetMs, SweepBudgetRule.ClampBudget(1000f));
        Assert.Equal(SweepBudgetRule.DefaultFrameBudgetMs, SweepBudgetRule.ClampBudget(SweepBudgetRule.DefaultFrameBudgetMs));
        Assert.InRange(SweepBudgetRule.DefaultFrameBudgetMs, SweepBudgetRule.MinFrameBudgetMs, SweepBudgetRule.MaxFrameBudgetMs);
    }
}

public class SweepOrderRuleTests
{
    [Fact]
    public void TheCentreGoesFirstAndTheCornersLast()
    {
        long centre = SweepOrderRule.Rank(125, 125, 125, 125);
        long near = SweepOrderRule.Rank(128, 121, 125, 125);
        long corner = SweepOrderRule.Rank(0, 249, 125, 125);
        Assert.Equal(0, centre);
        Assert.True(centre < near);
        Assert.True(near < corner);
    }

    [Fact]
    public void TheRankIsTheSameInEveryDirection()
    {
        Assert.Equal(SweepOrderRule.Rank(130, 125, 125, 125), SweepOrderRule.Rank(120, 125, 125, 125));
        Assert.Equal(SweepOrderRule.Rank(125, 130, 125, 125), SweepOrderRule.Rank(125, 120, 125, 125));
    }
}
