using Verse;

namespace Phobos.Framework;

/// <summary>
/// How much a mod shows of its working. The reason is the same in every mode; only the depth changes
/// (docs/development/player-language.md): Plain never shows a decibel or a percentage, Operator shows the figures a
/// practitioner would want, Engineer shows the full working.
/// </summary>
public enum DisplayMode
{
    Plain,
    Operator,
    Engineer,
}

/// <summary>Settings-window pieces every Phobos mod draws the same way.</summary>
public static class SettingsUi
{
    /// <summary>
    /// Three radio buttons for the display mode. The labels come from the framework's own language file, so a mod needs no
    /// keys of its own; pass a <paramref name="keyPrefix"/> to use the mod's instead ("Prefix_DisplayModePlain" and "...Desc").
    /// </summary>
    public static void DrawDisplayMode(Listing_Standard listing, ref DisplayMode mode, string keyPrefix = "PhobosFramework")
    {
        listing.Label((keyPrefix + "_SettingDisplayMode").Translate());
        foreach (DisplayMode candidate in System.Enum.GetValues(typeof(DisplayMode)))
        {
            string key = keyPrefix + "_DisplayMode" + candidate;
            if (listing.RadioButton(key.Translate(), mode == candidate, 8f, (key + "Desc").Translate()))
            {
                mode = candidate;
            }
        }
    }
}
