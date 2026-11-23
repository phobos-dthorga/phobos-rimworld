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

    // ---- A later course only from fresh doses (Medical 0.26.1) ----

    [Fact]
    public void AFirstCourseStartsFromTheEffectiveDose()
    {
        Assert.True(RadiationRule.StartsCourse(0.7, 0, hadCourse: false));
        Assert.False(RadiationRule.StartsCourse(0.69, 0, hadCourse: false));
    }

    [Fact]
    public void OldDamageAloneStartsNoSecondCourse()
    {
        // 3 Gy on day 0; the course ends on day 47 with the effective dose still about 1.2 Gy, and no dose since.
        var doses = new[] { (47.0, 3.0) };
        double effective = RadiationRule.Effective(doses, 0);
        Assert.InRange(effective, 1.0, 1.4);
        double fresh = RadiationRule.Fresh(doses, 0, 0);
        Assert.Equal(0, fresh, 6);
        Assert.False(RadiationRule.StartsCourse(effective, fresh, hadCourse: true));
    }

    [Fact]
    public void AFreshDoseStartsACourseAsBadAsTheWholeDose()
    {
        // As above, then 0.8 Gy two days after the course ended: fresh alone crosses the threshold, and the course's height follows
        // the whole effective dose, old damage included.
        var doses = new[] { (49.0, 3.0), (0.0, 0.8) };
        double effective = RadiationRule.Effective(doses, 0);
        double fresh = RadiationRule.Fresh(doses, 2, 0);
        Assert.Equal(0.8, fresh, 6);
        Assert.True(RadiationRule.StartsCourse(effective, fresh, hadCourse: true));
        Assert.InRange(effective, 1.8, 2.2);
    }

    [Fact]
    public void ALargeDosesSurvivorNeverRestartsWithoutANewDose()
    {
        // 8 Gy: the tenth never repaired keeps the effective dose over 0.7 for life, folded beyond repair after 150 days.
        foreach (double daysAgo in new[] { 60.0, 120.0, 150.0 })
        {
            var doses = new[] { (daysAgo, 8.0) };
            double effective = RadiationRule.Effective(doses, 0);
            Assert.True(effective >= RadiationRule.CourseFrom);
            Assert.False(RadiationRule.StartsCourse(effective, RadiationRule.Fresh(doses, daysAgo - 40, 0), hadCourse: true));
        }
        double folded = RadiationRule.Effective(System.Array.Empty<(double, double)>(), 0.8);
        Assert.False(RadiationRule.StartsCourse(folded, 0, hadCourse: true));
    }

    [Fact]
    public void OnTheDayACourseEndsOnlyTheDoseAfterTheEndIsFresh()
    {
        // 0.5 Gy was already recorded on the end day when the course ended; that day now holds 1.3 Gy, so 0.8 Gy is fresh.
        var doses = new[] { (0.0, 1.3) };
        Assert.Equal(0.8, RadiationRule.Fresh(doses, 0, 0.5), 6);
        Assert.Equal(0, RadiationRule.Fresh(doses, 0, 1.3), 6);
    }

    [Fact]
    public void FreshDosesAreRepairedToo()
    {
        // Small fresh doses spread over two months add up to less than their sum.
        var doses = new[] { (60.0, 0.3), (40.0, 0.3), (20.0, 0.3) };
        double fresh = RadiationRule.Fresh(doses, 70, 0);
        Assert.True(fresh < 0.7);
        Assert.False(RadiationRule.StartsCourse(5, fresh, hadCourse: true));
    }

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
