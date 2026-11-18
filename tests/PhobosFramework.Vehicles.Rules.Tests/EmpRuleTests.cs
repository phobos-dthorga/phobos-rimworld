using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class EmpRuleTests
{
    [Fact]
    public void AnArmedVehicleIsMilitaryAndAnUnarmedOneCivilian()
    {
        Assert.Equal(EmpHardening.Military, EmpRule.ClassFor(EmpHardening.ByClass, armed: true));
        Assert.Equal(EmpHardening.Civilian, EmpRule.ClassFor(EmpHardening.ByClass, armed: false));
    }

    [Fact]
    public void TheLoadoutsChoiceWins()
    {
        Assert.Equal(EmpHardening.Mechanical, EmpRule.ClassFor(EmpHardening.Mechanical, armed: true));
        Assert.Equal(EmpHardening.Military, EmpRule.ClassFor(EmpHardening.Military, armed: false));
    }

    [Fact]
    public void MilitaryPartsTakeTheMilitaryRatingAndTheRestTheCivilian()
    {
        Assert.Equal(2, EmpRule.Severity(EmpHardening.Military, 0, civilianRating: 3, militaryRating: 2));
        Assert.Equal(3, EmpRule.Severity(EmpHardening.Civilian, 0, 3, 2));
        Assert.Equal(3, EmpRule.Severity(EmpHardening.Mechanical, 0, 3, 2));
    }

    [Fact]
    public void AnAuthorsOwnRatingStays()
    {
        Assert.Equal(4, EmpRule.Severity(EmpHardening.Military, 4, 3, 2));
    }

    [Fact]
    public void RatingsStayWithinVehicleFrameworksScale()
    {
        Assert.Equal(4, EmpRule.Severity(EmpHardening.Civilian, 0, 9, 2));
        Assert.Equal(0, EmpRule.Severity(EmpHardening.Military, 0, 3, -1));
    }

    [Fact]
    public void AHardenedPartTakesTheMilitaryRating()
    {
        Assert.Equal(1, EmpRule.HardenedSeverity(partRating: 2, militaryRating: 1));
        Assert.Equal(1, EmpRule.HardenedSeverity(4, 1));
    }

    [Fact]
    public void HardeningNeverMakesAPartWorse()
    {
        Assert.Equal(1, EmpRule.HardenedSeverity(1, militaryRating: 3));
        Assert.Equal(0, EmpRule.HardenedSeverity(0, 1));
        Assert.Equal(0, EmpRule.HardenedSeverity(2, -1));
    }

    [Fact]
    public void MechanicalVehiclesAndOptedOutOnesAreNeverStunned()
    {
        Assert.False(EmpRule.Stuns(EmpHardening.Mechanical, authorSaysNoStun: false));
        Assert.False(EmpRule.Stuns(EmpHardening.Military, authorSaysNoStun: true));
        Assert.True(EmpRule.Stuns(EmpHardening.Civilian, authorSaysNoStun: false));
    }
}
