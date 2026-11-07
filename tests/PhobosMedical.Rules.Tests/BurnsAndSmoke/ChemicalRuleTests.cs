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
    public void EyesAloneTakeAnEyewashNotAShower()
    {
        Assert.Equal(22.7f, ChemicalRule.RinseLitres(true, 1136f, 22.7f));
        Assert.Equal(1136f, ChemicalRule.RinseLitres(false, 1136f, 22.7f));
    }
}
