using Phobos.Radiation.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests.Radiation;

public class RadiationRuleTests
{
    // ---- Repair ----

    [Fact]
    public void ADoseCountsInFullForFourDays() => Assert.Equal(2, RadiationRule.Remaining(2, 4), 6);

    [Fact]
    public void AboutHalfTheRepairablePartIsGoneInAMonth()
    {
        // The 1962 handbook's example: 200 roentgens early in a month leave about 110 by its end.
        double left = RadiationRule.Remaining(200, 30);
        Assert.InRange(left, 100, 120);
    }

    [Fact]
    public void ATenthIsNeverRepaired() => Assert.InRange(RadiationRule.Remaining(10, 1000), 1.0, 1.0001);

    [Fact]
    public void DosesSpreadOutCountLessThanTheirSum()
    {
        var spread = new[] { (60.0, 1.0), (40.0, 1.0), (20.0, 1.0), (0.0, 1.0) };
        Assert.True(RadiationRule.Effective(spread, 0) < 4);
    }

    // ---- The course ----

    [Fact]
    public void VomitingComesSoonerWithMoreDose() =>
        Assert.True(RadiationRule.HoursToVomit(6) < RadiationRule.HoursToVomit(2));

    [Fact]
    public void TheTableIsFollowed() => Assert.Equal(1.74, RadiationRule.HoursToVomit(4), 2);

    [Fact]
    public void FewVomitAtALowDose() => Assert.True(RadiationRule.VomitShare(1) < 0.25);

    [Fact]
    public void EveryoneVomitsAboveEightGrays() => Assert.Equal(1, RadiationRule.VomitShare(9), 6);

    [Fact]
    public void TheWellPeriodShortensWithDose() => Assert.True(RadiationRule.WellDays(7) < RadiationRule.WellDays(3));

    [Fact]
    public void ThereIsNoWellPeriodAboveNineGrays() => Assert.Equal(0, RadiationRule.WellDays(9), 6);

    [Theory]
    [InlineData(0.5, RadiationSyndrome.None)]
    [InlineData(3, RadiationSyndrome.BloodForming)]
    [InlineData(8, RadiationSyndrome.Gut)]
    [InlineData(25, RadiationSyndrome.Neurovascular)]
    public void TheSyndromeFollowsTheDose(double grays, RadiationSyndrome expected) => Assert.Equal(expected, RadiationRule.Syndrome(grays));

    [Fact]
    public void BelowTheThresholdThereIsNoCourse() => Assert.Equal(RadiationPhase.None, RadiationRule.Phase(0.3, 1, false, 0));

    [Fact]
    public void ThePhasesComeInOrder()
    {
        Assert.Equal(RadiationPhase.Early, RadiationRule.Phase(3, 0.5, true, 0.1));
        Assert.Equal(RadiationPhase.Well, RadiationRule.Phase(3, 5, true, 0.1));
        Assert.Equal(RadiationPhase.Illness, RadiationRule.Phase(3, 30, true, 0.1));
    }

    // ---- Who lives ----

    [Fact]
    public void ATypicalPersonHasThePresetsDose() => Assert.Equal(5, RadiationRule.LethalDose(RadiationPreset.Moderate, 0, 0.5), 6);

    [Fact]
    public void CareRaisesTheLethalDose() =>
        Assert.Equal(7.5, RadiationRule.LethalDose(RadiationPreset.Moderate, 1, 0.5), 6);

    [Fact]
    public void ASensitivePersonDiesFromLess() =>
        Assert.True(RadiationRule.LethalDose(RadiationPreset.Realistic, 0, 0.1) < RadiationRule.LethalDose(RadiationPreset.Realistic, 0, 0.9));

    [Fact]
    public void AboveTheLethalDoseTheIllnessKills()
    {
        double lethal = RadiationRule.LethalDose(RadiationPreset.Realistic, 0, 0.5);
        Assert.Equal(1, RadiationRule.Severity(6, lethal, 30, 0.05), 6);
    }

    [Fact]
    public void BelowItThePersonRecovers()
    {
        double lethal = RadiationRule.LethalDose(RadiationPreset.Realistic, 0, 0.5);
        double atHeight = RadiationRule.Severity(2, lethal, RadiationRule.DaysToHeight(2), 0.05);
        double later = RadiationRule.Severity(2, lethal, RadiationRule.DaysToHeight(2) + 10, 0.05);
        Assert.True(later < atHeight);
    }

    // ---- What a doctor can tell ----

    [Fact]
    public void VomitingWithinHalfAnHourMeansAVeryHighDose() => Assert.Equal((8.0, 30.0), RadiationRule.EstimateFromVomiting(0.3, true, 1));

    [Fact]
    public void NoVomitingAfterHalfADayMeansUnderTwoGrays() => Assert.Equal((0.0, 2.0), RadiationRule.EstimateFromVomiting(0, false, 24));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 1)]
    [InlineData(1.0, 2)]
    [InlineData(2.0, 3)]
    public void TheArmysCategories(double grays, int category) => Assert.Equal(category, RadiationRule.ExposureCategory(grays));

    // ---- Rimatomics and fallout ----

    [Fact]
    public void RimatomicsFatalTurnIsAboutEightGrays() =>
        Assert.InRange(0.9 * RadiationRule.GraysPerRimatomicsSickness, 7.5, 8.5);

    [Fact]
    public void FalloutFallsToATenthAfterAWeek() => Assert.InRange(RadiationRule.FalloutDecay(7), 0.08, 0.11);

    [Fact]
    public void FalloutIsFullOnItsFirstDay() => Assert.Equal(1, RadiationRule.FalloutDecay(0.5), 6);
}
