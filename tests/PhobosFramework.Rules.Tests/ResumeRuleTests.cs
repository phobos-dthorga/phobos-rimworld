using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ResumeRuleTests
{
    const int Day = 60000;

    [Fact]
    public void AQuarterDoneLeavesThreeQuarters() => Assert.Equal(0.75, ResumeRule.ShareLeft(750, 1000), 6);

    [Fact]
    public void ANothingTotalKeepsNothing() => Assert.Equal(0, ResumeRule.ShareLeft(5, 0));

    [Fact]
    public void TheShareStaysBetweenNothingAndAll()
    {
        Assert.Equal(1, ResumeRule.ShareLeft(1500, 1000));
        Assert.Equal(0, ResumeRule.ShareLeft(-20, 1000));
    }

    [Fact]
    public void UntouchedOrFinishedWorkIsNotKept()
    {
        Assert.False(ResumeRule.WorthKeeping(1));
        Assert.False(ResumeRule.WorthKeeping(0));
        Assert.True(ResumeRule.WorthKeeping(0.4));
    }

    [Fact]
    public void NothingKeptMeansTheWholeJob() => Assert.Equal(1000, ResumeRule.WorkLeft(null, 1000), 6);

    [Fact]
    public void AKeptShareCarriesOn() => Assert.Equal(400, ResumeRule.WorkLeft(0.4, 1000), 6);

    [Fact]
    public void AChangedTotalKeepsThePointOfTheWayThrough() => Assert.Equal(200, ResumeRule.WorkLeft(0.4, 500), 6);

    [Fact]
    public void AFinishedShareStartsAfresh() => Assert.Equal(1000, ResumeRule.WorkLeft(0, 1000), 6);

    [Fact]
    public void WorkIsKeptUpToItsDays() => Assert.False(ResumeRule.Forgotten(0, ResumeRule.ForgetAfterDays * Day, Day));

    [Fact]
    public void WorkIsForgottenAfterItsDays() => Assert.True(ResumeRule.Forgotten(0, ResumeRule.ForgetAfterDays * Day + 1, Day));

    [Fact]
    public void LateGamesDoNotOverflow() => Assert.True(ResumeRule.Forgotten(int.MinValue + 10, int.MaxValue, Day));
}
