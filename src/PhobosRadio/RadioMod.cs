using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
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

    /// <summary>
    /// Whether an ally whose settlement the colony's stations reach warns of a raid by radio, holding it back a few hours (Radio 0.53.0; the
    /// owner's card of 27 Nov 2026, "Raid warning"). A benefit with no risk: on by default.
    /// </summary>
    public bool alliesWarnOfRaids = true;

    /// <summary>
    /// Whether a hostile settlement that hears a trader caravan asked for by radio may ambush it on its way (Radio 0.53.0; the owner's card of
    /// 27 Nov 2026, "Ambush the trader"). A moderate risk (goodwill spent, a trade lost, no colonist at stake): on by default.
    /// </summary>
    public bool listenersAmbushTraders = true;

    /// <summary>
    /// Whether a jammer's noise reaches radios (Radio 0.45.0; docs/development/electronic-warfare.md, EW1): links fail by physics near a
    /// jammer, ours included. A moderate risk (the agent's grade: a link lost at a bad moment, shown on the link sheet beforehand), so on
    /// by default. Off, jammers still blind radar as before.
    /// </summary>
    public bool jammingReachesRadios = true;

    /// <summary>
    /// The colony's own callsign on its QSL cards (Radio 0.62.0; the owner's leftover from the QSL record, 28 Nov 2026): empty works one out
    /// from the colony's place, as before. Letters, digits and a stroke, at most ten (QslRule.CleanCallsign).
    /// </summary>
    public string ownCallsign = "";

    /// <summary>
    /// S3 (Radio 0.64.0; the owner's choice of 28 Nov 2026: with HF, off by default, as RT6 graded it a high risk): the game's comms console
    /// calls only the factions the colony's radio reaches.
    /// </summary>
    public bool consoleNeedsReach;

    /// <summary>HF's world-map pictures (Radio 0.71.0; the owner's card: off until picked): the grey line, and the skip-zone rings.</summary>
    public bool hfGreyLine;
    public bool hfSkipRings;
    /// <summary>The flare's wash on the world map (Radio 0.72.0, RV13): off until picked.</summary>
    public bool hfFlareWash;

    /// <summary>
    /// Whether colonists talk on the radio by themselves (Radio 0.63.0; docs/development/radio-life-on-the-air.md, slice 1): chats across the
    /// map, calls in the evening to someone away, company through the night; and a colonist's hand-held lights the lamp only while they talk.
    /// No risk (the agent's grade: mood and the social log only), so on by default. Off, the radio is quiet as before.
    /// </summary>
    public bool talkOnAir = true;

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
        Scribe_Values.Look(ref alliesWarnOfRaids, "alliesWarnOfRaids", true);
        Scribe_Values.Look(ref listenersAmbushTraders, "listenersAmbushTraders", true);
        Scribe_Values.Look(ref jammingReachesRadios, "jammingReachesRadios", true);
        Scribe_Values.Look(ref ownCallsign, "ownCallsign", "");
        Scribe_Values.Look(ref consoleNeedsReach, "consoleNeedsReach", false);
        Scribe_Values.Look(ref hfGreyLine, "hfGreyLine", false);
        Scribe_Values.Look(ref hfSkipRings, "hfSkipRings", false);
        Scribe_Values.Look(ref hfFlareWash, "hfFlareWash", false);
        ownCallsign ??= "";
        Scribe_Values.Look(ref talkOnAir, "talkOnAir", true);
    }
}

public class RadioMod : SettingsBookMod
{
    public static RadioSettings Settings { get; private set; } = new();

    /// <summary>Every setting's default, read by the settings pages and the setup wizard.</summary>
    static readonly RadioSettings Defaults = new();

    /// <summary>The radio's log, through D'thorga Framework's shared logger (adopted in Radio 0.45.0).</summary>
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Radio Framework");

    /// <summary>The mod's folder (Radio 0.62.0: HF's tables ship in it).</summary>
    public static ModContentPack? ContentPack { get; private set; }

    public RadioMod(ModContentPack content) : base(content)
    {
        ContentPack = content;
        AddSettingsPages();
        Settings = GetSettings<RadioSettings>();
        // Radio 0.53.0: the radio's own patches (the raid warning), each switched off by itself if its target is gone.
        HarmonyPatchApplier.ApplyEach(new Harmony(content.PackageId), typeof(RadioMod).Assembly, "[D'thorga Radio Framework]");
        RegisterSetup();
    }

    /// <summary>Round values for the setup wizard's metres per cell (Radio 0.55.0); the settings window's slider still takes any half metre.</summary>
    static readonly float[] SetupMetres = { 0.5f, 1f, 2f, 3f, 5f, 10f, 20f, 30f };

    /// <summary>The settings D'thorga Framework's setup wizard offers (Radio 0.55.0), with the labels this mod's settings window shows.</summary>
    void RegisterSetup()
    {
        RadioSettings defaults = Defaults;
        Phobos.Framework.Setup.SetupSettings.Choice("radio.metresPerCell", this,
            () => "PhobosRadio_SetupMetresLabel".Translate(),
            () => "PhobosRadio_SettingMetresPerCellDesc".Translate(), () => Settings.metresPerCell, x => Settings.metresPerCell = x,
            defaults.metresPerCell, SetupMetres, x => "PhobosRadio_SetupMetres".Translate(x.ToString("0.#").Named("METRES")));
        Phobos.Framework.Setup.SetupSettings.Choice("radio.displayMode", this, () => "PhobosRadio_SettingDisplayMode".Translate(), null,
            () => Settings.displayMode, x => Settings.displayMode = x, defaults.displayMode,
            (DisplayMode[])System.Enum.GetValues(typeof(DisplayMode)), x => ("PhobosRadio_DisplayMode" + x).Translate());
        Tick("radio.terminalNoise", "PhobosRadio_SettingTerminalNoise", () => Settings.terminalNoise, x => Settings.terminalNoise = x, defaults.terminalNoise);
        Tick("radio.alliesWarnOfRaids", "PhobosRadio_SettingAlliesWarnOfRaids", () => Settings.alliesWarnOfRaids, x => Settings.alliesWarnOfRaids = x,
            defaults.alliesWarnOfRaids);
        Tick("radio.listenersAmbushTraders", "PhobosRadio_SettingListenersAmbushTraders", () => Settings.listenersAmbushTraders,
            x => Settings.listenersAmbushTraders = x, defaults.listenersAmbushTraders);
        Tick("radio.jammingReachesRadios", "PhobosRadio_SettingJammingReachesRadios", () => Settings.jammingReachesRadios,
            x => Settings.jammingReachesRadios = x, defaults.jammingReachesRadios);
        Tick("radio.consoleNeedsReach", "PhobosRadio_SettingConsoleNeedsReach", () => Settings.consoleNeedsReach, x => Settings.consoleNeedsReach = x,
            defaults.consoleNeedsReach);
        Tick("radio.relayMastLosses", "PhobosRadio_SettingRelayMastLosses", () => Settings.relayMastLosses, x => Settings.relayMastLosses = x,
            defaults.relayMastLosses);
        Tick("radio.talkOnAir", "PhobosRadio_SettingTalkOnAir", () => Settings.talkOnAir, x => Settings.talkOnAir = x, defaults.talkOnAir);
    }

    void Tick(string id, string key, System.Func<bool> get, System.Action<bool> set, bool byDefault) =>
        Phobos.Framework.Setup.SetupSettings.Tick(id, this, () => key.Translate(), () => (key + "Desc").Translate(), get, set, byDefault);

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.RadioFramework", () => "PhobosRadio_SettingsCategory".Translate());
    protected override SettingsBook Book => SettingsPages;

    /// <summary>
    /// The settings window as every other D'thorga mod lays it out (Radio 0.72.1; the owner's choice of 20 Nov 2026 for every mod, which the
    /// radio, then on the back burner, had waited for): pages down the left, search over every page, Changed only, and a reset per page.
    /// The single list it replaced had no scroll, so the settings below the window's edge could be found by search but never reached.
    /// </summary>
    void AddSettingsPages()
    {
        SettingsPages.Join(this)
            .Add(new SettingsPage("general", 0f, () => "PhobosRadio_PageGeneral".Translate(), DrawGeneral))
            .Add(new SettingsPage("colonists", 10f, () => "PhobosRadio_PageColonists".Translate(), DrawColonists))
            .Add(new SettingsPage("stations", 20f, () => "PhobosRadio_PageStations".Translate(), DrawStations))
            .Add(new SettingsPage("calling", 30f, () => "PhobosRadio_PageCalling".Translate(), DrawCalling))
            .Add(new SettingsPage("worldmap", 40f, () => "PhobosRadio_PageWorldMap".Translate(), DrawWorldMap));
    }

    static void DrawGeneral(SettingsList list)
    {
        RadioSettings s = Settings;
        list.Section("PhobosRadio_SectionDisplay".Translate());
        list.Radio("PhobosRadio_SettingDisplayMode".Translate(), ref s.displayMode, Defaults.displayMode,
            (DisplayMode[])System.Enum.GetValues(typeof(DisplayMode)), m => ("PhobosRadio_DisplayMode" + m).Translate(),
            m => ("PhobosRadio_DisplayMode" + m + "Desc").Translate());
        list.Section("PhobosRadio_SectionMapScale".Translate());
        list.Slider("PhobosRadio_SettingMetresPerCell".Translate(s.metresPerCell.ToString("0.0").Named("METRES")), ref s.metresPerCell,
            Defaults.metresPerCell, 0.5f, 30f, "PhobosRadio_SettingMetresPerCellDesc".Translate(), x => Mathf.Round(x * 2f) / 2f);
        // Radio 0.62.0: the colony's own callsign on its QSL cards; empty works one out.
        list.Section("PhobosRadio_SectionQsl".Translate());
        CallsignRow(list, s);
    }

    /// <summary>The callsign's text box: a row the page draws itself, so it resets itself too.</summary>
    static void CallsignRow(SettingsList list, RadioSettings s)
    {
        if (list.Resetting) s.ownCallsign = Defaults.ownCallsign;
        string label = "PhobosRadio_SettingOwnCallsign".Translate();
        string tip = "PhobosRadio_SettingOwnCallsignDesc".Translate();
        if (list.CustomRow(label, tip, s.ownCallsign != Defaults.ownCallsign, CallsignRowHeight) is not { } row) return;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(row.LeftHalf(), label);
        Text.Anchor = TextAnchor.UpperLeft;
        s.ownCallsign = Phobos.Radio.Physics.QslRule.CleanCallsign(Widgets.TextField(row.RightHalf().ContractedBy(0f, 3f), s.ownCallsign));
        list.Note(tip);
    }

    const float CallsignRowHeight = 30f;

    static void DrawColonists(SettingsList list)
    {
        RadioSettings s = Settings;
        list.Section("PhobosRadio_SectionMood".Translate());
        list.Check("PhobosRadio_SettingInTouchThought".Translate(), ref s.inTouchThought, Defaults.inTouchThought,
            "PhobosRadio_SettingInTouchThoughtDesc".Translate());
        // Radio 0.63.0: colonists talk on the air by themselves, on until turned off.
        list.Section("PhobosRadio_SectionTalk".Translate());
        list.Check("PhobosRadio_SettingTalkOnAir".Translate(), ref s.talkOnAir, Defaults.talkOnAir, "PhobosRadio_SettingTalkOnAirDesc".Translate());
    }

    static void DrawStations(SettingsList list)
    {
        RadioSettings s = Settings;
        list.Section("PhobosRadio_SectionNoise".Translate());
        list.Check("PhobosRadio_SettingTerminalNoise".Translate(), ref s.terminalNoise, Defaults.terminalNoise,
            "PhobosRadio_SettingTerminalNoiseDesc".Translate());
        // Radio 0.45.0: a jammer's noise drowns radios nearby, on until turned off.
        list.Check("PhobosRadio_SettingJammingReachesRadios".Translate(), ref s.jammingReachesRadios, Defaults.jammingReachesRadios,
            "PhobosRadio_SettingJammingReachesRadiosDesc".Translate());
        list.Section("PhobosRadio_SectionDesk".Translate());
        list.Check("PhobosRadio_SettingDeskWaterfall".Translate(), ref s.deskWaterfall, Defaults.deskWaterfall,
            "PhobosRadio_SettingDeskWaterfallDesc".Translate());
        // Radio 0.37.0: relay masts wear and can be raided, off until turned on.
        list.Section("PhobosRadio_SectionRelays".Translate());
        list.Check("PhobosRadio_SettingRelayMastLosses".Translate(), ref s.relayMastLosses, Defaults.relayMastLosses,
            "PhobosRadio_SettingRelayMastLossesDesc".Translate());
    }

    static void DrawCalling(SettingsList list)
    {
        RadioSettings s = Settings;
        list.Section("PhobosRadio_SectionAdvice".Translate());
        // Radio 0.23.0: a riskier kind of advice by radio has its own switch, on until turned off.
        foreach (RadioAdviceDef advice in DefDatabase<RadioAdviceDef>.AllDefsListForReading)
        {
            if (!advice.ownSetting) continue;
            bool on = !s.adviceOff.Contains(advice.defName);
            if (!list.Check(advice.settingLabel ?? advice.LabelCap.ToString(), ref on, true, advice.settingDescription)) continue;
            if (on) s.adviceOff.Remove(advice.defName);
            else if (!s.adviceOff.Contains(advice.defName)) s.adviceOff.Add(advice.defName);
        }
        // Radio 0.26.0: another faction's doctor by radio, on until turned off.
        list.Check("PhobosRadio_SettingFactionAdvice".Translate(), ref s.factionAdvice, Defaults.factionAdvice,
            "PhobosRadio_SettingFactionAdviceDesc".Translate());
        list.Section("PhobosRadio_SectionFactions".Translate());
        // Radio 0.36.0: neighbours asking for help by radio, on until turned off.
        list.Check("PhobosRadio_SettingNeighbourCalls".Translate(), ref s.neighbourCalls, Defaults.neighbourCalls,
            "PhobosRadio_SettingNeighbourCallsDesc".Translate());
        // Radio 0.53.0: calling other factions by radio.
        list.Check("PhobosRadio_SettingAlliesWarnOfRaids".Translate(), ref s.alliesWarnOfRaids, Defaults.alliesWarnOfRaids,
            "PhobosRadio_SettingAlliesWarnOfRaidsDesc".Translate());
        list.Check("PhobosRadio_SettingListenersAmbushTraders".Translate(), ref s.listenersAmbushTraders, Defaults.listenersAmbushTraders,
            "PhobosRadio_SettingListenersAmbushTradersDesc".Translate());
        // Radio 0.64.0: the comms console needs the radio's reach (S3), off until turned on.
        list.Check("PhobosRadio_SettingConsoleNeedsReach".Translate(), ref s.consoleNeedsReach, Defaults.consoleNeedsReach,
            "PhobosRadio_SettingConsoleNeedsReachDesc".Translate());
    }

    static void DrawWorldMap(SettingsList list)
    {
        RadioSettings s = Settings;
        // Radio 0.71.0 and 0.72.0: HF's world-map pictures, each off until picked.
        list.Section("PhobosRadio_SectionPictures".Translate());
        list.Check("PhobosRadio_SettingHfGreyLine".Translate(), ref s.hfGreyLine, Defaults.hfGreyLine, "PhobosRadio_SettingHfGreyLineDesc".Translate());
        list.Check("PhobosRadio_SettingHfSkipRings".Translate(), ref s.hfSkipRings, Defaults.hfSkipRings, "PhobosRadio_SettingHfSkipRingsDesc".Translate());
        list.Check("PhobosRadio_SettingHfFlareWash".Translate(), ref s.hfFlareWash, Defaults.hfFlareWash, "PhobosRadio_SettingHfFlareWashDesc".Translate());
    }
}
