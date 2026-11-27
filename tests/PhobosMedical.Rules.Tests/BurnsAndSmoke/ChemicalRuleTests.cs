using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Tests;

public class ChemicalRuleTests
{
    [Fact]
    public void AcidBurnsDeeperWhileFreshAndLessAsItIsSpent()
    {
        Assert.Equal(2f, ChemicalRule.Deepening(1f, 2f, 1f), 4);
        Assert.Equal(1f, ChemicalRule.Deepening(0.5f, 2f, 1f), 4);
        Assert.Equal(2f, ChemicalRule.Deepening(1.5f, 2f, 1f), 4);
        Assert.Equal(0f, ChemicalRule.Deepening(0f, 2f, 1f));
        Assert.Equal(0f, ChemicalRule.Deepening(1f, 2f, 0f));
    }

    [Fact]
    public void TheAcidOnTheSkinDeepensEveryMarkedBurn() =>
        Assert.Equal(DeepeningTarget.MarkedBurns, ChemicalRule.Target(isAcidOnSkin: true, isInjuryOnPart: false, permanent: false, tended: false));

    [Fact]
    public void AnyOtherConditionDeepensOnlyItselfWhileAnOpenBurn()
    {
        Assert.Equal(DeepeningTarget.Self, ChemicalRule.Target(false, isInjuryOnPart: true, permanent: false, tended: false));
        Assert.Equal(DeepeningTarget.None, ChemicalRule.Target(false, isInjuryOnPart: true, permanent: false, tended: true));
        Assert.Equal(DeepeningTarget.None, ChemicalRule.Target(false, isInjuryOnPart: true, permanent: true, tended: false));
        Assert.Equal(DeepeningTarget.None, ChemicalRule.Target(false, isInjuryOnPart: false, permanent: false, tended: false));
    }

    [Fact]
    public void ABurnDeepeningItselfMayTakeAllThePartHasLeftButNoMore()
    {
        Assert.Equal(2f, ChemicalRule.OwnDeepening(2f, 10f), 4);
        Assert.Equal(1f, ChemicalRule.OwnDeepening(2f, 1f), 4);
        Assert.Equal(2f, ChemicalRule.OwnDeepening(2f, 2f), 4);
        Assert.Equal(0f, ChemicalRule.OwnDeepening(2f, 0f));
        Assert.Equal(0f, ChemicalRule.OwnDeepening(0f, 10f));
    }

    [Fact]
    public void EyesAloneTakeAnEyewashNotAShower()
    {
        Assert.Equal(22.7f, ChemicalRule.RinseLitres(true, 1136f, 22.7f));
        Assert.Equal(1136f, ChemicalRule.RinseLitres(false, 1136f, 22.7f));
    }
}
