using System.Collections.Generic;
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
    /// <summary>
    /// Kinds of advice by radio switched off, by Def name (Radio 0.23.0): only a kind with its own switch, a riskier one (surgery), is listed in
    /// the window, and each starts on (docs/development/radio-advice-and-relays.md, "Risk and settings").
    /// </summary>
    public List<string> adviceOff = new();
    /// <summary>
    /// Whether another faction's doctor or mechanic gives advice by radio (Radio 0.26.0, mechanics 0.27.0; the owner's cards of 27 Nov 2026). A low risk (the agent's grade:
    /// a neutral faction's goodwill, never below the rules' floor), so on by default (docs/development/radio-advice-and-relays.md).
    /// </summary>
    public bool factionAdvice = true;
    /// <summary>Whether neighbours ask for a colonist on the radio now and then (Radio 0.36.0; on by default, an agent default).</summary>
    public bool neighbourCalls = true;

    /// <summary>
    /// Whether relay masts left on the world map wear out and can be knocked down by raiders (Radio 0.37.0; the owner's cards of 27 Nov 2026:
    /// "Raids and wear", and off by default, as losing a whole mast at once is a high risk).
    /// </summary>
    public bool relayMastLosses;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref displayMode, "displayMode", DisplayMode.Plain);
        Scribe_Values.Look(ref metresPerCell, "metresPerCell", 1f);
        Scribe_Values.Look(ref inTouchThought, "inTouchThought", true);
        Scribe_Values.Look(ref terminalNoise, "terminalNoise", true);
        Scribe_Values.Look(ref deskWaterfall, "deskWaterfall", false);
        Scribe_Collections.Look(ref adviceOff, "adviceOff", LookMode.Value);
        adviceOff ??= new List<string>();
        Scribe_Values.Look(ref factionAdvice, "factionAdvice", true);
        Scribe_Values.Look(ref neighbourCalls, "neighbourCalls", true);
        Scribe_Values.Look(ref relayMastLosses, "relayMastLosses", false);
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
        // Radio 0.23.0: a riskier kind of advice by radio has its own switch, on until turned off.
        foreach (RadioAdviceDef advice in DefDatabase<RadioAdviceDef>.AllDefsListForReading)
        {
            if (!advice.ownSetting) continue;
            bool on = !Settings.adviceOff.Contains(advice.defName);
            listing.Gap();
            listing.CheckboxLabeled(advice.settingLabel, ref on, advice.settingDescription);
            if (on) Settings.adviceOff.Remove(advice.defName);
            else if (!Settings.adviceOff.Contains(advice.defName)) Settings.adviceOff.Add(advice.defName);
        }
        // Radio 0.26.0: another faction's doctor by radio, on until turned off.
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingFactionAdvice".Translate(), ref Settings.factionAdvice,
            "PhobosRadio_SettingFactionAdviceDesc".Translate());
        // Radio 0.36.0: neighbours asking for help by radio, on until turned off.
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingNeighbourCalls".Translate(), ref Settings.neighbourCalls,
            "PhobosRadio_SettingNeighbourCallsDesc".Translate());
        // Radio 0.37.0: relay masts wear and can be raided, off until turned on.
        listing.Gap();
        listing.CheckboxLabeled("PhobosRadio_SettingRelayMastLosses".Translate(), ref Settings.relayMastLosses,
            "PhobosRadio_SettingRelayMastLossesDesc".Translate());
        listing.End();
    }
}
