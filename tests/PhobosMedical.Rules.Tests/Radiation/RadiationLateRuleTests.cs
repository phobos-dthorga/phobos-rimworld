using Phobos.Radiation.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests.Radiation;

public class RadiationLateRuleTests
{
    // ---- Timing ----

    [Fact]
    public void LateEffectsComeAtAQuarterOfTheRealTime() => Assert.Equal(30, RadiationLateRule.DaysFor(2), 6);

    [Fact]
    public void ModeratesHalfSparesHalf()
    {
        Assert.False(RadiationLateRule.Spared(0.5, 0.4));
        Assert.True(RadiationLateRule.Spared(0.5, 0.6));
    }

    [Fact]
    public void GentleSparesEveryone() => Assert.True(RadiationLateRule.Spared(0, 0.0));

    [Fact]
    public void RealisticSparesNoOne() => Assert.False(RadiationLateRule.Spared(1, 0.999));

    // ---- Cancer ----

    [Fact]
    public void CancerRisesWithTheLifetimeDose()
    {
        double total = RadiationLateRule.LeukaemiaChance(4) + RadiationLateRule.SolidCancerChance(4);
        Assert.Equal(0.22, total, 6);
        Assert.True(RadiationLateRule.SolidCancerChance(4) > RadiationLateRule.LeukaemiaChance(4));
    }

    [Fact]
    public void NoDoseNoCancer() => Assert.Equal(0, RadiationLateRule.LeukaemiaChance(0) + RadiationLateRule.SolidCancerChance(0), 9);

    [Fact]
    public void LeukaemiaComesBeforeSolidCancers() => Assert.True(RadiationLateRule.LeukaemiaYears(1) <= RadiationLateRule.SolidCancerYears(0));

    // ---- Cataract ----

    [Fact]
    public void NoCataractBelowHalfAGray() => Assert.Equal(0, RadiationLateRule.CataractChance(0.5), 9);

    [Fact]
    public void ACataractIsCertainByFiveGrays() => Assert.Equal(1, RadiationLateRule.CataractChance(6), 9);

    [Fact]
    public void ALargerDoseCloudsTheLensSooner() => Assert.True(RadiationLateRule.CataractYears(4) < RadiationLateRule.CataractYears(1));

    // ---- Sterility ----

    [Fact]
    public void AMansPermanentSterilityLiesBetweenThreeAndAHalfAndSixGrays()
    {
        Assert.Equal(3.5, RadiationLateRule.PermanentMaleAt(0), 9);
        Assert.Equal(6, RadiationLateRule.PermanentMaleAt(1), 9);
    }

    [Fact]
    public void AnOlderWomanIsMadeSterileByLess()
    {
        Assert.True(RadiationLateRule.PermanentFemaleAt(0, 45) < RadiationLateRule.PermanentFemaleAt(0, 20));
        Assert.Equal(2.5, RadiationLateRule.PermanentFemaleAt(1, 30), 9);
    }

    [Fact]
    public void TemporarySterilityLastsLongerAfterALargerDose() =>
        Assert.True(RadiationLateRule.TemporarySterileYears(2) > RadiationLateRule.TemporarySterileYears(0.2));

    // ---- Pregnancy ----

    [Fact]
    public void TheWindowsFollowTheCdcTable()
    {
        Assert.Equal(PrenatalWindow.Implantation, RadiationLateRule.Window(1));
        Assert.Equal(PrenatalWindow.Organs, RadiationLateRule.Window(5));
        Assert.Equal(PrenatalWindow.BrainEarly, RadiationLateRule.Window(10));
        Assert.Equal(PrenatalWindow.BrainLate, RadiationLateRule.Window(20));
        Assert.Equal(PrenatalWindow.Late, RadiationLateRule.Window(30));
    }

    [Fact]
    public void ABiotechPregnancyIsThirtyEightWeeks() => Assert.Equal(19, RadiationLateRule.WeeksSinceConception(0.5), 9);

    [Fact]
    public void NothingHappensToAPregnancyBelowATenthOfAGray() => Assert.Equal(0, RadiationLateRule.LossChance(0.09, 0.09), 9);

    [Fact]
    public void EarlyLossIsLikelyAboveHalfAGray() => Assert.True(RadiationLateRule.LossChance(1.5, 0) >= 0.85);

    [Fact]
    public void LaterLossNeedsMoreThanHalfAGray() => Assert.Equal(0, RadiationLateRule.LossChance(0, 0.5), 9);

    [Fact]
    public void IntellectualDisabilityIsFortyPercentAtOneGrayInWeeksEightToFifteen() =>
        Assert.Equal(0.4, RadiationLateRule.BrainInjuryChance(1, 0), 9);

    [Fact]
    public void IntellectualDisabilityIsFifteenPercentAtOneGrayInWeeksSixteenToTwentyFive() =>
        Assert.Equal(0.15, RadiationLateRule.BrainInjuryChance(0, 1), 9);

    [Fact]
    public void NoBrainInjuryBelowTheThreshold() => Assert.Equal(0, RadiationLateRule.BrainInjuryChance(0.29, 0.49), 9);

    [Fact]
    public void NoMalformationAtHalfAGray() => Assert.Equal(0, RadiationLateRule.MalformationChance(0.5), 9);
}
