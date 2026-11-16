using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class CrewRuleTests
{
    [Fact]
    public void AnEmptyVehicleHasNobodyToLeave() =>
        Assert.Equal(CrewDanger.None, CrewRule.Danger(crewAboard: false, burning: true, designMoves: true, canMove: false, anyWeaponWorks: false));

    [Fact]
    public void ABurningVehicleIsLeftEvenIfItCanFight() =>
        Assert.Equal(CrewDanger.Burning, CrewRule.Danger(crewAboard: true, burning: true, designMoves: true, canMove: true, anyWeaponWorks: true));

    [Fact]
    public void ADeadHullIsLeft() =>
        Assert.Equal(CrewDanger.KnockedOut, CrewRule.Danger(crewAboard: true, burning: false, designMoves: true, canMove: false, anyWeaponWorks: false));

    [Fact]
    public void AStuckTankThatCanStillShootIsHeld() =>
        Assert.Equal(CrewDanger.None, CrewRule.Danger(crewAboard: true, burning: false, designMoves: true, canMove: false, anyWeaponWorks: true));

    [Fact]
    public void ADisarmedVehicleThatCanDriveIsHeld() =>
        Assert.Equal(CrewDanger.None, CrewRule.Danger(crewAboard: true, burning: false, designMoves: true, canMove: true, anyWeaponWorks: false));

    [Fact]
    public void AGunThatNeverMovesIsNotKnockedOutByStandingStill() =>
        Assert.Equal(CrewDanger.None, CrewRule.Danger(crewAboard: true, burning: false, designMoves: false, canMove: false, anyWeaponWorks: false));
}
