using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class StoryRulesTests
{
    [Fact]
    public void ATileIsThirtyKilometres() => Assert.Equal(30, WorldScaleRule.Kilometres(1));

    [Fact]
    public void PartTilesScale() => Assert.Equal(75, WorldScaleRule.Kilometres(2.5), 6);

    [Fact]
    public void NoDistanceIsNeverNegative() => Assert.Equal(0, WorldScaleRule.Kilometres(-3));

    [Fact]
    public void ALogbookUnderItsCapDropsNothing() => Assert.Equal(0, LogbookRule.DropCount(150, 200));

    [Fact]
    public void ALogbookOverItsCapDropsTheExcess() => Assert.Equal(3, LogbookRule.DropCount(203, 200));

    [Fact]
    public void ANegativeCapKeepsEverything() => Assert.Equal(0, LogbookRule.DropCount(500, -1));

    [Fact]
    public void AQuietTripIsNoLetter() => Assert.False(TripRule.WentWrong(0, halted: false, someoneHurt: false));

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, true)]
    public void ATroubledTripIsALetter(int faults, bool halted, bool hurt) => Assert.True(TripRule.WentWrong(faults, halted, hurt));
}
