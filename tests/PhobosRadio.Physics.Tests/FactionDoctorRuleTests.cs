using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class FactionDoctorRuleTests
{
    const int Day = 60000;
    const int Hour = 2500;

    static FactionTerms Terms(FactionStanding standing, int goodwill = 20, int lastOpened = -1, int lastUse = -1, int now = 100000) =>
        FactionDoctorRule.Terms(standing, goodwill, cost: 3, floor: 0, lastOpened, lastUse, now, allyEveryTicks: Day, windowTicks: Hour);

    [Fact]
    public void AHostileFactionNeverHelps()
    {
        Assert.Equal(FactionTerms.Refused, Terms(FactionStanding.Hostile));
        Assert.Equal(FactionTerms.Refused, Terms(FactionStanding.Hostile, lastUse: 99000));
    }

    [Fact]
    public void AnAllyHelpsFreeOnceADay()
    {
        Assert.Equal(FactionTerms.Free, Terms(FactionStanding.Ally));
        Assert.Equal(FactionTerms.Refused, Terms(FactionStanding.Ally, lastOpened: 100000 - Day + 1, lastUse: 100000 - Day + 1));
        Assert.Equal(FactionTerms.Free, Terms(FactionStanding.Ally, lastOpened: 100000 - Day, lastUse: 100000 - Day));
    }

    [Fact]
    public void ACallInUseGoesOnAtNoNewCost()
    {
        Assert.Equal(FactionTerms.Continuing, Terms(FactionStanding.Ally, lastOpened: 90000, lastUse: 100000 - Hour + 1));
        Assert.Equal(FactionTerms.Continuing, Terms(FactionStanding.Neutral, goodwill: 0, lastOpened: 90000, lastUse: 99000));
    }

    [Fact]
    public void ANeutralFactionChargesGoodwillButNeverBelowTheFloor()
    {
        Assert.Equal(FactionTerms.Goodwill, Terms(FactionStanding.Neutral, goodwill: 3));
        Assert.Equal(FactionTerms.Refused, Terms(FactionStanding.Neutral, goodwill: 2));
        Assert.Equal(FactionTerms.Refused, Terms(FactionStanding.Neutral, goodwill: -40));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 10)]
    [InlineData(5, 14)]
    [InlineData(6, 16)]
    [InlineData(7, 16)]
    public void ADoctorsMedicineByHowAdvancedTheFactionIs(int tech, int level) =>
        Assert.Equal(level, FactionDoctorRule.DoctorLevel(tech, 10, 14, 16));
}
