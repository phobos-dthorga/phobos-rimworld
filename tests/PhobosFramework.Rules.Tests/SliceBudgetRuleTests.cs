using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SliceBudgetRuleTests
{
    [Fact]
    public void ASliceAlwaysTakesItsMinimumEvenPastItsBudget()
    {
        Assert.True(SliceBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisSlice: 0, minimumSteps: 8));
        Assert.True(SliceBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisSlice: 7, minimumSteps: 8));
        Assert.False(SliceBudgetRule.MayContinue(elapsedMs: 500, budgetMs: 12f, doneThisSlice: 8, minimumSteps: 8));
    }

    [Fact]
    public void WithinTheBudgetASliceKeepsGoing()
    {
        Assert.True(SliceBudgetRule.MayContinue(elapsedMs: 11.9, budgetMs: 12f, doneThisSlice: 5000, minimumSteps: 1));
        Assert.False(SliceBudgetRule.MayContinue(elapsedMs: 12.0, budgetMs: 12f, doneThisSlice: 5000, minimumSteps: 1));
    }

    [Fact]
    public void ClampKeepsAValueInItsBounds()
    {
        Assert.Equal(2f, SliceBudgetRule.Clamp(0f, 2f, 50f));
        Assert.Equal(50f, SliceBudgetRule.Clamp(1000f, 2f, 50f));
        Assert.Equal(12f, SliceBudgetRule.Clamp(12f, 2f, 50f));
    }
}
