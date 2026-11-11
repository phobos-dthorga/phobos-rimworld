using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>A picture that builds while wanted and fades when not, each at its own rate, within 0 and 1.</summary>
public class FadeRuleTests
{
    const float Hour = 2500f;

    [Fact]
    public void BuildsAtItsOwnRate()
    {
        Assert.Equal(0.25f, FadeRule.Step(0f, true, 625f, Hour, 3 * Hour), 4);
        Assert.Equal(1f, FadeRule.Step(0.9f, true, Hour, Hour, 3 * Hour));
    }

    [Fact]
    public void FadesMoreSlowlyThanItBuilds()
    {
        float built = FadeRule.Step(0f, true, 250f, Hour, 3 * Hour);
        float melted = 1f - FadeRule.Step(1f, false, 250f, Hour, 3 * Hour);
        Assert.True(melted < built);
        Assert.Equal(0f, FadeRule.Step(0.01f, false, Hour, Hour, 3 * Hour));
    }

    [Fact]
    public void AZeroTimeJumpsStraightThere()
    {
        Assert.Equal(1f, FadeRule.Step(0.2f, true, 1f, 0f, Hour));
        Assert.Equal(0f, FadeRule.Step(0.8f, false, 1f, Hour, 0f));
    }
}
