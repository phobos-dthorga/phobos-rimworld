using System;
using System.Collections.Generic;
using UnityEngine;
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

    /// <summary>The colour of a setting's cost warning: amber, readable on the dark settings window (agent choice).</summary>
    public static readonly Color WarningColor = new(1f, 0.72f, 0.3f);

    /// <summary>
    /// A setting's cost, in amber small print under it: what a choice does to the game's speed. Pass null or empty to draw nothing,
    /// so the caller can show the warning only for the choices that cost.
    /// </summary>
    public static void Warning(Listing_Standard listing, string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Color colour = GUI.color;
        GameFont font = Text.Font;
        GUI.color = WarningColor;
        Text.Font = GameFont.Tiny;
        listing.Label(text);
        Text.Font = font;
        GUI.color = colour;
    }

    /// <summary>
    /// A labelled button that opens a menu of choices. The menu outlives the settings frame, so the choice lands through
    /// <paramref name="choose"/>, which should write the settings object directly.
    /// </summary>
    public static void Choice<T>(Listing_Standard listing, string label, T current, IEnumerable<T> options, Func<T, string> optionLabel,
        Action<T> choose, string? tooltip = null)
    {
        if (!listing.ButtonTextLabeled(label, optionLabel(current), tooltip: tooltip)) return;
        var menu = new List<FloatMenuOption>();
        foreach (T option in options)
        {
            T chosen = option;
            menu.Add(new FloatMenuOption(optionLabel(chosen), () => choose(chosen)));
        }
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    /// <summary>A heading between the parts of one settings window, in the game's medium font, with a line under it.</summary>
    public static void Section(Listing_Standard listing, string label)
    {
        listing.Gap(16f);
        Text.Font = GameFont.Medium;
        listing.Label(label);
        Text.Font = GameFont.Small;
        listing.GapLine();
    }
}

/// <summary>
/// A settings page that scrolls when it is taller than the window. Keep one per Mod (it remembers the scroll position and the
/// height the page needed last frame): Begin in DoSettingsWindowContents, draw into the listing it returns, then End.
/// </summary>
public sealed class SettingsScroll
{
    const float ScrollBarWidth = 16f;
    const float BottomMargin = 12f;

    Vector2 position;
    float height = 1200f;

    public Listing_Standard Begin(Rect inRect)
    {
        var view = new Rect(0f, 0f, inRect.width - ScrollBarWidth, height);
        Widgets.BeginScrollView(inRect, ref position, view);
        var listing = new Listing_Standard();
        listing.Begin(view);
        return listing;
    }

    public void End(Listing_Standard listing)
    {
        height = listing.CurHeight + BottomMargin;
        listing.End();
        Widgets.EndScrollView();
    }
}
