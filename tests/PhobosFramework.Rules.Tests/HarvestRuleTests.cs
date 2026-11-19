using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class HarvestRuleTests
{
    [Fact]
    public void AClumsyDriverCanFailAHarvest() => Assert.True(HarvestRule.Fails(humanlike: true, cropCanFail: true, blighted: false, 0.6f, roll: 0.7f));

    [Fact]
    public void ARollUnderTheStatSucceeds() => Assert.False(HarvestRule.Fails(true, true, false, 0.6f, roll: 0.5f));

    [Fact]
    public void AStatOfOneOrMoreNeverFails()
    {
        Assert.False(HarvestRule.Fails(true, true, false, 1f, roll: 0.999f));
        Assert.False(HarvestRule.Fails(true, true, false, 1.2f, roll: 1f));
    }

    [Fact]
    public void OnlyAHumanlikeHarvesterFails() => Assert.False(HarvestRule.Fails(humanlike: false, true, false, 0.1f, 0.9f));

    [Fact]
    public void ACropThatCannotFailNeverFails() => Assert.False(HarvestRule.Fails(true, cropCanFail: false, false, 0.1f, 0.9f));

    [Fact]
    public void ABlightedPlantNeverFails() => Assert.False(HarvestRule.Fails(true, true, blighted: true, 0.1f, 0.9f));

    [Fact]
    public void AStatOfOneOrLessGivesThePlantsOwnYield()
    {
        Assert.Equal(11, HarvestRule.Yield(11, 1f, 0f));
        Assert.Equal(11, HarvestRule.Yield(11, 0.6f, 0f));
    }

    [Fact]
    public void AStatAboveOneMultipliesAndTheRollSettlesTheFraction()
    {
        // Eleven times 1.15 is 12.65: thirteen when the roll is under 0.65, twelve otherwise.
        Assert.Equal(13, HarvestRule.Yield(11, 1.15f, 0.5f));
        Assert.Equal(12, HarvestRule.Yield(11, 1.15f, 0.7f));
    }

    [Fact]
    public void AWholeProductNeverGainsOneFromTheRoll() => Assert.Equal(20, HarvestRule.Yield(10, 2f, 0f));

    [Fact]
    public void APlantThatYieldsNothingGivesNothing() => Assert.Equal(0, HarvestRule.Yield(0, 1.5f, 0f));

    [Fact]
    public void TheSecondProductHasItsOwnRoll()
    {
        Assert.Equal(0, HarvestRule.SecondProduct(false, 6, 1f, yieldStat: 0.6f, chanceRoll: 0.6f, 0f));
        Assert.Equal(6, HarvestRule.SecondProduct(false, 6, 1f, yieldStat: 0.6f, chanceRoll: 0.59f, 0f));
    }

    [Fact]
    public void ABlightedPlantGivesNoSecondProduct() => Assert.Equal(0, HarvestRule.SecondProduct(blighted: true, 6, 1f, 1f, 0f, 0f));

    [Fact]
    public void TheSecondProductFollowsGrowthCutToAWholeNumberFirst()
    {
        // Six at 0.9 grown is 5.4, cut to five before the stat is applied.
        Assert.Equal(5, HarvestRule.SecondProduct(false, 6, 0.9f, 1f, 0f, 0f));
        // Five times 1.3 is 6.5: seven under a roll of a half, six over.
        Assert.Equal(7, HarvestRule.SecondProduct(false, 6, 0.9f, 1.3f, 0f, 0.4f));
        Assert.Equal(6, HarvestRule.SecondProduct(false, 6, 0.9f, 1.3f, 0f, 0.6f));
    }

    [Fact]
    public void AnAmountTooSmallToGrowAWholeUnitGivesNothing() => Assert.Equal(0, HarvestRule.SecondProduct(false, 1, 0.5f, 1.5f, 0f, 0f));

    [Fact]
    public void ADriverLearnsAShareOfWhatAHandWould()
    {
        Assert.Equal(22f, HarvestRule.DriverExperience(0.11f, 800f, 0.25f), 3);
        Assert.Equal(0f, HarvestRule.DriverExperience(0.11f, 800f, 0f));
        Assert.Equal(88f, HarvestRule.DriverExperience(0.11f, 800f, 4f), 3);
    }
}
