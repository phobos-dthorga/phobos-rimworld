using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Medical;
using Phobos.Triage.Rules;
using Phobos.Triage.Services;
using Verse;

namespace Phobos.Triage;

/// <summary>Player preferences. Thresholds and bands live in the rules Def, not here. The saved keys never change.</summary>
public class TriageSettings : ModSettings
{
    public bool enabled = true;
    public TriageAuthority defaultColonist = TriageAuthority.LifeSaving;
    public TriageAuthority defaultSlave = TriageAuthority.LifeSaving;
    public TriageAuthority defaultPrisoner = TriageAuthority.LifeSaving;
    public TriageAuthority defaultGuest = TriageAuthority.None;
    /// <summary>The highest chance of death on the table a doctor may accept without orders (authored default).</summary>
    public float maxDeathOnFailureChance = 0.05f;
    public bool allowRareTools = true;
    public bool allowViolations;
    public bool messages = true;
    public bool triageOrdering = true;

    public TriageAuthority DefaultFor(PatientGroup group) => group switch
    {
        PatientGroup.Slave => defaultSlave,
        PatientGroup.Prisoner => defaultPrisoner,
        PatientGroup.Guest => defaultGuest,
        _ => defaultColonist,
    };

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enabled, "PhobosTriage_Enabled", true);
        Scribe_Values.Look(ref defaultColonist, "PhobosTriage_DefaultColonist", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultSlave, "PhobosTriage_DefaultSlave", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultPrisoner, "PhobosTriage_DefaultPrisoner", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultGuest, "PhobosTriage_DefaultGuest", TriageAuthority.None);
        Scribe_Values.Look(ref maxDeathOnFailureChance, "PhobosTriage_MaxDeathOnFailureChance", 0.05f);
        Scribe_Values.Look(ref allowRareTools, "PhobosTriage_AllowRareTools", true);
        Scribe_Values.Look(ref allowViolations, "PhobosTriage_AllowViolations", false);
        Scribe_Values.Look(ref messages, "PhobosTriage_Messages", true);
        Scribe_Values.Look(ref triageOrdering, "PhobosTriage_TriageOrdering", true);
    }
}

/// <summary>
/// D'thorga Medical's triage part (D'thorga Triage until 7 November 2026): doctors queue the life-saving operations a patient
/// needs, and take the worst case first. One planning pass per map on an interval, the game's own bills, a per-person authority
/// through the framework's patient comp, and a prioritised copy of the game's operate giver. No Harmony. More Injuries, Search and
/// Rescue and Hospitality are optional and never named: operations are found by what they remove.
/// </summary>
public static class TriageMod
{
    public const string LogPrefix = "[D'thorga Medical: triage]";

    public static TriageSettings Settings => MedicalMod.Settings.triage;

    internal static LoadReport Report(LoadReport report)
    {
        if (!Settings.enabled) report.Off("operations on the doctors' own", "setting");
        else report.On("operations on the doctors' own: colonists " + Word(Settings.defaultColonist) + ", slaves " + Word(Settings.defaultSlave)
            + ", prisoners " + Word(Settings.defaultPrisoner) + ", guests " + Word(Settings.defaultGuest)
            + ", death chance accepted up to " + (Settings.maxDeathOnFailureChance * 100f).ToString("0") + "%");
        int operations = OperationIndex.All.Count;
        report.On(operations + " operation" + (operations == 1 ? "" : "s") + " indexed by the condition they remove");
        if (Settings.triageOrdering) report.On("doctors take the worst case first"); else report.Off("doctors take the worst case first", "setting");
        if (Settings.messages) report.On("messages"); else report.Off("messages", "setting");
        return report;
    }

    static string Word(TriageAuthority authority) => authority switch
    {
        TriageAuthority.None => "never",
        TriageAuthority.LifeSaving => "life-saving",
        TriageAuthority.LifeSavingAndCrippling => "life-saving and crippling",
        _ => "any repair",
    };

    static readonly TriageSettings Defaults = new();

    /// <summary>The "Triage" page (settings pages, 20 Nov 2026).</summary>
    internal static void DrawSettings(SettingsList list)
    {
        TriageSettings s = Settings, d = Defaults;
        list.Check("PhobosTriage_SettingEnabled".Translate(), ref s.enabled, d.enabled, "PhobosTriage_SettingEnabledDesc".Translate());
        list.Section("PhobosTriage_SettingDefaults".Translate());
        list.Note("PhobosTriage_SettingDefaultsDesc".Translate());
        AuthorityRow(list, "PhobosTriage_GroupColonists", s.defaultColonist, d.defaultColonist);
        AuthorityRow(list, "PhobosTriage_GroupSlaves", s.defaultSlave, d.defaultSlave);
        AuthorityRow(list, "PhobosTriage_GroupPrisoners", s.defaultPrisoner, d.defaultPrisoner);
        AuthorityRow(list, "PhobosTriage_GroupGuests", s.defaultGuest, d.defaultGuest);
        list.Section("PhobosTriage_SectionLimits".Translate());
        list.Slider("PhobosTriage_SettingMaxDeath".Translate((s.maxDeathOnFailureChance * 100f).ToString("0").Named("PERCENT")),
            ref s.maxDeathOnFailureChance, d.maxDeathOnFailureChance, 0f, 0.5f, "PhobosTriage_SettingMaxDeathDesc".Translate());
        list.Check("PhobosTriage_SettingRareTools".Translate(), ref s.allowRareTools, d.allowRareTools, "PhobosTriage_SettingRareToolsDesc".Translate());
        list.Check("PhobosTriage_SettingViolations".Translate(), ref s.allowViolations, d.allowViolations, "PhobosTriage_SettingViolationsDesc".Translate());
        list.Check("PhobosTriage_SettingOrdering".Translate(), ref s.triageOrdering, d.triageOrdering, "PhobosTriage_SettingOrderingDesc".Translate());
        list.Check("PhobosTriage_SettingMessages".Translate(), ref s.messages, d.messages, "PhobosTriage_SettingMessagesDesc".Translate());
    }

    static void AuthorityRow(SettingsList list, string groupKey, TriageAuthority current, TriageAuthority byDefault) =>
        list.Choice(groupKey.Translate(), current, byDefault, (TriageAuthority[])System.Enum.GetValues(typeof(TriageAuthority)),
            authority => ("PhobosTriage_Authority" + authority).Translate().Resolve(), chosen => SetDefault(groupKey, chosen));


    /// <summary>A float menu outlives the settings frame, so the choice lands by group name rather than by reference.</summary>
    static void SetDefault(string groupKey, TriageAuthority authority)
    {
        switch (groupKey)
        {
            case "PhobosTriage_GroupSlaves": Settings.defaultSlave = authority; break;
            case "PhobosTriage_GroupPrisoners": Settings.defaultPrisoner = authority; break;
            case "PhobosTriage_GroupGuests": Settings.defaultGuest = authority; break;
            default: Settings.defaultColonist = authority; break;
        }
    }
}
