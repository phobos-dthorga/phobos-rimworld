using Phobos.Dosing.Rules;
using Xunit;

namespace Phobos.Dosing.Tests;

public class FussingRuleTests
{
    [Fact]
    public void BelowExtremePainOrAsleepThereIsNoFussing()
    {
        Assert.Equal(1f, FussingRule.Factor(0.79f, true, 0.8f, 0.9f, 0.75f));
        Assert.Equal(1f, FussingRule.Factor(1f, false, 0.8f, 0.9f, 0.75f));
    }

    [Fact]
    public void FussingWorsensFromTheThresholdToFullPain()
    {
        Assert.Equal(0.9f, FussingRule.Factor(0.8f, true, 0.8f, 0.9f, 0.75f), 4);
        Assert.Equal(0.825f, FussingRule.Factor(0.9f, true, 0.8f, 0.9f, 0.75f), 4);
        Assert.Equal(0.75f, FussingRule.Factor(1f, true, 0.8f, 0.9f, 0.75f), 4);
        Assert.Equal(0.75f, FussingRule.Factor(1.4f, true, 0.8f, 0.9f, 0.75f), 4);
    }
}
