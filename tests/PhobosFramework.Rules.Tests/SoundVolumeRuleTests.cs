using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SoundVolumeRuleTests
{
    [Theory]
    [InlineData(13f, 1f, 13f)]
    [InlineData(13f, 0f, 0f)]
    [InlineData(50f, 0.5f, 25f)]
    [InlineData(20f, 1.5f, 30f)]
    [InlineData(20f, 2f, 40f)]
    public void TheSettingScalesTheAuthoredVolume(float authored, float factor, float expected) =>
        Assert.Equal(expected, SoundVolumeRule.Scale(authored, factor), 3);

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(5f, SoundVolumeRule.MaxFactor)]
    [InlineData(float.NaN, 1f)]
    public void TheSettingStaysInRange(float factor, float expected) => Assert.Equal(expected, SoundVolumeRule.Clamp(factor), 3);

    [Fact]
    public void TheLabelIsAWholePercentage()
    {
        Assert.Equal(100, SoundVolumeRule.Percent(1f));
        Assert.Equal(75, SoundVolumeRule.Percent(0.7499f));
        Assert.Equal(200, SoundVolumeRule.Percent(9f));
    }
}
