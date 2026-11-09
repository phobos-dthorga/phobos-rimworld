using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Tests;

public class EscharRuleTests
{
    [Fact]
    public void AReleasedLimbDoesNotGrowItBack() => Assert.False(EscharRule.NewEscharForms(25f, 30f, 0.6f, released: true));

    [Fact]
    public void AnUnreleasedLimbStillForms() => Assert.True(EscharRule.NewEscharForms(25f, 30f, 0.6f, released: false));

    [Fact]
    public void ReleaseDoesNotMakeALightBurnForm() => Assert.False(EscharRule.NewEscharForms(10f, 30f, 0.6f, released: false));

    [Fact]
    public void ALimbBurnedPastSixtyPercentOfItsHealthFormsOne()
    {
        Assert.True(EscharRule.Forms(18f, 30f, 0.6f));
        Assert.True(EscharRule.Forms(25f, 30f, 0.6f));
        Assert.False(EscharRule.Forms(17.9f, 30f, 0.6f));
    }

    [Fact]
    public void NoHealthOrNoFractionFormsNothing()
    {
        Assert.False(EscharRule.Forms(10f, 0f, 0.6f));
        Assert.False(EscharRule.Forms(10f, 30f, 0f));
    }

    [Fact]
    public void IschaemiaStartsWhereBloodFlowFailsAndRisesToTheFullRate()
    {
        Assert.Equal(0f, EscharRule.IschaemicDamage(0.6f, 0.7f, 0.4f, 1f));
        Assert.Equal(0f, EscharRule.IschaemicDamage(0.7f, 0.7f, 0.4f, 1f));
        Assert.Equal(0.2f, EscharRule.IschaemicDamage(0.85f, 0.7f, 0.4f, 1f), 4);
        Assert.Equal(0.4f, EscharRule.IschaemicDamage(1f, 0.7f, 0.4f, 1f), 4);
        Assert.Equal(1.2f, EscharRule.IschaemicDamage(1f, 0.7f, 0.4f, 3f), 4);
    }

    [Fact]
    public void BadFiguresGiveNoDamage()
    {
        Assert.Equal(0f, EscharRule.IschaemicDamage(1f, 1f, 0.4f, 1f));
        Assert.Equal(0f, EscharRule.IschaemicDamage(1f, 0.7f, 0f, 1f));
        Assert.Equal(0f, EscharRule.IschaemicDamage(1f, 0.7f, 0.4f, 0f));
    }
}
