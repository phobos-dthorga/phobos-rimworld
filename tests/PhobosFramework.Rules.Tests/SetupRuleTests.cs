using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>The setup wizard (Framework 0.362.0): pages due by version, the preset in use, the settings outside every preset.</summary>
public class SetupRuleTests
{
    static readonly Dictionary<string, int> Seen = new() { ["Radio"] = 1, ["Farming"] = 2 };

    [Fact]
    public void APageNeverSeenIsDue() => Assert.True(SetupRule.Due(Seen, "Medical", 1));

    [Fact]
    public void APageSeenAtItsVersionIsNotDue() => Assert.False(SetupRule.Due(Seen, "Radio", 1));

    [Fact]
    public void APageWithANewerVersionIsDueAgain() => Assert.True(SetupRule.Due(Seen, "Radio", 2));

    [Fact]
    public void APageSeenAtANewerVersionIsNotDue() => Assert.False(SetupRule.Due(Seen, "Farming", 1));

    [Fact]
    public void DuePagesKeepTheirOrder() =>
        Assert.Equal(new[] { "Medical", "Radio" }, SetupRule.DuePages(Seen, new[] { ("Medical", 1), ("Farming", 2), ("Radio", 3) }));

    static readonly List<IReadOnlyList<(string, string)>> Presets = new()
    {
        new[] { ("drivetrain", "DisplayOnly"), ("parts", "false") },
        new[] { ("drivetrain", "Full"), ("parts", "true") },
        new[] { ("drivetrain", "Full"), ("parts", "true"), ("boilers", "true") },
    };

    [Fact]
    public void TheFirstPresetWhoseValuesAllHoldMatches()
    {
        var now = new Dictionary<string, string> { ["drivetrain"] = "Full", ["parts"] = "true", ["boilers"] = "false" };
        Assert.Equal(1, SetupRule.MatchingPreset(Presets, s => now.TryGetValue(s, out string? v) ? v : null));
    }

    [Fact]
    public void AMixOfPresetsMatchesNone()
    {
        var now = new Dictionary<string, string> { ["drivetrain"] = "DisplayOnly", ["parts"] = "true", ["boilers"] = "false" };
        Assert.Equal(-1, SetupRule.MatchingPreset(Presets, s => now.TryGetValue(s, out string? v) ? v : null));
    }

    [Fact]
    public void ASettingThatIsNotThereIsPassedOver()
    {
        var now = new Dictionary<string, string> { ["drivetrain"] = "Full", ["parts"] = "true" };
        Assert.Equal(1, SetupRule.MatchingPreset(Presets, s => now.TryGetValue(s, out string? v) ? v : null));
    }

    [Fact]
    public void APresetWithNothingReadableMatchesNothing() =>
        Assert.Equal(-1, SetupRule.MatchingPreset(Presets, _ => null));

    [Fact]
    public void ValuesCompareWithoutCase() =>
        Assert.Equal(0, SetupRule.MatchingPreset(Presets, s => s == "drivetrain" ? "displayonly" : "False"));

    [Fact]
    public void SettingsNoPresetSetsAreListed() =>
        Assert.Equal(new[] { "scope", "display" }, SetupRule.OutsidePresets(new[] { "scope", "drivetrain", "display", "boilers" }, Presets));
}
