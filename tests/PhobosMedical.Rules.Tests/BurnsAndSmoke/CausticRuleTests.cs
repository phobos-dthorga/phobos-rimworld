using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Tests;

public class CausticRuleTests
{
    [Theory]
    [InlineData(4f, 4f, true)]
    [InlineData(30f, 4f, true)]
    [InlineData(3.9f, 4f, false)]
    [InlineData(0f, 4f, false)]
    [InlineData(30f, 0f, false)]
    public void OnlyGasMetAfterTimeInClearAirIsFresh(float hoursClear, float gapHours, bool fresh) =>
        Assert.Equal(fresh, CausticRule.FreshExposure(hoursClear, gapHours));

    [Fact]
    public void DoseGrowsWithExposureAndTimeAndStopsAtOne()
    {
        Assert.Equal(0.2f, CausticRule.AddDose(0f, 1f, 0.6f, 1f / 3f), 3);
        Assert.Equal(0.1f, CausticRule.AddDose(0f, 0.5f, 0.6f, 1f / 3f), 3);
        Assert.Equal(1f, CausticRule.AddDose(0.9f, 1f, 0.6f, 1f));
        Assert.Equal(0.3f, CausticRule.AddDose(0.3f, 0f, 0.6f, 1f));
    }

    [Fact]
    public void DoseFadesButNeverBelowNothing()
    {
        Assert.Equal(0.25f, CausticRule.Fade(0.5f, 0.5f, 0.5f), 4);
        Assert.Equal(0f, CausticRule.Fade(0.1f, 0.5f, 1f));
    }

    [Fact]
    public void ASealedSuitStopsTheSkinAndClothesOnlyThinIt()
    {
        Assert.Equal(0f, CausticRule.SkinFactor(true, 0f, 0.25f));
        Assert.Equal(1f, CausticRule.SkinFactor(false, 0f, 0.25f));
        Assert.Equal(0.25f, CausticRule.SkinFactor(false, 1f, 0.25f), 4);
        Assert.Equal(0.625f, CausticRule.SkinFactor(false, 0.5f, 0.25f), 4);
    }

    [Fact]
    public void TheRampStartsAtTheOnsetAndPeaks()
    {
        Assert.Equal(0f, CausticRule.Ramp(3f, 4f, 12f));
        Assert.Equal(0.5f, CausticRule.Ramp(8f, 4f, 12f), 4);
        Assert.Equal(1f, CausticRule.Ramp(20f, 4f, 12f));
        Assert.Equal(1f, CausticRule.Ramp(0f, 0f, 0f));
    }

    [Fact]
    public void FastLungsComeSoonerAfterABiggerDose()
    {
        Assert.Equal(36f, CausticRule.FastLungOnsetHours(0f, 36f, 4f), 3);
        Assert.Equal(20f, CausticRule.FastLungOnsetHours(0.5f, 36f, 4f), 3);
        Assert.Equal(4f, CausticRule.FastLungOnsetHours(1f, 36f, 4f), 3);
    }

    [Fact]
    public void OnlyAHeavyDoseWeakensImmunity()
    {
        Assert.Equal(0f, CausticRule.Weakness(0.4f, 0.5f, 0.8f, 0.3f, 0.6f));
        Assert.Equal(0.3f, CausticRule.Weakness(0.5f, 0.5f, 0.8f, 0.3f, 0.6f));
        Assert.Equal(0.6f, CausticRule.Weakness(0.9f, 0.5f, 0.8f, 0.3f, 0.6f));
    }
}
