using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Rules.Tests;

/// <summary>The strike's flash. The cover and its share of the dose moved to D'thorga Framework's RadiationShelterRule (Patches 0.47.0), tested there.</summary>
public class StrikeShelterRuleTests
{
    [Fact]
    public void FlashBlindnessIsWorseAndLongerAtNight()
    {
        Assert.True(StrikeShelterRule.FlashChanceFactor(0f) > StrikeShelterRule.FlashChanceFactor(1f));
        Assert.True(StrikeShelterRule.FlashSeverity(0f) > 10 * StrikeShelterRule.FlashSeverity(1f));
    }
}
