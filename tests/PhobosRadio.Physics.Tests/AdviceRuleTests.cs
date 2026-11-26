using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class AdviceRuleTests
{
    [Theory]
    [InlineData(LinkOutcome.Clean, 0.5)]
    [InlineData(LinkOutcome.Workable, 0.5)]
    [InlineData(LinkOutcome.Marginal, 0.25)]
    [InlineData(LinkOutcome.NoContact, 0.0)]
    public void TheOwnersShares(LinkOutcome outcome, double expected) => Assert.Equal(expected, AdviceRule.Share(outcome, 0.5, 0.25, 0.0), 9);

    [Fact]
    public void AnAidAddsItsShareButNeverPastAll()
    {
        Assert.Equal(0.25 + 1.0 / 6.0, AdviceRule.Share(LinkOutcome.Marginal, 0.5, 0.25, 1.0 / 6.0), 9);
        Assert.Equal(1.0, AdviceRule.Share(LinkOutcome.Clean, 0.9, 0.25, 0.5), 9);
    }

    [Fact]
    public void AnAidDoesNothingWithNoContact() => Assert.Equal(0.0, AdviceRule.Share(LinkOutcome.NoContact, 0.5, 0.25, 1.0 / 6.0), 9);

    [Fact]
    public void TheRecordsExampleMedicineThreeTalkedThroughBySixteen() => Assert.Equal(9, AdviceRule.EffectiveLevel(3, 16, 0.5));

    [Fact]
    public void ALevelIsRoundedDown() => Assert.Equal(4, AdviceRule.EffectiveLevel(3, 6, 0.5));

    [Fact]
    public void AnAdviserWhoKnowsNoMoreChangesNothing()
    {
        Assert.Equal(9, AdviceRule.EffectiveLevel(9, 8, 0.5));
        Assert.Equal(9, AdviceRule.EffectiveLevel(9, 9, 0.5));
    }

    [Fact]
    public void NeverPastTheAdviser() => Assert.Equal(12, AdviceRule.EffectiveLevel(0, 12, 1.0));

    [Fact]
    public void TheWorseLinkDecides()
    {
        Assert.Equal(LinkOutcome.Marginal, AdviceRule.Worse(LinkOutcome.Clean, LinkOutcome.Marginal));
        Assert.Equal(LinkOutcome.NoContact, AdviceRule.Worse(LinkOutcome.NoContact, LinkOutcome.Workable));
    }
}
