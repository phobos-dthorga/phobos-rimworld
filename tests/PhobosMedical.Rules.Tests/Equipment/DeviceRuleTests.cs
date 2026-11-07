using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class FieldOxygenTests
{
    [Fact]
    public void APatientWhoNeedsOxygenGetsTheKit() => Assert.Equal(DeviceRefusal.None, DeviceRule.FieldOxygen(true, false, 0.6f));

    [Fact]
    public void APatientAlreadyOnOxygenIsLeftAlone() =>
        Assert.Equal(DeviceRefusal.AlreadyOnOxygen, DeviceRule.FieldOxygen(true, true, 0.6f));

    [Fact]
    public void APatientBreathingWellIsLeftAlone() => Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.FieldOxygen(false, false, 1f));

    [Fact]
    public void AShutAirwayComesFirstBecauseOxygenCannotReachTheLungs() =>
        Assert.Equal(DeviceRefusal.AirwayShut, DeviceRule.FieldOxygen(true, true, 0f));
}

public class BreathingAssistTests
{
    [Fact]
    public void WeakBreathingIsHelped() => Assert.Equal(DeviceRefusal.None, DeviceRule.BreathingAssist(0.3f, 0.5f));

    [Fact]
    public void BreathingAtTheThresholdIsLeftAlone() => Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.BreathingAssist(0.5f, 0.5f));

    [Fact]
    public void AShutAirwayCannotBeBagged() => Assert.Equal(DeviceRefusal.AirwayShut, DeviceRule.BreathingAssist(0f, 0.5f));
}

public class BoilOffTests
{
    [Fact]
    public void NothingBoilsAwayWhilePowered() => Assert.Equal(0f, BoilOffRule.LossForInterval(1000f, 0.02f, 250, true));

    [Fact]
    public void UnpoweredItLosesItsDailyShareSpreadOverTheDay() =>
        Assert.Equal(1000f * 0.02f * 250 / 60000, BoilOffRule.LossForInterval(1000f, 0.02f, 250, false), 5);

    [Fact]
    public void AFullDayUnpoweredLosesTheWholeShare()
    {
        float stored = 1000f;
        for (int i = 0; i < BoilOffRule.TicksPerDay / 250; i++) stored -= BoilOffRule.LossForInterval(1000f, 0.02f, 250, false);
        Assert.Equal(980f, stored, 1); // Float steps add up to a few thousandths over 240 of them.
    }

    [Fact]
    public void ItNeverLosesMoreThanItHolds() => Assert.Equal(1f, BoilOffRule.LossForInterval(1f, 100f, 60000, false));

    [Fact]
    public void AnEmptyStoreLosesNothing() => Assert.Equal(0f, BoilOffRule.LossForInterval(0f, 0.02f, 250, false));
}

public class RefusalOrderTests
{
    [Fact]
    public void AShutAirwayIsTheReasonToTellAboveAll() =>
        Assert.Equal(DeviceRefusal.AirwayShut, DeviceRule.MostUrgent(DeviceRefusal.NoDevice, DeviceRefusal.AirwayShut));

    [Fact]
    public void AMissingDeviceIsToldOverAnotherDeviceNotBeingNeeded() =>
        Assert.Equal(DeviceRefusal.NoDevice, DeviceRule.MostUrgent(DeviceRefusal.NotNeeded, DeviceRefusal.NoDevice));

    [Fact]
    public void AlreadyOnOxygenIsToldOverNotNeeded() =>
        Assert.Equal(DeviceRefusal.AlreadyOnOxygen, DeviceRule.MostUrgent(DeviceRefusal.AlreadyOnOxygen, DeviceRefusal.NotNeeded));
}

public class TimesFasterTests
{
    [Fact]
    public void OxygenAtSeventyFiveMinutesIsAboutFourTimesRoomAir() => Assert.Equal(4.3f, OxygenRule.TimesFaster(5.33f, 1.25f), 1);

    [Fact]
    public void AnUnsetHalfLifeGivesNothing() => Assert.Equal(0f, OxygenRule.TimesFaster(5.33f, 0f));
}

public class WorthTellingTests
{
    [Fact]
    public void BreathingWellEnoughIsNotWorthAMenuLine() => Assert.False(DeviceRule.WorthTelling(DeviceRefusal.NotNeeded));

    [Fact]
    public void AShutAirwayAMissingDeviceAndOxygenAlreadyGivenAreTold()
    {
        Assert.True(DeviceRule.WorthTelling(DeviceRefusal.AirwayShut));
        Assert.True(DeviceRule.WorthTelling(DeviceRefusal.NoDevice));
        Assert.True(DeviceRule.WorthTelling(DeviceRefusal.AlreadyOnOxygen));
    }
}
