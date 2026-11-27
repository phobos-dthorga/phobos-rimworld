using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Framework.Rules;

/// <summary>
/// The setup wizard's game-free rules (Framework 0.362.0; docs/development/first-load-setup-wizard.md, the owner's cards of 28 Nov 2026):
/// which pages are due, which preset a page's settings match now, and which of its settings no preset sets.
/// </summary>
public static class SetupRule
{
    /// <summary>A page is due when it was never seen, or was seen at an older version than it has now.</summary>
    public static bool Due(IReadOnlyDictionary<string, int> seen, string page, int version) =>
        !seen.TryGetValue(page, out int at) || at < version;

    /// <summary>The pages due, in the order given.</summary>
    public static List<string> DuePages(IReadOnlyDictionary<string, int> seen, IEnumerable<(string Page, int Version)> pages) =>
        pages.Where(p => Due(seen, p.Page, p.Version)).Select(p => p.Page).ToList();

    /// <summary>
    /// The first preset whose every value the settings hold now, or -1 for none (the player's own mix). A value for a setting that is not
    /// there (<paramref name="current"/> answers null: a tie-in's setting with its mod inactive) is passed over; a preset with no value
    /// that can be read matches nothing.
    /// </summary>
    public static int MatchingPreset(IReadOnlyList<IReadOnlyList<(string Setting, string Value)>> presets, Func<string, string?> current)
    {
        for (int i = 0; i < presets.Count; i++)
        {
            bool any = false;
            bool all = true;
            foreach ((string setting, string value) in presets[i])
            {
                if (current(setting) is not { } now) continue;
                any = true;
                if (!string.Equals(now, value, StringComparison.OrdinalIgnoreCase))
                {
                    all = false;
                    break;
                }
            }
            if (any && all) return i;
        }
        return -1;
    }

    /// <summary>The page's settings that no preset sets: shown at all times, where the preset's own are shown on request.</summary>
    public static List<string> OutsidePresets(IEnumerable<string> settings, IEnumerable<IEnumerable<(string Setting, string Value)>> presets)
    {
        var set = new HashSet<string>(presets.SelectMany(p => p.Select(v => v.Setting)), StringComparer.Ordinal);
        return settings.Where(s => !set.Contains(s)).ToList();
    }
}
