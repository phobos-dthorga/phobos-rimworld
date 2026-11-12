using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class UnderlayRuleTests
{
    [Fact]
    public void AParkedVehicleWithItsEngineOffLaysItsPieces() =>
        Assert.True(UnderlayRule.Lays(spawned: true, players: true, moving: false, engineOn: false, setUp: false));

    [Fact]
    public void StartingTheEngineTakesThemUp() =>
        Assert.False(UnderlayRule.Lays(spawned: true, players: true, moving: false, engineOn: true, setUp: false));

    [Fact]
    public void AMovingVehicleLaysNothing() =>
        Assert.False(UnderlayRule.Lays(spawned: true, players: true, moving: true, engineOn: false, setUp: false));

    [Fact]
    public void ASetUpVehicleKeepsItsPiecesWithItsEngineRunning() =>
        Assert.True(UnderlayRule.Lays(spawned: true, players: true, moving: false, engineOn: true, setUp: true));

    [Fact]
    public void AnotherFactionsVehicleLaysNothing() =>
        Assert.False(UnderlayRule.Lays(spawned: true, players: false, moving: false, engineOn: false, setUp: false));

    [Fact]
    public void AVehicleOffTheMapLaysNothing() =>
        Assert.False(UnderlayRule.Lays(spawned: false, players: true, moving: false, engineOn: false, setUp: false));

    [Fact]
    public void AnOldHiddenPieceOnAFreeCellIsAdoptedOnce()
    {
        Assert.True(UnderlayRule.Adopts(adoptedBefore: false, oldHiddenKind: true, cellAlreadyHeld: false));
        Assert.False(UnderlayRule.Adopts(adoptedBefore: true, oldHiddenKind: true, cellAlreadyHeld: false));
    }

    [Fact]
    public void ThePlayersOrdinaryPipeIsNeverAdopted() =>
        Assert.False(UnderlayRule.Adopts(adoptedBefore: false, oldHiddenKind: false, cellAlreadyHeld: false));

    [Fact]
    public void ACellAlreadyHeldIsNotAdoptedTwice() =>
        Assert.False(UnderlayRule.Adopts(adoptedBefore: false, oldHiddenKind: true, cellAlreadyHeld: true));
}
