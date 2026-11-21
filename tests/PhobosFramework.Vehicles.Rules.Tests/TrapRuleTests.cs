using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class TrapRuleTests
{
    [Fact]
    public void AnAircraftInTheAirOverfliesEveryTrapWhateverTheSetting()
    {
        Assert.Equal(TrapRule.Outcome.Overflown, TrapRule.For(inAir: true, players: true, settingOn: false, onPlayersOrder: true));
        Assert.Equal(TrapRule.Outcome.Overflown, TrapRule.For(inAir: true, players: false, settingOn: true, onPlayersOrder: false));
    }

    [Fact]
    public void ThePlayersVehicleOffAnOrderPassesOverTraps()
    {
        Assert.Equal(TrapRule.Outcome.PassedOver, TrapRule.For(inAir: false, players: true, settingOn: true, onPlayersOrder: false));
    }

    [Fact]
    public void ADriveThePlayerOrderedMeetsTrapsAsTheGameDecides()
    {
        Assert.Equal(TrapRule.Outcome.AsTheGame, TrapRule.For(inAir: false, players: true, settingOn: true, onPlayersOrder: true));
    }

    [Fact]
    public void WithTheSettingOffEveryDriveIsAsTheGameDecides()
    {
        Assert.Equal(TrapRule.Outcome.AsTheGame, TrapRule.For(inAir: false, players: true, settingOn: false, onPlayersOrder: false));
    }

    [Fact]
    public void AnotherFactionsVehicleStillSetsOffTraps()
    {
        Assert.Equal(TrapRule.Outcome.AsTheGame, TrapRule.For(inAir: false, players: false, settingOn: true, onPlayersOrder: false));
    }
}
