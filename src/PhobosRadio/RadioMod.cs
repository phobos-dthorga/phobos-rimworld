using UnityEngine;
using Verse;

namespace Phobos.Radio;

public enum DisplayMode
{
    Plain,
    Operator,
    Engineer,
}

/// <summary>Player preferences. Physics rules live in <see cref="RadioRulesDef"/>, not here.</summary>
public class RadioSettings : ModSettings
{
    public DisplayMode displayMode = DisplayMode.Plain;
    /// <summary>What one map cell measures. Changing it mid-save changes every path and feedline loss.</summary>
    public float metresPerCell = 1f;
    public bool inTouchThought = true;
    /// <summary>Whether the radio terminal's computer counts as a noise source (Radio 0.5.0; on, the owner's choice of 24 Nov 2026).</summary>
    public bool terminalNoise = true;
    /// <summary>Whether the radio desk shows a Waterfall tab too (Radio 0.6.0; off, the owner's choice of 24 Nov 2026).</summary>
    public bool deskWaterfall;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref displayMode, "displayMode", DisplayMode.Plain);
        Scribe_Values.Look(ref metresPerCell, "metresPerCell", 1f);
        Scribe_Values.Look(ref inTouchThought, "inTouchThought", true);
        Scribe_Values.Look(ref terminalNoise, "terminalNoise", true);
        Scribe_Values.Look(ref deskWaterfall, "deskWaterfall", false);
    }
}

public class RadioMod : Mod
{
    public static RadioSettings Settings { get; private set; } = new();

    public RadioMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<RadioSettings>();
    }

    public override string SettingsCategory() => "PhobosRadio_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("PhobosRadio_SettingDisplayMode".Translate());
        foreach (DisplayMode mode in System.Enum.GetValues(typeof(DisplayMode)))
        {
            if (listing.RadioButton(("PhobosRadio_DisplayMode" + mode).Translate(), Settings.displayMode == mode, 8f,
                    ("PhobosRadio_DisplayMode" + mode + "Desc").Translate()))
            {
                Settings.displayMode = mode;
            }
        }
        listing.Gap();
        listing.Label("PhobosRadio_SettingMetresPerCell".Translate(Settings.metresPerCell.ToString("0.0").Named("METRES")),
            tooltip: "PhobosRadio_SettingMetresPerCellDesc".Translate());
        Settings.metresPerCell = Mathf.Round(listing.Slider(Settings.metresPerCell, 0.5f, 30f) * 2f) / 2f;
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingInTouchThought".Translate(), ref Settings.inTouchThought,
            "PhobosRadio_SettingInTouchThoughtDesc".Translate());
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingTerminalNoise".Translate(), ref Settings.terminalNoise,
            "PhobosRadio_SettingTerminalNoiseDesc".Translate());
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingDeskWaterfall".Translate(), ref Settings.deskWaterfall,
            "PhobosRadio_SettingDeskWaterfallDesc".Translate());
        listing.End();
    }
}
