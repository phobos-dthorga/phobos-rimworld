using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>Standby power (Framework 0.222.0): listed power in use, a share idle, nothing for a light that is off.</summary>
public class StandbyPowerRuleTests
{
    [Fact]
    public void InUseDrawsTheListedPower() => Assert.Equal(250f, StandbyPowerRule.Draw(250f, inUse: true, StandbyKind.Machine, 10));

    [Fact]
    public void IdleDrawsTheStandbyShare() => Assert.Equal(25f, StandbyPowerRule.Draw(250f, inUse: false, StandbyKind.Machine, 10), 3);

    [Fact]
    public void ALightThatIsOffDrawsNothing() => Assert.Equal(0f, StandbyPowerRule.Draw(75f, inUse: false, StandbyKind.Light, 10));

    [Fact]
    public void ALightInUseDrawsItsListedPower() => Assert.Equal(75f, StandbyPowerRule.Draw(75f, inUse: true, StandbyKind.Light, 10));

    [Fact]
    public void ZeroPercentStandbyDrawsNothing() => Assert.Equal(0f, StandbyPowerRule.Draw(400f, inUse: false, StandbyKind.Turret, 0));

    [Fact]
    public void TheStandbyShareIsCapped() => Assert.Equal(StandbyPowerRule.MaxStandbyPercent, StandbyPowerRule.ClampPercent(90));

    [Fact]
    public void ANegativeShareCountsAsNone() => Assert.Equal(0f, StandbyPowerRule.Draw(100f, inUse: false, StandbyKind.Door, -5));

    [Fact]
    public void ANegativeListedPowerDrawsNothing() => Assert.Equal(0f, StandbyPowerRule.Draw(-100f, inUse: true, StandbyKind.Machine, 10));

    [Fact]
    public void EveryOfferedShareIsWithinTheRule()
    {
        foreach (int percent in StandbyPowerRule.StandbyPercents) Assert.Equal(percent, StandbyPowerRule.ClampPercent(percent));
        Assert.Contains(StandbyPowerRule.DefaultStandbyPercent, StandbyPowerRule.StandbyPercents);
    }

    [Fact]
    public void OurOwnDrawIsOurs() => Assert.True(StandbyPowerRule.DrawIsOursOrTheGames(25f, 25f, 250f));

    [Fact]
    public void TheGamesResetToListedIsAccepted() => Assert.True(StandbyPowerRule.DrawIsOursOrTheGames(250f, 25f, 250f));

    [Fact]
    public void NoDrawIsAccepted() => Assert.True(StandbyPowerRule.DrawIsOursOrTheGames(0f, 25f, 250f));

    [Fact]
    public void AnotherModsDrawIsLeftAlone() => Assert.False(StandbyPowerRule.DrawIsOursOrTheGames(612f, 25f, 250f));

    [Fact]
    public void OutdoorLightsStayLit() => Assert.True(StandbyPowerRule.LightWanted(kindOn: true, indoors: false, someoneAwakeInRoom: false));

    [Fact]
    public void AnEmptyRoomIsDark() => Assert.False(StandbyPowerRule.LightWanted(kindOn: true, indoors: true, someoneAwakeInRoom: false));

    [Fact]
    public void SomeoneAwakeLightsTheRoom() => Assert.True(StandbyPowerRule.LightWanted(kindOn: true, indoors: true, someoneAwakeInRoom: true));

    [Fact]
    public void LightsAreLitWhenTheKindIsOff() => Assert.True(StandbyPowerRule.LightWanted(kindOn: false, indoors: true, someoneAwakeInRoom: false));

    [Fact]
    public void AMapLooksOnceASecond()
    {
        int looks = 0;
        for (int tick = 0; tick < StandbyPowerRule.SweepTicks * 10; tick++)
        {
            if (StandbyPowerRule.SweepDue(tick, mapIndex: 2)) looks++;
        }
        Assert.Equal(10, looks);
    }

    [Fact]
    public void TwoMapsLookOnDifferentTicks()
    {
        for (int tick = 0; tick < StandbyPowerRule.SweepTicks; tick++)
        {
            Assert.False(StandbyPowerRule.SweepDue(tick, 0) && StandbyPowerRule.SweepDue(tick, 1));
        }
    }
}
