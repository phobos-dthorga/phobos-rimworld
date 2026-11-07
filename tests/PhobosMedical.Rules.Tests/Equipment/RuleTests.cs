using System.Linq;
using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class BurnCareStatusTests
{
    [Fact]
    public void SwitchedOffComesFirst() => Assert.Equal(BurnCareStatus.SwitchedOff, BurnCareRule.Status(false, false, 1));

    [Fact]
    public void NoPowerComesNext() => Assert.Equal(BurnCareStatus.NoPower, BurnCareRule.Status(true, false, 1));

    [Fact]
    public void AnEmptyBedWaits() => Assert.Equal(BurnCareStatus.Waiting, BurnCareRule.Status(true, true, 0));

    [Fact]
    public void AnOccupiedBedTreats() => Assert.Equal(BurnCareStatus.Treating, BurnCareRule.Status(true, true, 1));

    [Fact]
    public void AnOccupantWithoutBurnsIsNamedNotIgnored() => Assert.Equal(BurnCareStatus.NotNeeded, BurnCareRule.Status(true, true, 0, 1));

    [Fact]
    public void TreatingOutranksNotNeeded() => Assert.Equal(BurnCareStatus.Treating, BurnCareRule.Status(true, true, 1, 1));
}

public class NeedTests
{
    const float Threshold = 0.8f;

    [Fact]
    public void AHealthyPatientNeedsNothing() => Assert.False(OxygenRule.NeedsOxygen(1f, false, Threshold));

    [Fact]
    public void LowBreathingFromAnyCauseQualifies() => Assert.True(OxygenRule.NeedsOxygen(0.5f, false, Threshold));

    [Fact]
    public void BreathingAtTheThresholdDoesNot() => Assert.False(OxygenRule.NeedsOxygen(Threshold, false, Threshold));

    [Fact]
    public void ACarbonMonoxidePatientQualifiesEvenBreathingNormally() => Assert.True(OxygenRule.NeedsOxygen(1f, true, Threshold));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AClosedAirwayCannotBeHelped(bool responsive) => Assert.False(OxygenRule.NeedsOxygen(0f, responsive, Threshold));
}

public class FuelTests
{
    [Fact]
    public void OneRareTickIsATenthOfAnHour() => Assert.Equal(0.1f, OxygenRule.FuelForInterval(1, 1f, 250), 4);

    [Fact]
    public void TwoPatientsUseTwiceAsMuch() => Assert.Equal(0.2f, OxygenRule.FuelForInterval(2, 1f, 250), 4);

    [Theory]
    [InlineData(0, 1f)]
    [InlineData(2, 0f)]
    public void NoPatientsOrAPowerOnlyMachineBurnNothing(int patients, float rate) =>
        Assert.Equal(0f, OxygenRule.FuelForInterval(patients, rate, 250));
}

public class CeilingTests
{
    [Theory]
    [InlineData(0.5f, true)]
    [InlineData(0.99f, true)]
    [InlineData(1f, false)]
    [InlineData(1.25f, false)]
    public void OxygenRaisesOnlyBreathingBelowNormal(float without, bool raises) =>
        Assert.Equal(raises, OxygenRule.RaisesBreathing(without));
}

public class StatusTests
{
    [Fact]
    public void TreatingNeedsEverythingInPlace() =>
        Assert.Equal(OxygenStatus.Treating, OxygenRule.Status(true, true, 1, true, 1));

    [Fact]
    public void WithNobodyToTreatItWaits() =>
        Assert.Equal(OxygenStatus.Waiting, OxygenRule.Status(true, true, 1, true, 0));

    [Fact]
    public void APatientBreathingWellIsNamedNotIgnored() =>
        Assert.Equal(OxygenStatus.NotNeeded, OxygenRule.Status(true, true, 1, true, 0, 1));

    [Fact]
    public void TreatingOutranksNotNeeded() =>
        Assert.Equal(OxygenStatus.Treating, OxygenRule.Status(true, true, 1, true, 1, 1));

    [Fact]
    public void AMissingBedOutranksNotNeeded() =>
        Assert.Equal(OxygenStatus.NoBed, OxygenRule.Status(true, true, 0, true, 0, 1));

    [Fact]
    public void TheSwitchIsReportedBeforeEverythingElse() =>
        Assert.Equal(OxygenStatus.SwitchedOff, OxygenRule.Status(false, false, 0, false, 0));

    [Fact]
    public void PowerComesBeforeTheBed() =>
        Assert.Equal(OxygenStatus.NoPower, OxygenRule.Status(true, false, 0, false, 0));

    [Fact]
    public void TheBedComesBeforeTheOxygen() =>
        Assert.Equal(OxygenStatus.NoBed, OxygenRule.Status(true, true, 0, false, 0));

    [Fact]
    public void AnEmptyStandSaysSo() =>
        Assert.Equal(OxygenStatus.NoOxygen, OxygenRule.Status(true, true, 1, false, 0));
}

public class ChoiceTests
{
    [Fact]
    public void TheWorstBreathingComesFirstUpToTheLimit()
    {
        float[] patients = { 0.7f, 0.2f, 0.5f };
        Assert.Equal(new[] { 0.2f, 0.5f }, OxygenRule.WorstFirst(patients, p => p, 2).ToArray());
    }

    [Fact]
    public void TiesKeepTheirOrder()
    {
        (string Name, float Level)[] patients = { ("a", 0.5f), ("b", 0.5f) };
        Assert.Equal("a", OxygenRule.WorstFirst(patients, p => p.Level, 1)[0].Name);
    }

    [Fact]
    public void ALimitOfZeroServesNobody() => Assert.Empty(OxygenRule.WorstFirst(new[] { 0.1f }, p => p, 0));
}
