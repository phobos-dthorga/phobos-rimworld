using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class GasAlarmRuleTests
{
    [Fact]
    public void ItSoundsAtTheLevelAndHoldsUntilTheAirClears()
    {
        Assert.Equal(AlarmGases.None, GasAlarmRule.Next(AlarmGases.None, 0.04f, 0f, 0.05f, 0.1f, 0.5f));
        Assert.Equal(AlarmGases.CarbonMonoxide, GasAlarmRule.Next(AlarmGases.None, 0.05f, 0f, 0.05f, 0.1f, 0.5f));
        Assert.Equal(AlarmGases.CarbonMonoxide, GasAlarmRule.Next(AlarmGases.CarbonMonoxide, 0.03f, 0f, 0.05f, 0.1f, 0.5f));
        Assert.Equal(AlarmGases.None, GasAlarmRule.Next(AlarmGases.CarbonMonoxide, 0.02f, 0f, 0.05f, 0.1f, 0.5f));
    }

    [Fact]
    public void BothGasesCanSoundAndANewOneIsANewWarning()
    {
        AlarmGases both = GasAlarmRule.Next(AlarmGases.Smoke, 0.06f, 0.2f, 0.05f, 0.1f, 0.5f);
        Assert.Equal(AlarmGases.CarbonMonoxide | AlarmGases.Smoke, both);
        Assert.True(GasAlarmRule.NewWarning(AlarmGases.Smoke, both));
        Assert.False(GasAlarmRule.NewWarning(both, both));
        Assert.False(GasAlarmRule.NewWarning(both, AlarmGases.Smoke));
    }

    [Fact]
    public void ALevelOfNothingNeverSounds() => Assert.Equal(AlarmGases.None, GasAlarmRule.Next(AlarmGases.None, 1f, 1f, 0f, 0f, 0.5f));
}
