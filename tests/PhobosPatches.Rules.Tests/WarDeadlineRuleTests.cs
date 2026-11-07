using Phobos.RimPactsPatches.Rules;
using Xunit;

namespace Phobos.RimPactsPatches.Tests;

public class WarDeadlineRuleTests
{
    const int Day = 60000;

    [Fact]
    public void TheWarIsLostAtTheFirstDailyCheckAfterTheDeadline() =>
        // The owner's Lance Village war: due 48,008,059; RimPacts' checks fell on whole days, and 48,060,000 failed it.
        Assert.Equal(48_060_000, WarDeadlineRule.EndTick(48_008_059, 47_880_000, Day));

    [Fact]
    public void ACheckExactlyOnTheDeadlineDecidesIt() => Assert.Equal(300_000, WarDeadlineRule.EndTick(300_000, 120_000, Day));

    [Fact]
    public void ACheckAlreadyScheduledPastTheDeadlineIsTheEnd() => Assert.Equal(350_000, WarDeadlineRule.EndTick(300_000, 350_000, Day));

    [Fact]
    public void AnUnknownScheduleFallsBackToTheDeadline() => Assert.Equal(300_000, WarDeadlineRule.EndTick(300_000, 0, Day));
}
